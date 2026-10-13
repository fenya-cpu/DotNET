using System.Data;
using AutoMapper;
using FitPulse.Bll.Dtos;
using FitPulse.Dal.Interfaces;
using FitPulse.Domain.Entities;
using FitPulse.Domain.Exceptions;

namespace FitPulse.Bll.Services;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public BookingService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<BookingDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var booking = await _uow.Bookings.GetWithPaymentAsync(id, ct)
            ?? throw new NotFoundException($"Бронювання з ID {id} не знайдено.");

        return _mapper.Map<BookingDto>(booking);
    }

    // Транзакційна бізнес-операція (UoW охоплює додавання бронювання та платежу)
    public async Task<BookingDto> CreateBookingWithPaymentAsync(CreateBookingWithPaymentDto dto, CancellationToken ct = default)
    {
        // Бізнес-валідація в BLL: перевіряємо наявність клієнта
        var client = await _uow.Clients.GetByIdAsync(dto.ClientId, ct)
            ?? throw new NotFoundException($"Клієнта з ID {dto.ClientId} не знайдено.");

        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            // 1. Створення бронювання (Знімок імені тренера на момент замовлення)
            var booking = new Booking
            {
                ClientId = dto.ClientId,
                ScheduleSlotId = dto.ScheduleSlotId,
                TrainerName = dto.TrainerName, // Snapshot
                BookingDate = dto.BookingDate,
                Status = "CONFIRMED",
                CreatedBy = "api_user"
            };

            long bookingId = await _uow.Bookings.AddAsync(booking, ct);
            booking.BookingId = bookingId;

            // 2. Створення фінансового чеку (1:1)
            var payment = new PaymentRecord
            {
                BookingId = bookingId,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                TransactionStatus = "SUCCESS",
                PaidAt = DateTime.UtcNow
            };

            await _uow.Payments.AddAsync(payment, ct);
            booking.Payment = payment;

            await _uow.CommitAsync(ct);
            return _mapper.Map<BookingDto>(booking);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }

    public async Task CancelBookingAsync(long bookingId, CancellationToken ct = default)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId, ct)
            ?? throw new NotFoundException($"Бронювання з ID {bookingId} не знайдено.");

        if (booking.Status == "CANCELLED")
            throw new BusinessConflictException($"Бронювання {bookingId} вже було скасовано раніше.");

        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await _uow.Bookings.ConfirmBookingAsync(bookingId, ct);
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }
}