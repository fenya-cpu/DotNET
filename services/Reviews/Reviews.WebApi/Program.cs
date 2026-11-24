using Reviews.Application;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Seeding;
using Reviews.WebApi.Middleware;
using Platform.ServiceDefaults;
using Reviews.WebApi.Grpc;
using Reviews.WebApi.IntegrationEvents;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<LoggingInterceptor>();
});
var reviewsHealth = builder.Services.AddHealthChecks();
var mongoConn = builder.Configuration.GetConnectionString("ReviewsDb")
                ?? builder.Configuration.GetConnectionString("mongodb");
if (!string.IsNullOrWhiteSpace(mongoConn))
{
    reviewsHealth.AddMongoDb(
            sp => new MongoClient(mongoConn),
            name: "reviews-db",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["database", "mongodb", "ready"],
            timeout: TimeSpan.FromSeconds(5));
}

var reviewsRedisConn = builder.Configuration.GetConnectionString("redis");
if (!string.IsNullOrWhiteSpace(reviewsRedisConn))
{
    reviewsHealth.AddRedis(
        redisConnectionString: reviewsRedisConn,
        name: "redis-cache",
        failureStatus: HealthStatus.Degraded,
        tags: ["cache", "redis", "ready"],
        timeout: TimeSpan.FromSeconds(3));
}

builder.Services.AddGrpcHealthChecks();
// Додавання шарів Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.AddRabbitMQClient("messaging");
builder.Services.AddScoped<ReviewsProductSyncHandler>();
builder.Services.AddHostedService<ReviewsCatalogConsumer>();

var app = builder.Build();
app.MapGrpcHealthChecksService();
app.MapGrpcService<ReviewsGrpcService>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Автоматичний сідінг даних при старті
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await ReviewSeeder.SeedAsync(context);
}

app.Run();