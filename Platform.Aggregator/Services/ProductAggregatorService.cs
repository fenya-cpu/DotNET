using System.Text.Json;
using FitPulse.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using Platform.Aggregator.Dtos;
using Reviews.Grpc;

namespace Platform.Aggregator.Services;

public class ProductAggregatorService
{
    private readonly FitPulseService.FitPulseServiceClient _fitPulseClient;
    private readonly ReviewsService.ReviewsServiceClient _reviewsClient;
    private readonly IDistributedCache _redisCache;
    private readonly ILogger<ProductAggregatorService> _logger;

    public ProductAggregatorService(
        FitPulseService.FitPulseServiceClient fitPulseClient,
        ReviewsService.ReviewsServiceClient reviewsClient,
        IDistributedCache redisCache,
        ILogger<ProductAggregatorService> logger)
    {
        _fitPulseClient = fitPulseClient;
        _reviewsClient = reviewsClient;
        _redisCache = redisCache;
        _logger = logger;
    }

    public async Task<ProductFullDetailsDto> GetProductFullDetailsAsync(long productId, CancellationToken ct)
    {
        var cacheKey = $"aggregator:product:{productId}:full";

        // 1. Перевіряємо кеш у Redis ДО паралельних викликів[cite: 18]
        var cachedBytes = await _redisCache.GetAsync(cacheKey, ct);
        if (cachedBytes is not null)
        {
            _logger.LogInformation("Cache hit for aggregated product {ProductId}", productId);
            var cachedResult = JsonSerializer.Deserialize<ProductFullDetailsDto>(cachedBytes);
            if (cachedResult is not null) return cachedResult;
        }

        var warnings = new List<string>();

        // 2. Паралельні gRPC-виклики через Task.WhenAll[cite: 18]
        var productTask = SafeAsync(
            async () => await _fitPulseClient.GetProductAsync(new GetProductRequest { Id = productId }, cancellationToken: ct),
            "fitpulse-api",
            warnings);

        var reviewsTask = SafeAsync(
            async () => await _reviewsClient.GetForProductAsync(new GetForProductRequest { ProductId = productId }, cancellationToken: ct),
            "reviews-api",
            warnings);

        await Task.WhenAll(productTask, reviewsTask);

        var productReply = await productTask;
        var reviewsReply = await reviewsTask;

        var result = new ProductFullDetailsDto
        {
            Product = productReply is null ? null : new ProductDto
            {
                Id = productReply.Id,
                Name = productReply.Name,
                Price = productReply.Price
            },
            Reviews = reviewsReply?.Items.Select(r => new ReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                AuthorName = r.AuthorName,
                Rating = r.Rating,
                Text = r.Text
            }).ToList() ?? new List<ReviewDto>(),
            Warnings = warnings
        };

        // 3. Часткові відповіді з помилками НЕ кешуємо[cite: 18, 23]
        if (result.Warnings.Count == 0 && result.Product is not null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) // Короткий TTL[cite: 18]
            };
            await _redisCache.SetAsync(cacheKey, JsonSerializer.SerializeToUtf8Bytes(result), options, ct);
        }

        return result;
    }

    private async Task<T?> SafeAsync<T>(Func<Task<T>> call, string serviceName, List<string> warnings) where T : class
    {
        try
        {
            return await call();
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.NotFound)
        {
            _logger.LogWarning(ex, "gRPC call to {ServiceName} failed with status {Status}", serviceName, ex.StatusCode);
            warnings.Add($"Сервіс {serviceName} тимчасово недоступний або повернув помилку: {ex.Status.Detail}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during gRPC call to {ServiceName}", serviceName);
            warnings.Add($"Несподівана помилка при виклику {serviceName}");
            return null;
        }
    }
}