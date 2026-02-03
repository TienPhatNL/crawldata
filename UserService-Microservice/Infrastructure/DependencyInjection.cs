using System;
using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PayOS;
using StackExchange.Redis;
using UserService.Domain.Common;
using UserService.Domain.Interfaces;
using UserService.Domain.Services;
using UserService.Infrastructure.BackgroundServices;
using UserService.Infrastructure.Caching;
using UserService.Infrastructure.Configuration;
using UserService.Infrastructure.Messaging;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;
using UserService.Infrastructure.Caching;

namespace UserService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<UserDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("UserDb")));

        // Unit of Work (manages all repository access)
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IQuotaSnapshotService, QuotaSnapshotService>();
        services.AddScoped<IPayOSPaymentService, PayOSPaymentService>();
        services.AddSingleton<IAmazonS3>(_ =>
        {
            var accessKey = configuration["AWS:AccessKey"]
                ?? throw new InvalidOperationException("AWS AccessKey not configured");
            var secretKey = configuration["AWS:SecretKey"]
                ?? throw new InvalidOperationException("AWS SecretKey not configured");
            var region = configuration["AWS:Region"] ?? RegionEndpoint.USEast1.SystemName;

            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            return new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(region));
        });
        services.AddScoped<IUploadService, UploadService>();

        // Redis Cache Configuration
        services.Configure<RedisCacheSettings>(configuration.GetSection(RedisCacheSettings.SectionName));
        services.Configure<QuotaSyncSettings>(configuration.GetSection(QuotaSyncSettings.SectionName));

        services.Configure<PayOSSettings>(configuration.GetSection(PayOSSettings.SectionName));
        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<PayOSSettings>>().Value;
            var logger = sp.GetRequiredService<ILogger<PayOSClient>>();

            static string ResolveSecret(string configuredValue, string envVariable)
            {
                if (!string.IsNullOrWhiteSpace(configuredValue))
                {
                    return configuredValue;
                }

                var fromEnv = Environment.GetEnvironmentVariable(envVariable);
                return string.IsNullOrWhiteSpace(fromEnv) ? string.Empty : fromEnv;
            }

            var clientId = ResolveSecret(settings.ClientId, "PAYOS_CLIENT_ID");
            var apiKey = ResolveSecret(settings.ApiKey, "PAYOS_API_KEY");
            var checksumKey = ResolveSecret(settings.ChecksumKey, "PAYOS_CHECKSUM_KEY");

            if (string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(checksumKey))
            {
                throw new InvalidOperationException("PayOS settings are not configured properly. Provide ClientId, ApiKey, and ChecksumKey via configuration or environment variables.");
            }

            var options = new PayOSOptions
            {
                ClientId = clientId,
                ApiKey = apiKey,
                ChecksumKey = checksumKey,
                Logger = logger
            };

            return new PayOSClient(options);
        });
        services.AddHttpClient<IPayOSRelayClient, PayOSRelayClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<PayOSSettings>>().Value;

            if (!string.IsNullOrWhiteSpace(settings.RelayBaseUrl))
            {
                client.BaseAddress = new Uri(settings.RelayBaseUrl, UriKind.Absolute);
            }

            if (settings.RelayTimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(settings.RelayTimeoutSeconds);
            }
        });
        
        // Redis Connection - Aspire provides connection string automatically
        var redisConnection = configuration.GetConnectionString("redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            // Register IConnectionMultiplexer for advanced Redis operations
            services.AddSingleton<IConnectionMultiplexer>(sp => 
                ConnectionMultiplexer.Connect(redisConnection));
            
            // Register IDistributedCache for standard caching operations
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "userservice:";
            });

            services.AddSingleton<IWebCrawlerQuotaCacheWriter, WebCrawlerQuotaCacheWriter>();
        }
        else
        {
            services.AddSingleton<IWebCrawlerQuotaCacheWriter, NoopWebCrawlerQuotaCacheWriter>();
        }
        
        // Redis Cache Service with stampede prevention
        services.AddScoped<IUserCacheService, RedisUserCacheService>();
        services.AddScoped<IPaymentConfirmationTokenStore, RedisPaymentConfirmationTokenStore>();

        // JWT Configuration
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<GoogleSettings>(configuration.GetSection(GoogleSettings.SectionName));
        services.Configure<SubscriptionExpirySyncSettings>(configuration.GetSection(SubscriptionExpirySyncSettings.SectionName));

        // Kafka Configuration
        var kafkaSection = configuration.GetSection(KafkaSettings.SectionName);
        services.Configure<KafkaSettings>(options =>
        {
            kafkaSection.Bind(options);

            // Use appsettings.json configuration (localhost:9092 for host-based services)
            // Aspire's GetConnectionString("kafka") resolves to host.docker.internal which doesn't work for host services
            Console.WriteLine($"[Kafka Config] Using appsettings.json: {options.BootstrapServers}");
        });
        var kafkaSettingsSnapshot = kafkaSection.Get<KafkaSettings>() ?? new KafkaSettings();
        
        // Kafka Event Publisher for domain events
        services.AddSingleton<UserService.Infrastructure.Messaging.KafkaEventPublisher>();
        
        // Kafka Cache Invalidation Publisher
        services.AddSingleton<UserService.Infrastructure.Messaging.CacheInvalidationPublisher>();

        // Kafka Consumer for handling requests from ClassroomService
        services.AddHostedService<UserServiceKafkaConsumer>();

        // Background sync workers
        if (kafkaSettingsSnapshot.EnableQuotaUsageConsumer)
        {
            services.AddHostedService<QuotaUsageKafkaConsumer>();
        }
        else
        {
            Console.WriteLine("[Kafka Config] Quota usage consumer disabled via configuration.");
        }
        services.AddHostedService<SubscriptionQuotaSyncWorker>();
        services.AddHostedService<ExpiredSubscriptionSyncBackgroundService>();

        return services;
    }
}
