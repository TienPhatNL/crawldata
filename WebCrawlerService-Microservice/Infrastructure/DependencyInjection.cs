using WebCrawlerService.Domain.Interfaces;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Polly;
using StackExchange.Redis;
using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Models;
using WebCrawlerService.Infrastructure.BackgroundServices;
using WebCrawlerService.Infrastructure.Contexts;
using static WebCrawlerService.Domain.Common.Policies;
using WebCrawlerService.Infrastructure.Caching;
using WebCrawlerService.Infrastructure.Interceptors;
using WebCrawlerService.Infrastructure.Messaging;
using WebCrawlerService.Infrastructure.Persistence.Interceptors;
using WebCrawlerService.Infrastructure.Repositories;
using WebCrawlerService.Infrastructure.Security.AuthorizationHandlers;
using WebCrawlerService.Infrastructure.Services;

namespace WebCrawlerService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register interceptors (these will be added later once we verify Aspire connection works)
        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<OutboxInterceptor>();

        // NOTE: DbContext is registered in Program.cs using Aspire's AddSqlServerDbContext helper
        // This allows Aspire to properly inject connection strings and manage database lifecycle
        // TODO: Add interceptors back once basic Aspire connection is verified

        // Repository implementations
        services.AddScoped<ICrawlJobRepository, CrawlJobRepository>();
        services.AddScoped<ICrawlerAgentRepository, CrawlerAgentRepository>();
        services.AddScoped<IDomainPolicyRepository, DomainPolicyRepository>();
        services.AddScoped<ICrawlResultRepository, CrawlResultRepository>();
        services.AddScoped<ICrawlTemplateRepository, CrawlTemplateRepository>();

        // Generic repositories for new entities
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IEventPublisher, KafkaEventPublisher>();
        services.AddScoped<IUserQuotaService, UserQuotaService>();
        services.AddHttpContextAccessor();

        // Mobile API Services
        AddMobileApiServices(services, configuration);

        // MCP and LLM Services
        AddMcpAndLlmServices(services, configuration);

        // Crawl4AI Services
        AddCrawl4AIServices(services, configuration);

        // JWT Configuration
        AddJwtAuthentication(services, configuration);
        
        // Authorization
        AddAuthorizationPolicies(services);
        
        // Caching
        AddCaching(services, configuration);
        
        // Messaging and Outbox
        AddMessaging(services, configuration);
        
        // Background Services
        services.AddHostedService<OutboxProcessorService>();
        services.AddHostedService<CrawlJobProcessorService>();
        services.AddHostedService<CrawlerAgentHealthService>();
        services.AddHostedService<JobSchedulingService>();
        services.AddHostedService<MetricsCollectionService>();

        // External service integrations
        // gRPC clients will be added here

        return services;
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = new JwtSettings();
        configuration.Bind(JwtSettings.SectionName, jwtSettings);
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });
    }

    private static void AddAuthorizationPolicies(IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, UserDataAccessHandler>();
        services.AddScoped<IAuthorizationHandler, SubscriptionTierHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.RequireAuthentication, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(Policies.RequireAdminRole, policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy(Policies.RequireEducatorRole, policy =>
                policy.RequireRole("Educator", "Admin"));

            options.AddPolicy(Policies.RequirePremiumTier, policy =>
                policy.Requirements.Add(new SubscriptionTierRequirement(SubscriptionTier.Premium)));

            options.AddPolicy(Policies.CanAccessUserData, policy =>
                policy.Requirements.Add(new UserDataAccessRequirement()));

            options.AddPolicy(Policies.CanManageCrawlers, policy =>
                policy.RequireRole("Admin", "CrawlerManager"));
        });
    }

    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var cacheSettings = new CacheSettings();
        configuration.Bind(CacheSettings.SectionName, cacheSettings);
        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));

        // Check Aspire connection string first (takes precedence over appsettings)
        var aspireRedisConnection = configuration.GetConnectionString("redis");

        if (!string.IsNullOrEmpty(aspireRedisConnection))
        {
            // Use Aspire's Redis connection
            Console.WriteLine($"[Redis Config] Using Aspire connection string: {aspireRedisConnection}");

            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(aspireRedisConnection));

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = aspireRedisConnection;
                options.InstanceName = cacheSettings.InstanceName;
            });

            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else if (cacheSettings.UseDistributedCache && !string.IsNullOrEmpty(cacheSettings.ConnectionString))
        {
            // Fallback to appsettings.json Redis
            Console.WriteLine($"[Redis Config] Using appsettings.json: {cacheSettings.ConnectionString}");

            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(cacheSettings.ConnectionString));

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheSettings.ConnectionString;
                options.InstanceName = cacheSettings.InstanceName;
            });

            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            // In-memory cache fallback
            Console.WriteLine("[Redis Config] Using in-memory cache (no Redis connection available)");
            services.AddDistributedMemoryCache(); // Registers IDistributedCache with in-memory implementation
            services.AddMemoryCache();
            services.AddScoped<ICacheService, MemoryCacheService>();
        }
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaSettings>(options =>
        {
            var kafkaSettings = configuration.GetSection(KafkaSettings.SectionName);
            kafkaSettings.Bind(options);

            // Use appsettings.json configuration (localhost:9092)
            // Aspire's GetConnectionString("kafka") auto-converts localhost→host.docker.internal on Windows
            // which resolves to network IP instead of 127.0.0.1, causing connection failures
            Console.WriteLine($"[Kafka Config] Using appsettings.json: {options.BootstrapServers}");
        });

        // Register Kafka Producer as singleton for quota usage events
        services.AddSingleton<Confluent.Kafka.IProducer<string, string>>(sp =>
        {
            var kafkaSettings = configuration.GetSection(KafkaSettings.SectionName).Get<KafkaSettings>();
            var config = new Confluent.Kafka.ProducerConfig
            {
                BootstrapServers = kafkaSettings?.BootstrapServers ?? "localhost:9092",
                Acks = Confluent.Kafka.Acks.All,
                MessageTimeoutMs = 10000,
                EnableIdempotence = true,
                MaxInFlight = 1,
                CompressionType = Confluent.Kafka.CompressionType.Snappy,
                BrokerAddressFamily = Confluent.Kafka.BrokerAddressFamily.V4
            };
            return new Confluent.Kafka.ProducerBuilder<string, string>(config).Build();
        });
    }

    private static void AddMobileApiServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure Shopee API settings
        services.Configure<ShopeeApiConfiguration>(
            configuration.GetSection("ShopeeApi"));

        // Register HttpClient for Shopee API with named client
        services.AddHttpClient("ShopeeApi")
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        // Register Shopee API service
        services.AddScoped<IShopeeApiService, ShopeeApiService>();

        // Register generic mobile API crawler service
        services.AddScoped<IMobileApiCrawlerService, MobileApiCrawlerService>();

        // Register crawler agents
        services.AddScoped<Agents.ShopeeCrawlerAgent>();
        services.AddScoped<Agents.MobileMcpCrawlerAgent>();

        // Register agent factory
        services.AddScoped<CrawlerAgentFactory>();

        // TODO: Add proxy rotation service when implemented
        // services.AddScoped<IProxyRotationService, ProxyRotationService>();
    }

    private static void AddMcpAndLlmServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure MCP settings
        services.Configure<Common.McpSettings>(
            configuration.GetSection(Common.McpSettings.SectionName));

        // Configure LLM settings
        services.Configure<Common.LlmSettings>(
            configuration.GetSection(Common.LlmSettings.SectionName));

        // Register HttpClient for LLM API calls
        services.AddHttpClient();

        // Register MCP client service (scoped because it manages a process)
        services.AddScoped<IMcpClientService, McpClientService>();

        // Register LLM extraction service
        services.AddScoped<ILlmExtractionService, LlmExtractionService>();
    }

    private static void AddCrawl4AIServices(IServiceCollection services, IConfiguration configuration)
    {
        // Note: Crawl4AI service implementations are in Application layer
        // They will be registered via reflection or in Application/Program.cs
        // Infrastructure only provides HttpClient factories

        // HttpClient for Gemini API calls (used by GeminiService in Application)
        services.AddHttpClient("GeminiClient", client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        // HttpClient for Crawl4AI agents with custom resilience for long-running operations
        services.AddHttpClient("Crawl4AIClient", client =>
        {
            // Default to first instance, will be overridden per request
            client.BaseAddress = new Uri(configuration["Crawl4AI:BaseUrl"] ?? "http://localhost:8004");
            client.Timeout = Timeout.InfiniteTimeSpan; // Let resilience handler control timeout
        })
        .AddStandardResilienceHandler(options =>
        {
            // Crawl4AI operations are long-running - need extended timeouts
            options.AttemptTimeout = new Microsoft.Extensions.Http.Resilience.HttpTimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromMinutes(5) // Match appsettings RequestTimeoutMinutes
            };

            options.TotalRequestTimeout = new Microsoft.Extensions.Http.Resilience.HttpTimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromMinutes(5) // Same as attempt - no retry on timeout
            };

            // Retry configuration - only for transient network errors, not timeouts
            options.Retry = new Microsoft.Extensions.Http.Resilience.HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                BackoffType = Polly.DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(2)
            };

            // Circuit breaker - protect against cascading failures
            // SamplingDuration must be >= 2 × AttemptTimeout (2 × 300s = 600s minimum)
            options.CircuitBreaker = new Microsoft.Extensions.Http.Resilience.HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromMinutes(12), // 720s (safe margin over 600s requirement)
                MinimumThroughput = 3,
                FailureRatio = 0.5,
                BreakDuration = TimeSpan.FromSeconds(30)
            };
        });

        // Add HttpClient for ClassroomService integration
        services.AddHttpClient<IClassroomValidationService, ClassroomValidationService>(client =>
        {
            var classroomServiceUrl = configuration["Services:ClassroomService:BaseUrl"] ?? "http://localhost:5006";
            client.BaseAddress = new Uri(classroomServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient("UserService", client =>
        {
            var userServiceUrl = configuration["Services:UserService:BaseUrl"] ?? "http://localhost:5001";
            client.BaseAddress = new Uri(userServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
    }
}
