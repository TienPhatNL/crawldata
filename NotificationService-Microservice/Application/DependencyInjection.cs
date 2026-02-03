using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Services;

namespace NotificationService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR for CQRS pattern
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        
        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register HttpContextAccessor for CurrentUserService
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // SignalR for real-time in-app notifications
        services.AddSignalR();

        return services;
    }
}
