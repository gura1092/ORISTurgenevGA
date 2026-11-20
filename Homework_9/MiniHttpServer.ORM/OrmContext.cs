using Npgsql;
using System.Reflection;

namespace MiniHttpServer.ORM;
public sealed class OrmContext : IOrmContext
{
    private readonly string _connectionString;
    public OrmContext(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public Task<IReadOnlyList<T>> GetAllAsync<T>(string tableName, CancellationToken cancellationToken = default) where T : class, new()
        => QueryAsync<T>(SqlCommandFactory.SelectAll(tableName), cancellationToken);

    public async Task<T?> GetByIdAsync<T>(string tableName, int id, CancellationToken cancellationToken = default) where T : class, new()
    {
        var rows = await QueryAsync<T>(SqlCommandFactory.SelectById(tableName, id), cancellationToken).ConfigureAwait(false);
        return rows.FirstOrDefault();
    }

    public Task<int> InsertAsync<T>(string tableName, T entity, CancellationToken cancellationToken = default) where T : class
        => ExecuteAsync(SqlCommandFactory.Insert(tableName, entity), cancellationToken);

    public Task<int> UpdateAsync<T>(string tableName, int id, T entity, CancellationToken cancellationToken = default) where T : class
        => ExecuteAsync(SqlCommandFactory.Update(tableName, id, entity), cancellationToken);

    public Task<int> DeleteAsync(string tableName, int id, CancellationToken cancellationToken = default)
        => ExecuteAsync(SqlCommandFactory.Delete(tableName, id), cancellationToken);

    private async Task<IReadOnlyList<T>> QueryAsync<T>(SqlCommandSpec spec, CancellationToken token) where T : class, new()
    {
        var result = new List<T>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(token).ConfigureAwait(false);
        await using var command = CreateCommand(connection, spec);
        await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
        var ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++) ordinals[reader.GetName(i)] = i;
        var writable = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.SetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0).ToArray();
        while (await reader.ReadAsync(token).ConfigureAwait(false))
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in writable)
                if (ordinals.TryGetValue(property.Name, out int ordinal))
                    values[property.Name] = await reader.IsDBNullAsync(ordinal, token).ConfigureAwait(false) ? null : reader.GetValue(ordinal);
            result.Add(EntityMapper.Map<T>(values));
        }
        return result;
    }

    private async Task<int> ExecuteAsync(SqlCommandSpec spec, CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(token).ConfigureAwait(false);
        await using var command = CreateCommand(connection, spec);
        return await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
    }

    private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, SqlCommandSpec spec)
    {
        var command = new NpgsqlCommand(spec.CommandText, connection);
        foreach (var parameter in spec.Parameters)
            command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
        return command;
    }
}
