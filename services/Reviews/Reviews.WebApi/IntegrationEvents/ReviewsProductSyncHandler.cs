using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Platform.EventBus.Events;

namespace Reviews.WebApi.IntegrationEvents;

public class ProcessedEventDocument
{
    [BsonId]
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class ReviewsProductSyncHandler(
    IMongoClient mongoClient,
    IConfiguration configuration,
    ILogger<ReviewsProductSyncHandler> logger)
{
    private IMongoDatabase Database =>
        mongoClient.GetDatabase(configuration["MongoDb:DatabaseName"] ?? "ReviewsDb");

    public async Task HandleRenamedAsync(ProductRenamedEvent e, CancellationToken ct)
    {
        if (!await TryMarkEventProcessedAsync(e.EventId, nameof(ProductRenamedEvent), ct))
        {
            logger.LogInformation(
                "Duplicate event {EventId} ({EventType}) for Product {ProductId} skipped in Reviews",
                e.EventId, nameof(ProductRenamedEvent), e.ProductId);
            return;
        }

        // Оновлюємо денормалізовану назву productName в усіх документах відгуків про товар[cite: 14, 23]
        var reviewsCollection = Database.GetCollection<BsonDocument>("reviews");
        var filter = Builders<BsonDocument>.Filter.Eq("productId", e.ProductId);
        var update = Builders<BsonDocument>.Update.Set("productName", e.NewName);

        var result = await reviewsCollection.UpdateManyAsync(filter, update, cancellationToken: ct);

        logger.LogInformation(
            "Updated productName to '{NewName}' in {ModifiedCount} reviews for ProductId {ProductId} (EventId: {EventId})",
            e.NewName, result.ModifiedCount, e.ProductId, e.EventId);
    }

    public async Task HandlePriceChangedAsync(ProductPriceChangedEvent e, CancellationToken ct)
    {
        if (!await TryMarkEventProcessedAsync(e.EventId, nameof(ProductPriceChangedEvent), ct))
        {
            logger.LogInformation(
                "Duplicate event {EventId} ({EventType}) skipped in Reviews",
                e.EventId, nameof(ProductPriceChangedEvent));
            return;
        }

        logger.LogInformation(
            "Recorded ProductPriceChangedEvent {EventId} for ProductId {ProductId} in Reviews",
            e.EventId, e.ProductId);
    }

    private async Task<bool> TryMarkEventProcessedAsync(Guid eventId, string eventType, CancellationToken ct)
    {
        var collection = Database.GetCollection<ProcessedEventDocument>("processed_events");
        try
        {
            await collection.InsertOneAsync(
                new ProcessedEventDocument { EventId = eventId, EventType = eventType, ProcessedAt = DateTime.UtcNow },
                cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}