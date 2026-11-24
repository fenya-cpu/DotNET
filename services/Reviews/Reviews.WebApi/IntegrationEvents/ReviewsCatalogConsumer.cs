using MongoDB.Bson;
using Platform.EventBus;
using Platform.EventBus.Events;
using RabbitMQ.Client;
using System.Text.Json;

namespace Reviews.WebApi.IntegrationEvents;

public class ReviewsCatalogConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<ReviewsCatalogConsumer> logger)
    : RabbitMqConsumerBase(connection, scopeFactory, logger, Topology.ReviewsCatalogQueue)
{
    protected override async Task DispatchEventAsync(
        IServiceProvider serviceProvider,
        string? eventType,
        ReadOnlyMemory<byte> body,
        CancellationToken ct)
    {
        var handler = serviceProvider.GetRequiredService<ReviewsProductSyncHandler>();

        switch (eventType)
        {
            case nameof(ProductRenamedEvent):
                {
                    var evt = JsonSerializer.Deserialize<ProductRenamedEvent>(body.Span, JsonDefaults.Options)
                              ?? throw new JsonException("Empty ProductRenamedEvent payload");
                    await handler.HandleRenamedAsync(evt, ct);
                    break;
                }
            case nameof(ProductPriceChangedEvent):
                {
                    var evt = JsonSerializer.Deserialize<ProductPriceChangedEvent>(body.Span, JsonDefaults.Options)
                              ?? throw new JsonException("Empty ProductPriceChangedEvent payload");
                    await handler.HandlePriceChangedAsync(evt, ct);
                    break;
                }
            default:
                throw new JsonException($"Unsupported event type '{eventType}'");
        }
    }
}