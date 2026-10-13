using FitPulse.Domain.Entities;

namespace FitPulse.Dal.Interfaces;

// Чистий ADO.NET репозиторій свідомо НЕ успадковує IGenericRepository<T>
public interface IClientRepository
{
    Task<Client?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Client?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<long> AddAsync(Client client, CancellationToken ct = default);
}

public interface IBookingRepository : IGenericRepository<Booking>
{
    Task<Booking?> GetWithPaymentAsync(long bookingId, CancellationToken ct = default);
    Task ConfirmBookingAsync(long bookingId, CancellationToken ct = default);
}

public interface IPaymentRepository : IGenericRepository<PaymentRecord>
{
    Task<PaymentRecord?> GetByBookingIdAsync(long bookingId, CancellationToken ct = default);
}