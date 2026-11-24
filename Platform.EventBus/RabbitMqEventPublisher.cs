using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Platform.EventBus;

public class RabbitMqEventPublisher(IConnection connection, ILogger<RabbitMqEventPublisher> logger)
    : IEventPublisher
{
    public async Task PublishAsync<T>(T @event, string exchange, string routingKey, CancellationToken ct = default)
        where T : IntegrationEvent
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await Topology.DeclareTopologyAsync(channel, ct);

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = @event.EventId.ToString(),
            Type = typeof(T).Name,
            Headers = new Dictionary<string, object?>
            {
                ["x-correlation-id"] = @event.CorrelationId,
                ["x-schema-version"] = "1.0"
            }
        };

        TracePropagation.Inject(props);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, JsonDefaults.Options);
        await channel.BasicPublishAsync(exchange, routingKey, mandatory: false, props, body, ct);

        logger.LogInformation(
            "Published {EventType} {EventId} (CorrelationId: {CorrelationId}) to {Exchange}/{RoutingKey}",
            typeof(T).Name, @event.EventId, @event.CorrelationId, exchange, routingKey);
    }
}
