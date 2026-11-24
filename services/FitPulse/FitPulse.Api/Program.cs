using FitPulse.Api.Middleware;
using FitPulse.Bll.Caching;
using FitPulse.Bll.Profiles;
using FitPulse.Bll.Services;
using FitPulse.Dal.Interfaces;
using FitPulse.Dal.UnitOfWork;
using Platform.ServiceDefaults;
using FitPulse.Api.Grpc;
using FitPulse.Api.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.EventBus;
using Serilog;


var builder = WebApplication.CreateBuilder(args);
// Додати перед builder.Build():
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<LoggingInterceptor>();
});

builder.Services.AddSingleton<FitPulseMetrics>();
// 1. Local In-Memory Cache[cite: 12]
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 10_000;
});

// 2. Distributed Redis Cache (Aspire)[cite: 14]
builder.AddRedisDistributedCache("redis");
var healthChecksBuilder = builder.Services.AddHealthChecks();

var sqlConn = builder.Configuration.GetConnectionString("FitPulseDb")
              ?? builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(sqlConn))
{
    healthChecksBuilder.AddSqlServer(
        connectionString: sqlConn,
        name: "fitpulse-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["database", "sql", "ready"],
        timeout: TimeSpan.FromSeconds(5));
}

var redisConn = builder.Configuration.GetConnectionString("redis");
if (!string.IsNullOrWhiteSpace(redisConn))
{
    healthChecksBuilder.AddRedis(
        redisConnectionString: redisConn,
        name: "redis-cache",
        failureStatus: HealthStatus.Degraded, // Кеш необов'язковий: сервіс працює через БД[cite: 10, 16]
        tags: ["cache", "redis", "ready"],
        timeout: TimeSpan.FromSeconds(3));
}

builder.Services.AddGrpcHealthChecks();
// 3. Register Caching Services & Metrics
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddSingleton<TwoLevelCacheService>();
// 1. Serilog Logging
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// 2. Connection String
var connectionString = builder.Configuration.GetConnectionString("fitpulse-db")
    ?? throw new InvalidOperationException("Connection string 'fitpulse-db' is missing");

// 3. DI Registrations (Scoped)
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<IBookingService, BookingService>();
// 4. AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<BookingProfile>());

// 5. Global ProblemDetails Exception Handling

builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.AddRabbitMQClient("messaging");

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.MapGrpcHealthChecksService();
app.MapGrpcService<FitPulseGrpcService>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();