using System.Data;
using Microsoft.Data.SqlClient;
using FitPulse.Dal.Interfaces;
using FitPulse.Domain.Entities;

namespace FitPulse.Dal.Repositories;

// Реалізація на чистому ADO.NET (без Dapper) для демонстрації параметризації та ручного читання
public sealed class ClientRepository : RepositoryBase, IClientRepository
{
    public ClientRepository(SqlConnection connection, Func<SqlTransaction?> transaction)
        : base(connection, transaction) { }

    public async Task<Client?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT client_id, external_user_id, full_name, email, phone, version, created_at, created_by, is_deleted
            FROM clients
            WHERE client_id = @Id AND is_deleted = 0;";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapClient(reader);
    }

    public async Task<Client?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT client_id, external_user_id, full_name, email, phone, version, created_at, created_by, is_deleted
            FROM clients
            WHERE email = @Email AND is_deleted = 0;";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = email;

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapClient(reader);
    }

    public async Task<long> AddAsync(Client client, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO clients (external_user_id, full_name, email, phone, created_by)
            OUTPUT INSERTED.client_id
            VALUES (@ExternalUserId, @FullName, @Email, @Phone, @CreatedBy);";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@ExternalUserId", SqlDbType.UniqueIdentifier).Value = client.ExternalUserId;
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = client.FullName;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = client.Email;
        command.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = client.Phone;
        command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100).Value = client.CreatedBy;

        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    private static Client MapClient(SqlDataReader reader)
    {
        var client = new Client
        {
            ClientId = Convert.ToInt64(reader["client_id"]),
            ExternalUserId = (Guid)reader["external_user_id"],
            FullName = Convert.ToString(reader["full_name"])!,
            Email = Convert.ToString(reader["email"])!,
            Phone = Convert.ToString(reader["phone"])!,
            Version = Convert.ToInt32(reader["version"]),
            CreatedAt = reader["created_at"] is DateTimeOffset dto
                ? dto.UtcDateTime
                : Convert.ToDateTime(reader["created_at"]),
            CreatedBy = Convert.ToString(reader["created_by"])!,
            IsDeleted = Convert.ToBoolean(reader["is_deleted"])
        };

        // Безпечно зчитуємо optional-поля тільки якщо вони є у вибірці SQL
        if (HasColumn(reader, "updated_at") && reader["updated_at"] != DBNull.Value)
        {
            client.UpdatedAt = reader["updated_at"] is DateTimeOffset udto
                ? udto.UtcDateTime
                : Convert.ToDateTime(reader["updated_at"]);
        }

        if (HasColumn(reader, "updated_by") && reader["updated_by"] != DBNull.Value)
        {
            client.UpdatedBy = Convert.ToString(reader["updated_by"]);
        }

        return client;
    }

    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}