using FitPulse.Bll.Caching;
using FitPulse.Grpc;
using Grpc.Core;

namespace FitPulse.Api.Grpc;

public class FitPulseGrpcService : FitPulseService.FitPulseServiceBase
{
    private readonly TwoLevelCacheService _cache;
    private readonly ILogger<FitPulseGrpcService> _logger;

    public FitPulseGrpcService(TwoLevelCacheService cache, ILogger<FitPulseGrpcService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public override async Task<ProductReply> GetProduct(GetProductRequest request, ServerCallContext context)
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
                await Task.Delay(5, context.CancellationToken); // Моделювання виклику БД
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

        return new ProductReply
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price
        };
    }
}

public class ProductCachedDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
}