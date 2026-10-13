using System.Data;

namespace FitPulse.Dal.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IClientRepository Clients { get; }
    IBookingRepository Bookings { get; }
    IPaymentRepository Payments { get; }

    Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}