
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Dtos;
using Platform.Aggregator.Services;


[ApiController]
[Route("api/aggregator")]
public class AggregatorController : ControllerBase
{
    private readonly FitPulseClient _fitpulse;
    private readonly ReviewsClient _reviews;
    private readonly ILogger<AggregatorController> _logger;

    public AggregatorController(FitPulseClient fitpulse, ReviewsClient reviews, ILogger<AggregatorController> logger)
    {
        _fitpulse = fitpulse;
        _reviews = reviews;
        _logger = logger;
    }

    [HttpGet("products/{id}/full")]
    public async Task<ActionResult<FullProductSummaryDto>> GetFullProductDetails(long id, CancellationToken ct)
    {
        var warnings = new List<string>();

        // Паралельний виклик через Task.WhenAll
        var productTask = SafeAsync(() => _fitpulse.GetProductAsync(id, ct), "fitpulse-api", warnings);
        var reviewsTask = SafeAsync(() => _reviews.GetForProductAsync(id, ct), "reviews-api", warnings);

        await Task.WhenAll(productTask, reviewsTask);

        var product = await productTask;
        var reviews = await reviewsTask ?? new List<ReviewDto>();

        return Ok(new FullProductSummaryDto(product, reviews, warnings));
    }

    private async Task<T?> SafeAsync<T>(Func<Task<T?>> call, string service, List<string> warnings)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await call();
            _logger.LogInformation("{Service} responded in {Elapsed} ms", service, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "{Service} is unavailable", service);
            lock (warnings) warnings.Add($"Сервіс {service} тимчасово недоступний.");
            return default;
        }
    }
}