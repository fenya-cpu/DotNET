using System.Text.Json;
using Platform.EventBus;
using Platform.EventBus.Events;
using RabbitMQ.Client;

namespace FitPulse.Api.IntegrationEvents;

public class OrdersCatalogConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<OrdersCatalogConsumer> logger)
    : RabbitMqConsumerBase(connection, scopeFactory, logger, Topology.OrdersCatalogQueue)
{
    protected override async Task DispatchEventAsync(
        IServiceProvider serviceProvider,
        string? eventType,
        ReadOnlyMemory<byte> body,
        CancellationToken ct)
    {
        var handler = serviceProvider.GetRequiredService<OrdersProductSyncHandler>();

        switch (eventType)
        {
            case nameof(ProductPriceChangedEvent):
                {
                    var evt = JsonSerializer.Deserialize<ProductPriceChangedEvent>(body.Span, JsonDefaults.Options)
                              ?? throw new JsonException("Empty ProductPriceChangedEvent payload");
                    await handler.HandlePriceChangedAsync(evt, ct);
                    break;
                }
            case nameof(ProductRenamedEvent):
                {
                    var evt = JsonSerializer.Deserialize<ProductRenamedEvent>(body.Span, JsonDefaults.Options)
                              ?? throw new JsonException("Empty ProductRenamedEvent payload");
                    await handler.HandleRenamedAsync(evt, ct);
                    break;
                }
            default:
                throw new JsonException($"Unsupported event type '{eventType}'");
        }
    }
}