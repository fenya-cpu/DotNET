using System.Data;
using Microsoft.Data.SqlClient;
using FitPulse.Dal.Interfaces;
using FitPulse.Dal.Repositories;

namespace FitPulse.Dal.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SqlConnection _connection;
    private SqlTransaction? _transaction;

    public UnitOfWork(string connectionString)
    {
        _connection = new SqlConnection(connectionString);

        // Усі репозиторії отримують ОДНЕ з'єднання і доступ до ПОТОЧНОЇ транзакції
        Clients = new ClientRepository(_connection, () => _transaction);
        Bookings = new BookingRepository(_connection, () => _transaction);
        Payments = new PaymentRepository(_connection, () => _transaction);
    }

    public IClientRepository Clients { get; }
    public IBookingRepository Bookings { get; }
    public IPaymentRepository Payments { get; }

    public async Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(ct);

        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(level, ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();

        await _connection.DisposeAsync();
    }
}