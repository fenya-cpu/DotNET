namespace Reviews.WebApi.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Commands;
using Reviews.Application.Reviews.Queries;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly ISender _mediator;

    public ReviewsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetList(
        [FromQuery] long? productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new GetReviewsListQuery(productId, page, pageSize), ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ReviewDto>> GetById(string id, CancellationToken ct)
    {
        var review = await _mediator.Send(new GetReviewByIdQuery(id), ct);
        Response.Headers.ETag = $"\"{review.Version}\"";
        return Ok(review);
    }

    [HttpPost]
    public async Task<ActionResult<string>> Create([FromBody] CreateReviewCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }
}