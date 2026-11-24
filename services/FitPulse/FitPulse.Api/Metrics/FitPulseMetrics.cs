using System.Diagnostics.Metrics;

namespace FitPulse.Api.Metrics;

public class FitPulseMetrics
{
    private readonly Counter<long> _workoutsRequested;
    private readonly Histogram<double> _operationDuration;
    private long _activeSessionsCount;

    public FitPulseMetrics(IMeterFactory meterFactory)
    {
        // Назва Meter відповідає сервісу (зареєстрована у Platform.ServiceDefaults)
        var meter = meterFactory.Create("FitPulse.Api");

        // 1. Лічильник бізнес-операцій (Counter)
        _workoutsRequested = meter.CreateCounter<long>(
            name: "fitpulse.workouts.requested",
            unit: "{request}",
            description: "Number of workout/product requests processed");

        // 2. Гістограма тривалості операцій (Histogram)[cite: 13]
        _operationDuration = meter.CreateHistogram<double>(
            name: "fitpulse.operation.duration",
            unit: "ms",
            description: "Duration of FitPulse business operations");

        // 3. Поточний стан (ObservableGauge)[cite: 14]
        meter.CreateObservableGauge(
            name: "fitpulse.active_sessions",
            observeValue: () => Interlocked.Read(ref _activeSessionsCount),
            unit: "{session}",
            description: "Current number of active workout sessions");
    }

    // Низька кардинальність тегів: лише фіксовані статуси/типи[cite: 14]
    public void RecordWorkoutRequest(string source, string status) =>
        _workoutsRequested.Add(1,
            new KeyValuePair<string, object?>("source", source),
            new KeyValuePair<string, object?>("status", status));

    public void RecordOperationDuration(string operation, bool success, double durationMs) =>
        _operationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));

    public void IncrementActiveSessions() => Interlocked.Increment(ref _activeSessionsCount);
    public void DecrementActiveSessions() => Interlocked.Decrement(ref _activeSessionsCount);
}