using FitPulse.Bll.Dtos;

namespace FitPulse.Bll.Services;

public interface IBookingService
{
    Task<BookingDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<BookingDto> CreateBookingWithPaymentAsync(CreateBookingWithPaymentDto dto, CancellationToken ct = default);
    Task CancelBookingAsync(long bookingId, CancellationToken ct = default);
}