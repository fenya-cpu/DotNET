namespace Reviews.Application;

using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using global::Reviews.Application.Common.Behaviors;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}