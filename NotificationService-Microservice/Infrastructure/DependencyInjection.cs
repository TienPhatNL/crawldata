using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StackExchange.Redis;
using NotificationService.Domain.Common;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Services;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Caching;
using NotificationService.Infrastructure.Messaging;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database is now configured via Aspire's AddSqlServerDbContext in Program.cs
        // No need to manually register DbContext here

        // JWT Configuration
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Set to true in production
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                // Token extraction for both HTTP and SignalR (WebSocket)
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Extract token from query string FIRST (SignalR WebSocket connections)
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        // For HTTP requests, let the default JWT middleware handle Authorization header
                        // Don't manually extract - it interferes with proper validation

                        return Task.CompletedTask;
                    }
                };
            });

        // Add Authorization with policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Staff", "Admin"));
            options.AddPolicy("LecturerOnly", policy => policy.RequireRole("Lecturer"));
            options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
            options.AddPolicy("PaidUserOnly", policy => policy.RequireRole("PaidUser"));
        });

        // Register Domain Event Service
        services.AddScoped<IDomainEventService, DomainEventService>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Generic Repository (for entities that don't need custom repositories)
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register Specific Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<INotificationDeliveryLogRepository, NotificationDeliveryLogRepository>();

        // Register Email Service
        services.AddScoped<IEmailService, EmailService>();

        // Register Notification Delivery Service
        services.AddScoped<INotificationDeliveryService, NotificationDeliveryService>();

        // Redis Cache Configuration
        services.Configure<RedisCacheSettings>(configuration.GetSection(RedisCacheSettings.SectionName));

        // Redis Connection - Aspire provides connection string automatically (dynamic port), fallback to appsettings.json
        var aspireRedisConnection = configuration.GetConnectionString("redis");
        
        // Debug: Log all connection strings to diagnose
        var allConnectionStrings = configuration.GetSection("ConnectionStrings").GetChildren();
        Console.WriteLine($"[Redis Config] Available connection strings: {string.Join(", ", allConnectionStrings.Select(x => x.Key))}");
        
        if (!string.IsNullOrEmpty(aspireRedisConnection))
        {
            // Aspire provides connection in format "localhost:6379" (port may change each run)
            Console.WriteLine($"[Redis Config] Using Aspire connection string: {aspireRedisConnection}");
            
            // Register IConnectionMultiplexer for advanced Redis operations
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(aspireRedisConnection));

            // Register IDistributedCache for standard caching operations
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = aspireRedisConnection;
                options.InstanceName = "notification:";
            });
        }
        else
        {
            // Fallback to appsettings.json for local dev without Aspire
            Console.WriteLine("[Redis Config] WARNING: No Redis connection string found. Cache features will not work.");
            Console.WriteLine("[Redis Config] Run via Aspire AppHost or add 'redis' connection string to appsettings.Development.json");
        }

        // Redis Cache Service
        services.AddScoped<INotificationCacheService, NotificationCacheService>();

        // Kafka Configuration
        services.Configure<NotificationService.Domain.Common.KafkaSettings>(options =>
        {
            var kafkaSettings = configuration.GetSection(NotificationService.Domain.Common.KafkaSettings.SectionName);
            kafkaSettings.Bind(options);

            // Use appsettings.json configuration (localhost:9092 for host-based services)
            // Aspire's GetConnectionString("kafka") resolves to host.docker.internal which doesn't work for host services
            Console.WriteLine($"[Kafka Config] Using appsettings.json: {options.BootstrapServers}");
        });

        // Register Kafka services
        services.AddSingleton<KafkaEventPublisher>();
        services.AddSingleton<MessageCorrelationManager>();
        
        // Register Kafka Consumers as Background Services
        services.AddHostedService<ClassroomEventConsumer>();
        services.AddHostedService<UserEventConsumer>();
        services.AddHostedService<CrawlerEventConsumer>();
        services.AddHostedService<CourseRequestEventConsumer>();
        services.AddHostedService<ReportEventConsumer>();

        return services;
    }
}
