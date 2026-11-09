var builder = DistributedApplication.CreateBuilder(args);

// 1. Контейнери Баз Даних
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", sqlPassword).WithDataVolume();
var mainDb = sql.AddDatabase("FitPulseDb");

var mongo = builder.AddMongoDB("mongo").WithDataVolume();
var reviewsDb = mongo.AddDatabase("ReviewsDb");

// 2. Ваші мікросервіси
var fitpulseApi = builder.AddProject<Projects.FitPulse_Api>("fitpulse-api")
    .WithReference(mainDb)
    .WaitFor(mainDb);

var reviewsApi = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb)
    .WaitFor(reviewsDb);

// 3. Агрегатор
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(fitpulseApi)
    .WithReference(reviewsApi)
    .WaitFor(fitpulseApi)
    .WaitFor(reviewsApi);

// 4. API Gateway (Порт 5000)
builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(fitpulseApi)
    .WithReference(reviewsApi)
    .WithReference(aggregator)
    .WaitFor(aggregator);

builder.Build().Run();