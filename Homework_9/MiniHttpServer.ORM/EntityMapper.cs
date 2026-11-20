using System.Globalization;
using System.Reflection;

namespace MiniHttpServer.ORM;

internal static class EntityMapper
{
    internal static T Map<T>(IReadOnlyDictionary<string, object?> values) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(values);
        var entity = new T();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.SetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0);
        foreach (var property in properties)
        {
            if (!values.TryGetValue(property.Name, out var value)) continue;
            if (value is null or DBNull)
            {
                if (!property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) is not null)
                    property.SetValue(entity, null);
                continue;
            }
            var target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            object converted;
            if (target.IsInstanceOfType(value)) converted = value;
            else if (target.IsEnum) converted = Enum.ToObject(target, value);
            else if (target == typeof(Guid)) converted = value is Guid g ? g : Guid.Parse(value.ToString()!);
            else if (target == typeof(DateTime)) converted = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
            else converted = Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
            property.SetValue(entity, converted);
        }
        return entity;
    }
}
