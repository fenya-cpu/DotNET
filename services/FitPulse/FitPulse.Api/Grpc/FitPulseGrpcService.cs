using FitPulse.Bll.Caching;
using FitPulse.Grpc;
using Grpc.Core;
using System.Diagnostics;
using FitPulse.Api.Metrics;

namespace FitPulse.Api.Grpc;

public class FitPulseGrpcService : FitPulseService.FitPulseServiceBase
{
    private readonly TwoLevelCacheService _cache;
    private readonly FitPulseMetrics _metrics;
    private readonly ILogger<FitPulseGrpcService> _logger;

    public FitPulseGrpcService(
        TwoLevelCacheService cache,
        FitPulseMetrics metrics,
        ILogger<FitPulseGrpcService> logger)
    {
        _cache = cache;
        _metrics = metrics;
        _logger = logger;
    }

    public override async Task<ProductReply> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        var sw = Stopwatch.StartNew();
        _metrics.IncrementActiveSessions();

        try
        {
            if (request.Id <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Id must be positive"));
            }

            var cacheKey = $"fitpulse:product:{request.Id}";

        // Дворівневе кешування (L1 + L2)
            var product = await _cache.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    await Task.Delay(5, context.CancellationToken);
                    return new ProductCachedDto
                    {
                        Id = request.Id,
                        Name = $"Персональне тренування #{request.Id}",
                        Price = 500.00
                    };
                },
                ct: context.CancellationToken);

            if (product is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} not found"));
            }

            sw.Stop();
            _metrics.RecordWorkoutRequest(source: "grpc", status: "success");
            _metrics.RecordOperationDuration("get_product", success: true, sw.Elapsed.TotalMilliseconds);

            return new ProductReply
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price
            };
        }
        catch
        {
            sw.Stop();
            _metrics.RecordWorkoutRequest(source: "grpc", status: "failure");
            _metrics.RecordOperationDuration("get_product", success: false, sw.Elapsed.TotalMilliseconds);
            throw;
        }
        finally
        {
            _metrics.DecrementActiveSessions();
        }
    }
}

public class ProductCachedDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
}