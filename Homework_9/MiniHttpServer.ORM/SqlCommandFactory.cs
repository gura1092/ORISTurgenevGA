using System.Reflection;
using System.Text.RegularExpressions;

namespace MiniHttpServer.ORM;

internal sealed record SqlCommandSpec(string CommandText, IReadOnlyDictionary<string, object?> Parameters);

internal static class SqlCommandFactory
{
    private static readonly Regex Identifier = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    internal static string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || !Identifier.IsMatch(identifier))
            throw new ArgumentException("Недопустимое имя SQL-идентификатора. Разрешены буквы, цифры и подчёркивание; первый символ — буква или _.", nameof(identifier));
        return $"\"{identifier}\"";
    }

    internal static PropertyInfo[] GetWritableProperties<T>(bool includeId)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0 && p.SetMethod?.IsPublic == true && p.GetMethod?.IsPublic == true)
            .Where(p => includeId || !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.MetadataToken).ToArray();
        if (properties.Length == 0) throw new ArgumentException($"У модели {typeof(T).Name} нет доступных свойств для SQL-операции.");
        foreach (var p in properties) EscapeIdentifier(p.Name);
        return properties;
    }

    internal static SqlCommandSpec SelectAll(string tableName)
        => new($"SELECT * FROM {EscapeIdentifier(tableName)}", new Dictionary<string, object?>());

    internal static SqlCommandSpec SelectById(string tableName, int id)
        => new($"SELECT * FROM {EscapeIdentifier(tableName)} WHERE \"Id\" = @Id", new Dictionary<string, object?> { ["@Id"] = id });

    internal static SqlCommandSpec Insert<T>(string tableName, T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var props = GetWritableProperties<T>(includeId: false);
        var columns = string.Join(", ", props.Select(p => EscapeIdentifier(p.Name)));
        var parameters = string.Join(", ", props.Select(p => "@" + p.Name));
        var values = props.ToDictionary(p => "@" + p.Name, p => p.GetValue(entity));
        return new SqlCommandSpec($"INSERT INTO {EscapeIdentifier(tableName)} ({columns}) VALUES ({parameters})", values);
    }

    internal static SqlCommandSpec Update<T>(string tableName, int id, T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var props = GetWritableProperties<T>(includeId: false);
        var assignments = string.Join(", ", props.Select(p => $"{EscapeIdentifier(p.Name)} = @{p.Name}"));
        var parameters = props.ToDictionary(p => "@" + p.Name, p => p.GetValue(entity));
        parameters["@Id"] = id;
        return new SqlCommandSpec($"UPDATE {EscapeIdentifier(tableName)} SET {assignments} WHERE \"Id\" = @Id", parameters);
    }

    internal static SqlCommandSpec Delete(string tableName, int id)
        => new($"DELETE FROM {EscapeIdentifier(tableName)} WHERE \"Id\" = @Id", new Dictionary<string, object?> { ["@Id"] = id });
}
