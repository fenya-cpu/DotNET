namespace FitPulse.Api.Controllers;

using FitPulse.Bll.DTOs;
using FitPulse.Bll.Services;
using FitPulse.Dal.Specifications;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetList(
        [FromQuery] ProductQuery query, CancellationToken ct)
    {
        return Ok(await _service.GetListAsync(query, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
    {
        return Ok(await _service.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductDto input,
        [FromServices] IValidator<CreateProductDto> validator,
        CancellationToken ct)
    {
        // Явний виклик валідатора згідно з новими гайдлайнами FluentValidation
        var validationResult = await validator.ValidateAsync(input, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var created = await _service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:int}/add-stock")]
    public async Task<IActionResult> AddStock(int id, [FromBody] UpdateStockDto dto, CancellationToken ct)
    {
        await _service.AddStockAsync(id, dto.Quantity, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/remove-stock")]
    public async Task<IActionResult> RemoveStock(int id, [FromBody] UpdateStockDto dto, CancellationToken ct)
    {
        await _service.RemoveStockAsync(id, dto.Quantity, ct);
        return NoContent();
    }
}