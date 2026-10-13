using Dapper;
using Microsoft.Data.SqlClient;
using FitPulse.Dal.Interfaces;
using FitPulse.Domain.Entities;

namespace FitPulse.Dal.Repositories;

public sealed class PaymentRepository : BaseDapperRepository<PaymentRecord>, IPaymentRepository
{
    public PaymentRepository(SqlConnection connection, Func<SqlTransaction?> transaction)
        : base(connection, transaction, "payment_records", "booking_id") { }

    public override async Task<long> AddAsync(PaymentRecord entity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        const string sql = @"
            INSERT INTO payment_records (booking_id, amount, payment_method, transaction_status, paid_at)
            VALUES (@BookingId, @Amount, @PaymentMethod, @TransactionStatus, @PaidAt);";

        await Connection.ExecuteAsync(
            new CommandDefinition(sql, entity, transaction: Transaction, cancellationToken: ct));

        return entity.BookingId;
    }

    public async Task<PaymentRecord?> GetByBookingIdAsync(long bookingId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        const string sql = "SELECT * FROM payment_records WHERE booking_id = @BookingId;";
        return await Connection.QuerySingleOrDefaultAsync<PaymentRecord>(
            new CommandDefinition(sql, new { BookingId = bookingId }, transaction: Transaction, cancellationToken: ct));
    }
}