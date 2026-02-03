using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Reflection;
using WebCrawlerService.Application.Common.Behaviors;
using WebCrawlerService.Application.Common.Interfaces;
using WebCrawlerService.Application.Configuration;
using WebCrawlerService.Application.Services;
using WebCrawlerService.Application.Services.Charts;
using WebCrawlerService.Application.Workers;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Infrastructure.Repositories;

namespace WebCrawlerService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // MediatR for CQRS
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Application Services
        // TODO: Implement these service classes
        // services.AddScoped<ICrawlerOrchestrationService, CrawlerOrchestrationService>();
        services.AddScoped<IDomainValidationService, DomainValidationService>();
        services.AddMemoryCache();
        services.AddScoped<Common.Interfaces.ICacheService, InMemoryCacheService>();
        // services.AddScoped<IJobSchedulingService, JobSchedulingService>();
        services.AddScoped<ICrawlerMonitoringService, CrawlerMonitoringService>();

        // Chart and Analytics Services
        services.AddScoped<IChartDataService, ChartDataService>();
        services.AddScoped<Services.DataVisualization.IDataVisualizationService, Services.DataVisualization.DataVisualizationService>();
        services.AddScoped<Services.DataVisualization.ICrawlSummaryService, Services.DataVisualization.CrawlSummaryService>();

        // Intelligent Crawl Services
        services.Configure<LlmConfiguration>(configuration.GetSection("LlmConfiguration"));
        services.Configure<Configuration.Crawl4AIOptions>(configuration.GetSection(Configuration.Crawl4AIOptions.SectionName));
        services.Configure<GoogleSearchOptions>(configuration.GetSection(GoogleSearchOptions.SectionName));
        services.AddScoped<ILlmService, LlmService>();
        services.AddScoped<IPromptAnalyzerService, PromptAnalyzerService>();
        services.AddHttpClient<IGoogleProductSearchService, GoogleProductSearchService>();

        // HTML Processing and AI Services
        services.AddScoped<IHtmlCleaningService, HtmlCleaningService>();
        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<IAiResponseGenerationService, AiResponseGenerationService>();

        // Repository Interfaces and Implementations
        // services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICrawlJobRepository, CrawlJobRepository>();
        services.AddScoped<ICrawlerAgentRepository, CrawlerAgentRepository>();
        services.AddScoped<IDomainPolicyRepository, DomainPolicyRepository>();
        services.AddScoped<ICrawlResultRepository, CrawlResultRepository>();
        services.AddScoped<ICrawlTemplateRepository, CrawlTemplateRepository>();

        // Unit of Work Pattern
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Background Services
        // TODO: Implement these worker classes
        // services.AddHostedService<CrawlerWorker>();
        // services.AddHostedService<JobProcessorWorker>();
        // services.AddHostedService<HealthCheckWorker>();

        // Kafka Consumers
        services.AddHostedService<Messaging.SmartCrawlRequestConsumer>();
        services.AddHostedService<Messaging.CrawlJobResultConsumer>();

        // SignalR for real-time updates
        services.AddSignalR();

        // gRPC Services
        services.AddGrpc();

        // HTTP Context Accessor
        services.AddHttpContextAccessor();

        // Swagger/OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "WebCrawlerService API",
                Version = "v1"
            });

            // Include XML comments for better documentation
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // Add JWT Authentication to Swagger
            c.AddSecurityDefinition("Bearer", new()
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Group operations by controller
            c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
            c.DocInclusionPredicate((name, api) => true);
        });

        return services;
    }
}
