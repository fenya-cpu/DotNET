using FitPulse.Grpc;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Platform.Aggregator.Services;
using Polly;
using Reviews.Grpc;
using Grpc.Health.V1;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.Aggregator.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddGrpcClient<Health.HealthClient>("FitPulseHealth", o =>
{
    o.Address = new Uri("http://fitpulse-api");
});

builder.Services.AddGrpcClient<Health.HealthClient>("ReviewsHealth", o =>
{
    o.Address = new Uri("http://reviews-api");
});

var aggregatorHealth = builder.Services.AddHealthChecks()
    .AddCheck<FitPulseHealthCheck>(
        "fitpulse-api",
        tags: ["grpc", "downstream", "ready"])
    .AddCheck<ReviewsHealthCheck>(
        "reviews-api",
        tags: ["grpc", "downstream", "ready"]);

// Спільна нативна конфігурація gRPC Retry (повторює статуси Unavailable / ResourceExhausted)
var defaultGrpcMethodConfig = new MethodConfig
{
    Names = { MethodName.Default },
    RetryPolicy = new RetryPolicy
    {
        MaxAttempts = 4,
        InitialBackoff = TimeSpan.FromSeconds(0.5),
        MaxBackoff = TimeSpan.FromSeconds(4),
        BackoffMultiplier = 2,
        RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.ResourceExhausted }
    }
};
// 1. КРИТИЧНИЙ СЕРВІС: FitPulse
// Очищаємо стандартний обробник із ServiceDefaults без нашарування
builder.Services.Configure<Microsoft.Extensions.Http.HttpClientFactoryOptions>(
    nameof(FitPulseService.FitPulseServiceClient),
    o => o.HttpMessageHandlerBuilderActions.Clear());

builder.Services.AddHttpClient(nameof(FitPulseService.FitPulseServiceClient))
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 4;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;

        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(4);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);

        options.CircuitBreaker.FailureRatio = 0.3;
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(15);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        options.CircuitBreaker.OnOpened = args =>
        {
            Console.WriteLine($"[CircuitBreaker] fitpulse-api OPENED for {args.BreakDuration}");
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnClosed = _ =>
        {
            Console.WriteLine("[CircuitBreaker] fitpulse-api CLOSED");
            return ValueTask.CompletedTask;
        };
    });

builder.Services.Configure<Microsoft.Extensions.Http.HttpClientFactoryOptions>(
    nameof(ReviewsService.ReviewsServiceClient),
    o => o.HttpMessageHandlerBuilderActions.Clear());

builder.Services.AddHttpClient(nameof(ReviewsService.ReviewsServiceClient))
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 1;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.Retry.BackoffType = DelayBackoffType.Constant;
        options.Retry.UseJitter = true;

        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);

        options.CircuitBreaker.FailureRatio = 0.7;
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(10);
        options.CircuitBreaker.OnOpened = args =>
        {
            Console.WriteLine($"[CircuitBreaker] reviews-api OPENED for {args.BreakDuration}");
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnClosed = _ =>
        {
            Console.WriteLine("[CircuitBreaker] reviews-api CLOSED");
            return ValueTask.CompletedTask;
        };
    });

builder.Services.AddTransient(sp =>
{
    var factory = sp.GetRequiredService<GrpcClientFactory>();
    var client = factory.CreateClient<Health.HealthClient>("FitPulseHealth");
    var logger = sp.GetRequiredService<ILogger<FitPulseHealthCheck>>();
    return new FitPulseHealthCheck(client, logger);
});

builder.Services.AddTransient(sp =>
{
    var factory = sp.GetRequiredService<GrpcClientFactory>();
    var client = factory.CreateClient<Health.HealthClient>("ReviewsHealth");
    var logger = sp.GetRequiredService<ILogger<ReviewsHealthCheck>>();
    return new ReviewsHealthCheck(client, logger);
});

var aggRedisConn = builder.Configuration.GetConnectionString("redis");
if (!string.IsNullOrWhiteSpace(aggRedisConn))
{
    aggregatorHealth.AddRedis(
        redisConnectionString: aggRedisConn,
        name: "redis-cache",
        failureStatus: HealthStatus.Degraded,
        tags: ["cache", "redis", "ready"],
        timeout: TimeSpan.FromSeconds(3));
}

// 3. Distributed Cache поверх Redis
builder.AddRedisDistributedCache("redis");

// 4. Реєстрація сервісу агрегації
builder.Services.AddScoped<ProductAggregatorService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseRouting();
app.MapControllers();

app.Run();