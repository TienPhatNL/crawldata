using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Reflection;
using UserService.Application.Common.Interfaces;
using UserService.Application.Common.Services;
using UserService.Application.Common.Behaviors;
using UserService.Domain.Services;

namespace UserService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR for CQRS
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // MediatR Behaviors
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Application Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDomainEventService, DomainEventService>();
        services.AddScoped<Domain.Interfaces.IStudentAccountService, Services.StudentAccountService>();

        // HTTP Context Accessor
        services.AddHttpContextAccessor();

        return services;
    }
}