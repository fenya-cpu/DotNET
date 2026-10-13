using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using FitPulse.Dal.Interfaces;
using FitPulse.Domain.Entities;

namespace FitPulse.Dal.Repositories;

public sealed class BookingRepository : BaseDapperRepository<Booking>, IBookingRepository
{
    public BookingRepository(SqlConnection connection, Func<SqlTransaction?> transaction)
        : base(connection, transaction, "bookings", "booking_id") { }

    public override async Task<long> AddAsync(Booking entity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        const string sql = @"
            INSERT INTO bookings (client_id, schedule_slot_id, trainer_name, booking_date, status, created_by)
            OUTPUT INSERTED.booking_id
            VALUES (@ClientId, @ScheduleSlotId, @TrainerName, @BookingDate, @Status, @CreatedBy);";

        return await Connection.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, entity, transaction: Transaction, cancellationToken: ct));
    }

    // Multi-mapping запит Dapper для відображення Booking + PaymentRecord
    public async Task<Booking?> GetWithPaymentAsync(long bookingId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        // Додано CAST для безпечного приведення типів дат та всі відсутні колонки
        const string sql = @"
            SELECT 
                b.booking_id AS BookingId, 
                b.client_id AS ClientId, 
                b.schedule_slot_id AS ScheduleSlotId,
                b.trainer_name AS TrainerName, 
                CAST(b.booking_date AS DATETIME2) AS BookingDate, 
                b.status AS Status,
                b.version AS Version, 
                CAST(b.created_at AS DATETIME2) AS CreatedAt, 
                b.created_by AS CreatedBy,
                b.is_deleted AS IsDeleted,
                p.booking_id AS BookingId, 
                p.amount AS Amount, 
                p.payment_method AS PaymentMethod,
                p.transaction_status AS TransactionStatus, 
                CAST(p.paid_at AS DATETIME2) AS PaidAt
            FROM bookings b
            LEFT JOIN payment_records p ON b.booking_id = p.booking_id
            WHERE b.booking_id = @BookingId AND b.is_deleted = 0;";

        Booking? result = null;
        await Connection.QueryAsync<Booking, PaymentRecord, Booking>(
            new CommandDefinition(
                sql,
                new { BookingId = bookingId },
                transaction: Transaction,
                cancellationToken: ct),
            (booking, payment) =>
            {
                result ??= booking;
                if (payment != null && payment.Amount > 0)
                {
                    result.Payment = payment;
                }
                return result;
            },
            splitOn: "BookingId");

        return result;
    }

    // Виклики збережуваної процедури з Лаб. 01 через Dapper
    public async Task ConfirmBookingAsync(long bookingId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        await Connection.ExecuteAsync(
            new CommandDefinition(
                "sp_cancel_booking",
                new { p_booking_id = bookingId, p_user_name = "api_service" },
                transaction: Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }
}