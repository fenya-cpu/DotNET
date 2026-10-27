namespace Reviews.Infrastructure.Seeding;

using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using global::Reviews.Domain.Entities;
using global::Reviews.Infrastructure.Persistence;

public class IndexCreationService : IHostedService
{
    private readonly MongoDbContext _context;

    public IndexCreationService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var keys = Builders<Review>.IndexKeys;
        await _context.Reviews.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Review>(
                keys.Ascending(r => r.ProductId).Descending(r => r.CreatedAt)),
            new CreateIndexModel<Review>(
                keys.Text(r => r.Text)),
            new CreateIndexModel<Review>(
                keys.Ascending(r => r.ProductId).Ascending(r => r.Author.UserId),
                new CreateIndexOptions { Unique = true })
        }, ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}