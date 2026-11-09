using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FitPulse.Bll.Dtos;
using FitPulse.Bll.Services;

namespace FitPulse.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private static readonly ActivitySource ActivitySource = new("FitPulse.Api");
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(IBookingService bookingService, ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<BookingDto>> GetById(long id, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("GetBookingById");
        activity?.SetTag("booking.id", id);

        _logger.LogInformation("Отримання деталей бронювання {BookingId}", id);

        var result = await _bookingService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingWithPaymentDto input, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("CreateBookingWithPayment");

        _logger.LogInformation("Створення нового бронювання");

        var created = await _bookingService.CreateBookingWithPaymentAsync(input, ct);

        activity?.SetTag("booking.id", created.BookingId);
        _logger.LogInformation("Бронювання успішно створено. BookingId: {BookingId}", created.BookingId);

        return CreatedAtAction(nameof(GetById), new { id = created.BookingId }, created);
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("CancelBooking");
        activity?.SetTag("booking.id", id);

        _logger.LogWarning("Скасування бронювання {BookingId}", id);

        await _bookingService.CancelBookingAsync(id, ct);

        _logger.LogInformation("Бронювання {BookingId} успішно скасовано", id);

        return NoContent();
    }
}