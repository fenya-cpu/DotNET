using RabbitMQ.Client;

namespace Platform.EventBus;

public static class Topology
{
    public const string CatalogExchange = "catalog-events";
    public const string CatalogDlx = "catalog-events.dlx";

    public const string OrdersCatalogQueue = "orders.catalog-sync";
    public const string OrdersCatalogDlq = "orders.catalog-sync.dlq";

    public const string ReviewsCatalogQueue = "reviews.catalog-sync";
    public const string ReviewsCatalogDlq = "reviews.catalog-sync.dlq";

    public static async Task DeclareTopologyAsync(IChannel ch, CancellationToken ct)
    {
        // Основний topic exchange для подій каталогу та fanout DLX для відхилених повідомлень
        await ch.ExchangeDeclareAsync(CatalogExchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await ch.ExchangeDeclareAsync(CatalogDlx, ExchangeType.Fanout, durable: true, cancellationToken: ct);

        // Черга та DLQ для сервісу замовлень (або FitPulse, що зберігає копії товарів)
        await ch.QueueDeclareAsync(
            OrdersCatalogQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = CatalogDlx },
            cancellationToken: ct);
        await ch.QueueBindAsync(OrdersCatalogQueue, CatalogExchange, routingKey: "product.*", cancellationToken: ct);

        await ch.QueueDeclareAsync(OrdersCatalogDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await ch.QueueBindAsync(OrdersCatalogDlq, CatalogDlx, routingKey: "", cancellationToken: ct);

        // Черга та DLQ для сервісу відгуків (оновлює денормалізоване поле productName)
        await ch.QueueDeclareAsync(
            ReviewsCatalogQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = CatalogDlx },
            cancellationToken: ct);
        await ch.QueueBindAsync(ReviewsCatalogQueue, CatalogExchange, routingKey: "product.*", cancellationToken: ct);

        await ch.QueueDeclareAsync(ReviewsCatalogDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await ch.QueueBindAsync(ReviewsCatalogDlq, CatalogDlx, routingKey: "", cancellationToken: ct);
    }
}