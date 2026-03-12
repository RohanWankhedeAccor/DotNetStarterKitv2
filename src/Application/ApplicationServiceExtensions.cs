using Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

/// <summary>
/// Provides the <see cref="AddApplication"/> extension method that registers all
/// Application layer services with the ASP.NET Core dependency injection container.
///
/// Called once from <c>Program.cs</c> in the Api project:
/// <code>
/// builder.Services.AddApplication();
/// </code>
///
/// This extension handles:
/// - MediatR command/query handlers
/// - FluentValidation validators
/// - MediatR pipeline behaviors (ValidationBehavior, LoggingBehavior)
/// - AutoMapper profile registration
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers all Application layer services: MediatR handlers, validators,
    /// pipeline behaviors, and AutoMapper profiles.
    /// </summary>
    /// <param name="services">The ASP.NET Core service collection.</param>
    /// <returns>The <paramref name="services"/> collection for fluent chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register FluentValidation: discovers all IValidator<T> implementations manually.
        // This must be done before MediatR registration so validators are available
        // to the ValidationBehavior pipeline behavior.
        var assembly = typeof(ApplicationServiceExtensions).Assembly;
        var validatorType = typeof(FluentValidation.IValidator<>);
        var validatorAssignableType = typeof(FluentValidation.IValidator);

        foreach (var type in assembly.GetTypes())
        {
            // Find all classes that implement IValidator<T>
            var interfaces = type.GetInterfaces();
            var implementsValidator = interfaces.Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == validatorType);

            if (implementsValidator && !type.IsAbstract)
            {
                // Register the validator with its interface
                var serviceInterface = interfaces.First(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == validatorType);
                services.AddScoped(serviceInterface, type);
            }
        }

        // Register MediatR: discovers and registers all handlers and behaviors in this assembly.
        services.AddMediatR(config =>
        {
            // Scan this assembly for IRequestHandler<,> implementations.
            config.RegisterServicesFromAssembly(assembly);

            // Register global pipeline behaviors in order.
            // 1. ValidationBehavior runs first — rejects invalid requests early.
            // 2. LoggingBehavior wraps the handler — logs start/stop and timing.
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        // Register AutoMapper: discovers all Profile implementations.
        services.AddAutoMapper(assembly);

        return services;
    }
}
