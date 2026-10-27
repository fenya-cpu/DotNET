namespace Reviews.Infrastructure.Persistence;

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Reviews.Domain.Entities;

public class MongoDbContext
{
    public IMongoDatabase Database { get; }

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        Database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<Review> Reviews => Database.GetCollection<Review>("reviews");
}