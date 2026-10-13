using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using FitPulse.Dal.Interfaces;

namespace FitPulse.Dal.Repositories;

public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T> where T : class
{
    protected readonly string TableName;
    protected readonly string IdColumnName;

    protected BaseDapperRepository(SqlConnection connection, Func<SqlTransaction?> transaction, string tableName, string idColumnName)
        : base(connection, transaction)
    {
        TableName = tableName;
        IdColumnName = idColumnName;
    }

    public async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        string sql = $"SELECT * FROM {TableName} WHERE {IdColumnName} = @Id AND is_deleted = 0;";
        return await Connection.QuerySingleOrDefaultAsync<T>(
            new CommandDefinition(sql, new { Id = id }, transaction: Transaction, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        string sql = $"SELECT * FROM {TableName} WHERE is_deleted = 0;";
        var result = await Connection.QueryAsync<T>(
            new CommandDefinition(sql, transaction: Transaction, cancellationToken: ct));
        return result.ToList().AsReadOnly();
    }

    public abstract Task<long> AddAsync(T entity, CancellationToken ct = default);

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        string sql = $"UPDATE {TableName} SET is_deleted = 1, updated_at = GETUTCDATE() WHERE {IdColumnName} = @Id;";
        int rows = await Connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, transaction: Transaction, cancellationToken: ct));
        return rows > 0;
    }
}