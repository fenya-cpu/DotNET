using Microsoft.Data.SqlClient;
using Platform.EventBus.Events;

namespace FitPulse.Api.IntegrationEvents;

public class OrdersProductSyncHandler(
    IConfiguration configuration,
    ILogger<OrdersProductSyncHandler> logger)
{
    private string ConnectionString =>
        configuration.GetConnectionString("fitpulse-db")
        ?? throw new InvalidOperationException("Connection string 'fitpulse-db' is missing");

    public async Task HandlePriceChangedAsync(ProductPriceChangedEvent e, CancellationToken ct)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync(ct);
        await EnsureSyncTablesAsync(conn, ct);

        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
        try
        {
            // 1. Перевірка ідемпотентності в тій самій транзакції[cite: 12, 17, 18]
            if (!await TryMarkEventProcessedAsync(conn, tx, e.EventId, nameof(ProductPriceChangedEvent), ct))
            {
                logger.LogInformation(
                    "Duplicate event {EventId} ({EventType}) for Product {ProductId} skipped",
                    e.EventId, nameof(ProductPriceChangedEvent), e.ProductId);
                await tx.RollbackAsync(ct);
                return;
            }

            // 2. Оновлюємо локальну копію довідника Products (захист від out-of-order через OccurredAt)[cite: 12, 13]
            // Важливо: ціни в уже оформлених позиціях замовлень (OrderItems.UnitPrice) не змінюються[cite: 12, 23]
            const string upsertSql = """
                MERGE dbo.Products AS target
                USING (SELECT @Id AS Id, @Name AS Name, @Price AS Price, @OccurredAt AS LastUpdated) AS source
                ON target.Id = source.Id
                WHEN MATCHED AND target.LastUpdated <= source.LastUpdated THEN
                    UPDATE SET Name = source.Name, Price = source.Price, LastUpdated = source.LastUpdated
                WHEN NOT MATCHED THEN
                    INSERT (Id, Name, Price, LastUpdated)
                    VALUES (source.Id, source.Name, source.Price, source.LastUpdated);
                """;

            await using var cmd = new SqlCommand(upsertSql, conn, tx);
            cmd.Parameters.AddWithValue("@Id", e.ProductId);
            cmd.Parameters.AddWithValue("@Name", e.ProductName);
            cmd.Parameters.AddWithValue("@Price", e.NewPrice);
            cmd.Parameters.AddWithValue("@OccurredAt", e.OccurredAt);
            await cmd.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);

            logger.LogInformation(
                "Updated local Products replica for ProductId {ProductId} to Price {NewPrice} (EventId: {EventId})",
                e.ProductId, e.NewPrice, e.EventId);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task HandleRenamedAsync(ProductRenamedEvent e, CancellationToken ct)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync(ct);
        await EnsureSyncTablesAsync(conn, ct);

        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
        try
        {
            if (!await TryMarkEventProcessedAsync(conn, tx, e.EventId, nameof(ProductRenamedEvent), ct))
            {
                logger.LogInformation(
                    "Duplicate event {EventId} ({EventType}) for Product {ProductId} skipped",
                    e.EventId, nameof(ProductRenamedEvent), e.ProductId);
                await tx.RollbackAsync(ct);
                return;
            }

            const string updateSql = """
                UPDATE dbo.Products
                SET Name = @Name, LastUpdated = @OccurredAt
                WHERE Id = @Id AND LastUpdated <= @OccurredAt;
                """;

            await using var cmd = new SqlCommand(updateSql, conn, tx);
            cmd.Parameters.AddWithValue("@Id", e.ProductId);
            cmd.Parameters.AddWithValue("@Name", e.NewName);
            cmd.Parameters.AddWithValue("@OccurredAt", e.OccurredAt);
            await cmd.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);

            logger.LogInformation(
                "Updated local Products replica name for ProductId {ProductId} to '{NewName}' (EventId: {EventId})",
                e.ProductId, e.NewName, e.EventId);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task<bool> TryMarkEventProcessedAsync(
        SqlConnection conn,
        SqlTransaction tx,
        Guid eventId,
        string eventType,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.ProcessedEvents (EventId, EventType, ProcessedAt)
            VALUES (@EventId, @EventType, SYSUTCDATETIME());
            """;

        try
        {
            await using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@EventId", eventId);
            cmd.Parameters.AddWithValue("@EventType", eventType);
            await cmd.ExecuteNonQueryAsync(ct);
            return true;
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601) // Порушення PRIMARY KEY -> дублікат події[cite: 18]
        {
            return false;
        }
    }

    private static async Task EnsureSyncTablesAsync(SqlConnection conn, CancellationToken ct)
    {
        const string sql = """
            IF OBJECT_ID('dbo.ProcessedEvents', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ProcessedEvents (
                    EventId UNIQUEIDENTIFIER PRIMARY KEY,
                    EventType NVARCHAR(150) NOT NULL,
                    ProcessedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );
            END
            IF OBJECT_ID('dbo.Products', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Products (
                    Id INT PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Price DECIMAL(18, 2) NOT NULL,
                    LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );
            END
            """;

        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}