using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MiniHttpServer.ORM
{
    public class SqlQueryBuilder
    {
        // Проверка имени таблицы или столбца и экранирование для PostgreSQL
        public string EscapeIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier) ||
                !Regex.IsMatch(identifier, @"^[A-Za-z_][A-Za-z0-9_]*$"))
            {
                throw new ArgumentException("Недопустимое имя SQL-идентификатора.", nameof(identifier));
            }

            return $"\"{identifier}\"";
        }

        // Формирование параметризованного INSERT без столбца Id
        public (string Sql, Dictionary<string, object> Parameters) BuildInsert<T>(string tableName, T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            string safeTable = EscapeIdentifier(tableName);
            var props = GetWritableProperties(typeof(T), excludeId: true);

            if (props.Length == 0)
                throw new InvalidOperationException("Модель не содержит свойств для вставки.");

            var columns = new List<string>();
            var paramNames = new List<string>();
            var parameters = new Dictionary<string, object>();

            foreach (var prop in props)
            {
                columns.Add(EscapeIdentifier(prop.Name));
                string paramName = $"@{prop.Name}";
                paramNames.Add(paramName);
                parameters[paramName] = prop.GetValue(entity) ?? DBNull.Value;
            }

            string sql = $"INSERT INTO {safeTable} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)})";
            return (sql, parameters);
        }

        // Формирование параметризованного UPDATE по первичному ключу Id (без изменения Id)
        public (string Sql, Dictionary<string, object> Parameters) BuildUpdate<T>(string tableName, int id, T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            string safeTable = EscapeIdentifier(tableName);
            var props = GetWritableProperties(typeof(T), excludeId: true);

            if (props.Length == 0)
                throw new InvalidOperationException("Модель не содержит свойств для обновления.");

            var setClauses = new List<string>();
            var parameters = new Dictionary<string, object>
            {
                ["@Id"] = id
            };

            foreach (var prop in props)
            {
                string paramName = $"@{prop.Name}";
                setClauses.Add($"{EscapeIdentifier(prop.Name)} = {paramName}");
                parameters[paramName] = prop.GetValue(entity) ?? DBNull.Value;
            }

            string sql = $"UPDATE {safeTable} SET {string.Join(", ", setClauses)} WHERE \"Id\" = @Id";
            return (sql, parameters);
        }

        // Формирование параметризованного DELETE по первичному ключу Id
        public (string Sql, Dictionary<string, object> Parameters) BuildDelete(string tableName, int id)
        {
            string safeTable = EscapeIdentifier(tableName);
            string sql = $"DELETE FROM {safeTable} WHERE \"Id\" = @Id";
            var parameters = new Dictionary<string, object> { ["@Id"] = id };
            return (sql, parameters);
        }

        private PropertyInfo[] GetWritableProperties(Type type, bool excludeId)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && (!excludeId || !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }
    }
}
