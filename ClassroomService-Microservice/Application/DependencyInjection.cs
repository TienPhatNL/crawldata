using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Common.Services;

namespace ClassroomService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Register HttpContextAccessor for CurrentUserService
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        // Register TopicWeightValidator for domain validation
        services.AddScoped<ClassroomService.Domain.Services.TopicWeightValidator>();

        // Register Grade Export Service
        services.AddScoped<Services.IGradeExportService, Services.GradeExportService>();

        // Register Term Access Validator
        services.AddScoped<Features.Dashboard.Helpers.TermAccessValidator>();

        // SignalR for real-time chat-based crawler communication
        services.AddSignalR();

        // Kafka Consumer Configuration for crawler event streaming
        services.Configure<Messaging.KafkaConsumerSettings>(options =>
        {
            var kafkaSettings = configuration.GetSection(Messaging.KafkaConsumerSettings.SectionName);
            kafkaSettings.Bind(options);

            // Use appsettings.json configuration (localhost:9092 for host-based services)
            // Aspire's GetConnectionString("kafka") resolves to host.docker.internal which doesn't work for host services
            Console.WriteLine($"[Kafka Config] Crawler Consumer using appsettings.json: {options.BootstrapServers}");
        });

        // Register Kafka consumer as background service
        services.AddHostedService<Messaging.CrawlerEventConsumer>();

        return services;
    }
}