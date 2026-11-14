using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Web;

namespace MiniHttpServer.Framework.Routing
{
    public class RequestParameterBinder
    {
        // Разбор строки запроса (?a=1&b=test) или form-urlencoded тела
        public NameValueCollection ParseFormOrQuery(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData))
                return new NameValueCollection();

            return HttpUtility.ParseQueryString(rawData.TrimStart('?'));
        }

        // Сопоставление и приведение параметров метода контроллера
        public object[] BindParameters(MethodInfo method, NameValueCollection values)
        {
            return method.GetParameters()
                .Select(p =>
                {
                    string rawValue = values?[p.Name];
                    if (rawValue == null)
                    {
                        if (p.HasDefaultValue) return p.DefaultValue;
                        if (p.ParameterType.IsValueType && Nullable.GetUnderlyingType(p.ParameterType) == null)
                            throw new ArgumentException($"Отсутствует обязательный параметр: {p.Name}");
                        return null;
                    }

                    Type targetType = Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType;
                    return Convert.ChangeType(rawValue, targetType);
                })
                .ToArray();
        }
    }
}
