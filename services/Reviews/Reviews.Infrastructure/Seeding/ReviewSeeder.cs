namespace Reviews.Infrastructure.Seeding;

using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;

public static class ReviewSeeder
{
    public static async Task SeedAsync(MongoDbContext context, CancellationToken ct = default)
    {
        if (await context.Reviews.EstimatedDocumentCountAsync(cancellationToken: ct) > 0)
            return; // Вже наповнено

        var reviews = new[]
        {
            new Review(1, "Whey Protein Isolate 1kg", new Author("u-101", "Олексій"), 5, "Чудова якість та смак!"),
            new Review(1, "Whey Protein Isolate 1kg", new Author("u-102", "Марія"), 4, "Добре розчиняється.")
        };

        await context.Reviews.InsertManyAsync(reviews, cancellationToken: ct);
    }
}