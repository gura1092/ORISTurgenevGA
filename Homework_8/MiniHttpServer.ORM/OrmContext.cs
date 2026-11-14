using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace MiniHttpServer.ORM
{
    public sealed class OrmContext : IOrmContext
    {
        private readonly string _connectionString;
        private readonly SqlQueryBuilder _queryBuilder;
        private readonly EntityMapper _mapper;

        public OrmContext(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Строка подключения не задана.", nameof(connectionString));

            _connectionString = connectionString;
            _queryBuilder = new SqlQueryBuilder();
            _mapper = new EntityMapper();
        }

        public async Task<IReadOnlyList<T>> GetAllAsync<T>(
            string tableName,
            CancellationToken cancellationToken = default)
            where T : class, new()
        {
            string safeTableName = _queryBuilder.EscapeIdentifier(tableName);
            string sql = $"SELECT * FROM {safeTableName}";
            var result = new List<T>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (var command = new NpgsqlCommand(sql, connection))
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < reader.FieldCount; i++)
                        columns[reader.GetName(i)] = i;

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        result.Add(_mapper.MapRow<T>(reader, columns));
                    }
                }
            }

            return result;
        }

        public async Task<int> InsertAsync<T>(
            string tableName,
            T entity,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var (sql, parameters) = _queryBuilder.BuildInsert(tableName, entity);
            return await ExecuteNonQueryAsync(sql, parameters, cancellationToken);
        }

        public async Task<int> UpdateAsync<T>(
            string tableName,
            int id,
            T entity,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var (sql, parameters) = _queryBuilder.BuildUpdate(tableName, id, entity);
            return await ExecuteNonQueryAsync(sql, parameters, cancellationToken);
        }

        public async Task<int> DeleteAsync(
            string tableName,
            int id,
            CancellationToken cancellationToken = default)
        {
            var (sql, parameters) = _queryBuilder.BuildDelete(tableName, id);
            return await ExecuteNonQueryAsync(sql, parameters, cancellationToken);
        }

        private async Task<int> ExecuteNonQueryAsync(
            string sql,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    foreach (var kv in parameters)
                        command.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

                    return await command.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }
    }
}
