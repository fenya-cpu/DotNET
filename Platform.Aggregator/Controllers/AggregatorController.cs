using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Dtos;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
public class AggregatorController : ControllerBase
{
    private readonly ProductAggregatorService _aggregatorService;

    public AggregatorController(ProductAggregatorService aggregatorService)
    {
        _aggregatorService = aggregatorService;
    }

    [HttpGet("products/{id:long}/full")]
    public async Task<ActionResult<ProductFullDetailsDto>> GetFullProduct(long id, CancellationToken ct)
    {
        var result = await _aggregatorService.GetProductFullDetailsAsync(id, ct);
        return Ok(result);
    }
}