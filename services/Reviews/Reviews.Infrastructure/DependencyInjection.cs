namespace Reviews.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Interfaces;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Seeding;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        MongoMappings.Register();

        services.Configure<MongoDbOptions>(config.GetSection("MongoDb"));
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewQueries, ReviewQueries>();
        services.AddHostedService<IndexCreationService>();

        return services;
    }
}