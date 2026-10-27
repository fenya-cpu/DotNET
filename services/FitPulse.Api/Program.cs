using FitPulse.Api.Data;
using FitPulse.Api.Middleware;
using FitPulse.Bll.Mappings;
using FitPulse.Bll.Services;
using FitPulse.Bll.Validators;
using FitPulse.Dal;
using FitPulse.Dal.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Логування (включно з логами EF Core SQL)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Додавання сервісів у DI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Unit of Work та Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductService, ProductService>();

// AutoMapper & FluentValidation
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<ProductProfile>();
});
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductDtoValidator>();

// ProblemDetails Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// 3. Конфігурація HTTP Pipeline
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// 4. Автоматичні міграції та програмний сідінг
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (!app.Environment.IsProduction())
    {
        await DbInitializer.SeedAsync(db);
    }
}

app.Run();