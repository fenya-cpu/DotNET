using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus;

public abstract class RabbitMqConsumerBase(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger logger,
    string queueName,
    int maxRetries = 3) : BackgroundService
{
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await Topology.DeclareTopologyAsync(_channel, stoppingToken);

        // Обмежуємо кількість одночасно оброблюваних непідтверджених повідомлень[cite: 12]
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageAsync;

        // Ручне підтвердження (autoAck: false)[cite: 12, 14]
        await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        logger.LogInformation("Started RabbitMQ consumer on queue {QueueName}", queueName);

        // Очікуємо зупинки застосунку через stoppingToken[cite: 12]
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Stopping RabbitMQ consumer on queue {QueueName}", queueName);
        }
    }

    protected abstract Task DispatchEventAsync(
        IServiceProvider serviceProvider,
        string? eventType,
        ReadOnlyMemory<byte> body,
        CancellationToken ct);

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        using var activity = TracePropagation.StartConsumerActivity(ea, queueName);

        var messageId = ea.BasicProperties.MessageId ?? "unknown";
        var eventType = ea.BasicProperties.Type;
        var correlationId = ExtractHeaderString(ea.BasicProperties.Headers, "x-correlation-id");

        using var logScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["QueueName"] = queueName,
            ["MessageId"] = messageId,
            ["EventType"] = eventType,
            ["CorrelationId"] = correlationId
        });

        try
        {
            logger.LogInformation(
                "Received message {MessageId} of type {EventType} from {QueueName} (CorrelationId: {CorrelationId})",
                messageId, eventType, queueName, correlationId);

            await using var scope = scopeFactory.CreateAsyncScope();
            await DispatchEventAsync(scope.ServiceProvider, eventType, ea.Body, CancellationToken.None);

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);

            logger.LogInformation(
                "Successfully processed and ACKed message {MessageId} ({EventType}) from {QueueName}",
                messageId, eventType, queueName);
        }
        catch (JsonException ex)
        {
            // Постійний збій (некоректний JSON або несумісна схема) — одразу в DLQ без повторів[cite: 13, 14, 17]
            logger.LogError(
                ex,
                "Malformed JSON in message {MessageId} ({EventType}) from {QueueName}, NACKing without requeue to DLQ",
                messageId, eventType, queueName);

            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            // Тимчасовий збій — повторні спроби з лічильником x-retry-count[cite: 14, 17]
            await RetryOrDeadLetterAsync(ea, messageId, eventType, ex);
        }
    }

    private async Task RetryOrDeadLetterAsync(
        BasicDeliverEventArgs ea,
        string messageId,
        string? eventType,
        Exception ex)
    {
        var headers = ea.BasicProperties.Headers ?? new Dictionary<string, object?>();
        var attempt = headers.TryGetValue("x-retry-count", out var v) ? Convert.ToInt32(v) : 0;

        if (attempt >= maxRetries)
        {
            logger.LogError(
                ex,
                "Message {MessageId} ({EventType}) failed after {Attempts} attempts, NACKing without requeue to DLQ",
                messageId, eventType, attempt);

            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        // Перепублікуємо зі збільшеним x-retry-count та підтверджуємо оригінал[cite: 17]
        var nextAttempt = attempt + 1;
        var props = new BasicProperties(ea.BasicProperties)
        {
            Headers = new Dictionary<string, object?>(headers) { ["x-retry-count"] = nextAttempt }
        };

        await _channel!.BasicPublishAsync(ea.Exchange, ea.RoutingKey, mandatory: false, props, ea.Body);
        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

        logger.LogWarning(
            ex,
            "Transient failure processing message {MessageId} ({EventType}), republished for retry {Attempt}/{MaxRetries}",
            messageId, eventType, nextAttempt, maxRetries);
    }

    private static string? ExtractHeaderString(IDictionary<string, object?>? headers, string key)
    {
        if (headers is null || !headers.TryGetValue(key, out var val) || val is null) return null;
        return val is byte[] bytes ? Encoding.UTF8.GetString(bytes) : val.ToString();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }
    }
}