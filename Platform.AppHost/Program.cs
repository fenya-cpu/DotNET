var builder = DistributedApplication.CreateBuilder(args);

// 1. Контейнери Баз Даних
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", sqlPassword).WithDataVolume();
var mainDb = sql.AddDatabase("FitPulseDb");

var mongo = builder.AddMongoDB("mongo").WithDataVolume();
var reviewsDb = mongo.AddDatabase("ReviewsDb");

// 2. Контейнер Redis (з постійним сховищем)
var redis = builder.AddRedis("redis").WithDataVolume();

// 3. Мікросервіси
var fitpulseApi = builder.AddProject<Projects.FitPulse_Api>("fitpulse-api")
    .WithReference(mainDb)
    .WaitFor(mainDb)
    .WithReference(redis)
    .WaitFor(redis);

var reviewsApi = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb)
    .WaitFor(reviewsDb)
    .WithReference(redis)
    .WaitFor(redis);

// 4. Агрегатор
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(fitpulseApi)
    .WithReference(reviewsApi)
    .WithReference(redis)
    .WaitFor(fitpulseApi)
    .WaitFor(reviewsApi)
    .WaitFor(redis);

// 5. API Gateway (YARP, порт 5000)
builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(fitpulseApi)
    .WithReference(reviewsApi)
    .WithReference(aggregator)
    .WaitFor(aggregator);

builder.Build().Run();