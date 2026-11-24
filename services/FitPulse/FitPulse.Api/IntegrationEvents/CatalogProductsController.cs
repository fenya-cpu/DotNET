using FitPulse.Bll.Caching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Platform.EventBus;
using Platform.EventBus.Events;

namespace FitPulse.Api.Controllers;

[ApiController]
[Route("api/products")]
public class CatalogProductsController(
    IConfiguration configuration,
    IEventPublisher eventPublisher,
    TwoLevelCacheService cache,
    ILogger<CatalogProductsController> logger) : ControllerBase
{
    private string ConnectionString =>
        configuration.GetConnectionString("fitpulse-db")
        ?? throw new InvalidOperationException("Connection string 'fitpulse-db' is missing");

    public record UpdatePriceRequest(decimal NewPrice);
    public record RenameProductRequest(string NewName);

    [HttpPatch("{id:int}/price")]
    public async Task<IActionResult> ChangePrice(int id, [FromBody] UpdatePriceRequest request, CancellationToken ct)
    {
        if (request.NewPrice <= 0)
            return BadRequest(new { error = "Price must be greater than zero." });

        string productName;
        decimal oldPrice;

        await using (var conn = new SqlConnection(ConnectionString))
        {
            await conn.OpenAsync(ct);

            // Отримуємо поточні дані товару (якщо таблиці Products ще немає — створюємо демо-запис)
            await EnsureCatalogTableAsync(conn, id, ct);

            await using var selectCmd = new SqlCommand(
                "SELECT Name, Price FROM dbo.CatalogProducts WHERE Id = @Id", conn);
            selectCmd.Parameters.AddWithValue("@Id", id);

            await using (var reader = await selectCmd.ExecuteReaderAsync(ct))
            {
                if (!await reader.ReadAsync(ct))
                    return NotFound($"Product {id} not found");

                productName = reader.GetString(0);
                oldPrice = reader.GetDecimal(1);
            }

            // 1) Зберігаємо нову ціну в БД у транзакції[cite: 10, 22]
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
            try
            {
                await using var updateCmd = new SqlCommand(
                    "UPDATE dbo.CatalogProducts SET Price = @NewPrice, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
                    conn, tx);
                updateCmd.Parameters.AddWithValue("@NewPrice", request.NewPrice);
                updateCmd.Parameters.AddWithValue("@Id", id);
                await updateCmd.ExecuteNonQueryAsync(ct);

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw; // При збої БД подія НЕ публікується[cite: 10, 22]
            }
        }

        // Інвалідуємо кеш продукту в FitPulse
        await cache.RemoveAsync($"fitpulse:product:{id}", ct);

        // 2) Лише після успішного збереження в БД публікуємо подію[cite: 10, 22]
        var correlationId = HttpContext.TraceIdentifier;
        var @event = new ProductPriceChangedEvent(id, productName, oldPrice, request.NewPrice)
        {
            CorrelationId = correlationId
        };

        await eventPublisher.PublishAsync(@event, Topology.CatalogExchange, "product.price_changed", ct);

        logger.LogInformation(
            "Product {ProductId} price changed from {OldPrice} to {NewPrice}, published EventId {EventId}",
            id, oldPrice, request.NewPrice, @event.EventId);

        return Ok(@event);
    }

    [HttpPatch("{id:int}/name")]
    public async Task<IActionResult> RenameProduct(int id, [FromBody] RenameProductRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NewName))
            return BadRequest(new { error = "NewName is required." });

        string oldName;

        await using (var conn = new SqlConnection(ConnectionString))
        {
            await conn.OpenAsync(ct);
            await EnsureCatalogTableAsync(conn, id, ct);

            await using var selectCmd = new SqlCommand(
                "SELECT Name FROM dbo.CatalogProducts WHERE Id = @Id", conn);
            selectCmd.Parameters.AddWithValue("@Id", id);

            var scalar = await selectCmd.ExecuteScalarAsync(ct);
            if (scalar is null)
                return NotFound($"Product {id} not found");

            oldName = (string)scalar;

            // 1) Зберігаємо нову назву в БД[cite: 10]
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
            try
            {
                await using var updateCmd = new SqlCommand(
                    "UPDATE dbo.CatalogProducts SET Name = @NewName, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
                    conn, tx);
                updateCmd.Parameters.AddWithValue("@NewName", request.NewName);
                updateCmd.Parameters.AddWithValue("@Id", id);
                await updateCmd.ExecuteNonQueryAsync(ct);

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        await cache.RemoveAsync($"fitpulse:product:{id}", ct);

        // 2) Публікуємо подію перейменування[cite: 9, 10]
        var @event = new ProductRenamedEvent(id, oldName, request.NewName)
        {
            CorrelationId = HttpContext.TraceIdentifier
        };

        await eventPublisher.PublishAsync(@event, Topology.CatalogExchange, "product.renamed", ct);

        return Ok(@event);
    }

    private static async Task EnsureCatalogTableAsync(SqlConnection conn, int productId, CancellationToken ct)
    {
        const string sql = """
            IF OBJECT_ID('dbo.CatalogProducts', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CatalogProducts (
                    Id INT PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Price DECIMAL(18, 2) NOT NULL,
                    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );
            END
            IF NOT EXISTS (SELECT 1 FROM dbo.CatalogProducts WHERE Id = @Id)
            BEGIN
                INSERT INTO dbo.CatalogProducts (Id, Name, Price)
                VALUES (@Id, CONCAT(N'Персональне тренування #', @Id), 500.00);
            END
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", productId);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}