var builder = DistributedApplication.CreateBuilder(args);

var fitpulseDb = builder.AddConnectionString("fitpulse-db");

builder.AddProject<Projects.FitPulse_Api>("fitpulse-api")
    .WithReference(fitpulseDb);

builder.Build().Run();