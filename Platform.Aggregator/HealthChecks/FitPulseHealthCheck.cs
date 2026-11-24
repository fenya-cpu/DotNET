using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

public class FitPulseHealthCheck(Health.HealthClient client, ILogger<FitPulseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var reply = await client.CheckAsync(
                new HealthCheckRequest(),
                deadline: DateTime.UtcNow.AddSeconds(2),
                cancellationToken: ct);

            return reply.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy("fitpulse-api is serving")
                : HealthCheckResult.Unhealthy($"fitpulse-api status: {reply.Status}");
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "fitpulse-api gRPC health check failed");
            return HealthCheckResult.Unhealthy("fitpulse-api is unavailable", ex);
        }
    }
}