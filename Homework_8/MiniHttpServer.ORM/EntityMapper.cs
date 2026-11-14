using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace MiniHttpServer.ORM
{
    public class EntityMapper
    {
        public T MapRow<T>(DbDataReader reader, Dictionary<string, int> columns) where T : class, new()
        {
            T entity = new T();
            PropertyInfo[] properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.SetMethod?.IsPublic == true)
                .ToArray();

            foreach (PropertyInfo property in properties)
            {
                if (!columns.TryGetValue(property.Name, out int ordinal))
                    continue;

                if (reader.IsDBNull(ordinal))
                {
                    if (!property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null)
                        property.SetValue(entity, null);
                    continue;
                }

                object value = reader.GetValue(ordinal);
                object converted = ConvertValue(value, property.PropertyType);
                property.SetValue(entity, converted);
            }

            return entity;
        }

        // Преобразование значения из БД в целевой тип свойства C#
        public object ConvertValue(object value, Type propertyType)
        {
            if (value == null || value is DBNull)
                return null;

            Type targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (targetType.IsInstanceOfType(value))
                return value;

            if (targetType.IsEnum)
                return Enum.ToObject(targetType, value);

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
    }
}
