using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StackExchange.Redis;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Services;
using ClassroomService.Infrastructure.Common;
using ClassroomService.Infrastructure.Repositories;
using ClassroomService.Infrastructure.Caching;
using Amazon.S3;
using Amazon.Runtime;

namespace ClassroomService.Infrastructure;

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
                        // Extract token from Authorization header (HTTP requests)
                        if (context.Request.Headers.ContainsKey("Authorization"))
                        {
                            var authHeader = context.Request.Headers["Authorization"].ToString();
                            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Token = authHeader.Substring("Bearer ".Length).Trim();
                            }
                        }

                        // Extract token from query string (SignalR WebSocket connections)
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

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
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseEnrollmentRepository, CourseEnrollmentRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();
        services.AddScoped<ITermRepository, TermRepository>();
        services.AddScoped<ICourseCodeRepository, CourseCodeRepository>();
        services.AddScoped<ICourseRequestRepository, CourseRequestRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<ICrawlerChatMessageRepository, CrawlerChatMessageRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportHistoryRepository, ReportHistoryRepository>();
        services.AddScoped<IReportAICheckRepository, ReportAICheckRepository>();
        services.AddScoped<IConversationCrawlDataRepository, ConversationCrawlDataRepository>();
        services.AddScoped<ITemplateFileRepository, TemplateFileRepository>();

        // Register AI Detection Service
        services.Configure<AIDetectionSettings>(configuration.GetSection("AIDetection"));
        services.AddHttpClient<IAIDetectionService, ZeroGPTAIDetectionService>();

        // Register services
        services.AddScoped<IAccessCodeService, AccessCodeService>();
        services.AddScoped<ICourseNameGenerationService, CourseNameGenerationService>();
        services.AddScoped<ICourseUniqueCodeService, CourseUniqueCodeService>();
        services.AddScoped<IExcelService, ExcelService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<ReportHistoryService>();
        
        // Register TopicWeightService for grade calculation
        services.AddScoped<ITopicWeightService, TopicWeightService>();
        
        // Report Collaboration Services
        services.AddScoped<IReportCollaborationBufferService, ReportCollaborationBufferService>();
        services.AddScoped<IReportManualSaveService, ReportManualSaveService>();
        services.AddScoped<IChangeTrackingService, ChangeTrackingService>();
        services.AddHostedService<ClassroomService.Infrastructure.BackgroundServices.ReportVersionCreationService>();
        
        // Background Services for Assignment Management
        services.AddHostedService<ClassroomService.Infrastructure.BackgroundServices.AssignmentCompletionService>();

        // AWS S3 Configuration
        var awsOptions = configuration.GetAWSOptions();
        awsOptions.Credentials = new BasicAWSCredentials(
            configuration["AWS:AccessKey"] ?? throw new InvalidOperationException("AWS AccessKey not configured"),
            configuration["AWS:SecretKey"] ?? throw new InvalidOperationException("AWS SecretKey not configured")
        );

        services.AddAWSService<IAmazonS3>(awsOptions);

        // Register Upload Service
        services.AddScoped<IUploadService, UploadService>();

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
                options.InstanceName = "classroom:";
            });
        }
        else
        {
            // Fallback to appsettings.json for local dev without Aspire
            Console.WriteLine("[Redis Config] WARNING: No Redis connection string found. Collaboration features will not work.");
            Console.WriteLine("[Redis Config] Run via Aspire AppHost or add 'redis' connection string to appsettings.Development.json");
        }

        // Redis Cache Service with stampede prevention
        services.AddScoped<IUserInfoCacheService, UserInfoCacheService>();

        // Kafka Configuration
        services.Configure<ClassroomService.Domain.Common.KafkaSettings>(options =>
        {
            var kafkaSettings = configuration.GetSection(ClassroomService.Domain.Common.KafkaSettings.SectionName);
            kafkaSettings.Bind(options);

            // Use appsettings.json configuration (localhost:9092 for host-based services)
            // Aspire's GetConnectionString("kafka") resolves to host.docker.internal which doesn't work for host services
            Console.WriteLine($"[Kafka Config] Using appsettings.json: {options.BootstrapServers}");
        });

        // Register Kafka services
        services.AddSingleton<ClassroomService.Infrastructure.Messaging.KafkaEventPublisher>();
        services.AddSingleton<ClassroomService.Infrastructure.Messaging.MessageCorrelationManager>();
        services.AddHostedService<ClassroomService.Infrastructure.Messaging.ClassroomKafkaConsumer>();
        services.AddHostedService<ClassroomService.Infrastructure.Messaging.CacheInvalidationConsumer>();

        // Register Kafka-based UserService
        services.AddScoped<IKafkaUserService, KafkaUserService>();

        // Add HttpClient for WebCrawlerService integration
        services.AddHttpClient<ICrawlerIntegrationService, CrawlerIntegrationService>(client =>
        {
            var crawlerServiceUrl = configuration["Services:WebCrawlerService:BaseUrl"] ?? "http://localhost:5014";
            client.BaseAddress = new Uri(crawlerServiceUrl);
            client.Timeout = TimeSpan.FromMinutes(5); // Extended timeout for crawl operations
        });

        // Register RAG and Data Normalization Services
        services.AddScoped<IDataValidator, DataValidationService>();
        services.AddScoped<ICrawlDataNormalizationService, CrawlDataNormalizationService>();
        
        // Register Vector Embedding Service with HttpClient
        services.AddHttpClient<IVectorEmbeddingService, VectorEmbeddingService>();

        // Register Topic Weight Management Services
        services.AddScoped<ITermService, TermService>();
        services.AddScoped<ITopicWeightValidationService, TopicWeightValidationService>();
        services.AddScoped<ITopicWeightHistoryService, TopicWeightHistoryService>();

        return services;
    }
}