using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

public class ReviewsHealthCheck(Health.HealthClient client, ILogger<ReviewsHealthCheck> logger) : IHealthCheck
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
                ? HealthCheckResult.Healthy("reviews-api is serving")
                : HealthCheckResult.Degraded($"reviews-api status: {reply.Status}"); // Некритичний сервіс
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "reviews-api gRPC health check failed");
            return HealthCheckResult.Degraded("reviews-api is unavailable", ex);
        }
    }
}