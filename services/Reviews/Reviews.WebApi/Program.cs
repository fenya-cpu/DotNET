using Reviews.Application;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Seeding;
using Reviews.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Додавання шарів Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

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