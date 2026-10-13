using Serilog;
using FitPulse.Api.Middleware;
using FitPulse.Bll.Profiles;
using FitPulse.Bll.Services;
using FitPulse.Dal.Interfaces;
using FitPulse.Dal.UnitOfWork;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();