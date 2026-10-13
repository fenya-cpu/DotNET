using Microsoft.AspNetCore.Mvc;
using FitPulse.Bll.Dtos;
using FitPulse.Bll.Services;

namespace FitPulse.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<BookingDto>> GetById(long id, CancellationToken ct)
    {
        var result = await _bookingService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingWithPaymentDto input, CancellationToken ct)
    {
        var created = await _bookingService.CreateBookingWithPaymentAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.BookingId }, created);
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        await _bookingService.CancelBookingAsync(id, ct);
        return NoContent();
    }
}