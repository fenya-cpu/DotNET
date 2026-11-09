using FitPulse.Grpc;
using Platform.Aggregator.Services;
using Reviews.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 1. Реєстрація gRPC-клієнтів із Service Discovery[cite: 17, 25]
builder.Services.AddGrpcClient<FitPulseService.FitPulseServiceClient>(o =>
{
    o.Address = new Uri("http://fitpulse-api");
});

builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(o =>
{
    o.Address = new Uri("http://reviews-api");
});

// 2. Distributed Cache поверх Redis[cite: 14, 25]
builder.AddRedisDistributedCache("redis");

// 3. Реєстрація сервісу агрегації
builder.Services.AddScoped<ProductAggregatorService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseRouting();
app.MapControllers();

app.Run();