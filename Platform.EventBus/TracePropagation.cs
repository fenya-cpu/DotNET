using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus;

public static class TracePropagation
{
    public static readonly ActivitySource Source = new("Platform.Messaging");
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public static void Inject(BasicProperties props)
    {
        if (Activity.Current is not { } activity) return;

        props.Headers ??= new Dictionary<string, object?>();
        Propagator.Inject(
            new PropagationContext(activity.Context, Baggage.Current),
            props.Headers,
            (headers, key, value) => headers[key] = value);
    }

    public static Activity? StartConsumerActivity(BasicDeliverEventArgs ea, string queue)
    {
        var parent = Propagator.Extract(
            default,
            ea.BasicProperties.Headers,
            (headers, key) => headers is not null && headers.TryGetValue(key, out var v) && v is byte[] bytes
                ? [Encoding.UTF8.GetString(bytes)]
                : []);

        var activity = Source.StartActivity($"{queue} process", ActivityKind.Consumer, parent.ActivityContext);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", queue);
        activity?.SetTag("messaging.message.id", ea.BasicProperties.MessageId);
        if (!string.IsNullOrEmpty(ea.BasicProperties.Type))
        {
            activity?.SetTag("messaging.message.type", ea.BasicProperties.Type);
        }

        return activity;
    }
}