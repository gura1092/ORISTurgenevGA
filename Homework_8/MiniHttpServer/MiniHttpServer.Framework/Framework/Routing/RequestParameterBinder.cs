using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;

namespace MiniHttpServer.Framework.Routing;

public static class RequestParameterBinder
{
    private const int MaxBodyBytes = 1024 * 1024;

    public static async Task<object?[]> BindAsync(MethodInfo method, HttpListenerContext context)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length == 0)
            return Array.Empty<object?>();

        IDictionary<string, string> values;

        if (context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string? key in context.Request.QueryString.AllKeys)
            {
                if (key is not null)
                    values[key] = context.Request.QueryString[key] ?? string.Empty;
            }
        }
        else if (context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            values = await ReadFormBodyAsync(context.Request);
        }
        else
        {
            throw new RequestBindingException(400, "Невозможно определить источник параметров запроса.");
        }

        var result = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            string name = parameter.Name ?? $"arg{i}";

            if (!values.TryGetValue(name, out string? rawValue))
            {
                if (parameter.HasDefaultValue)
                {
                    result[i] = parameter.DefaultValue;
                    continue;
                }

                if (IsNullable(parameter.ParameterType))
                {
                    result[i] = null;
                    continue;
                }

                throw new RequestBindingException(400,
                    $"Не передан обязательный параметр «{name}».");
            }

            try
            {
                result[i] = ConvertValue(rawValue, parameter.ParameterType);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
            {
                throw new RequestBindingException(400,
                    $"Параметр «{name}» имеет некорректное значение.");
            }
        }

        return result;
    }

    private static async Task<IDictionary<string, string>> ReadFormBodyAsync(HttpListenerRequest request)
    {
        string contentType = request.ContentType ?? string.Empty;
        string mediaType = contentType.Split(';', 2)[0].Trim();

        if (!string.Equals(mediaType, "application/x-www-form-urlencoded",
                StringComparison.OrdinalIgnoreCase))
            throw new RequestBindingException(415,
                "Для POST поддерживается Content-Type: application/x-www-form-urlencoded.");

        if (request.ContentLength64 > MaxBodyBytes)
            throw new RequestBindingException(413, "Размер тела запроса превышает 1 МБ.");

        Encoding encoding;
        try { encoding = request.ContentEncoding ?? Encoding.UTF8; }
        catch { encoding = Encoding.UTF8; }

        using var reader = new StreamReader(request.InputStream, encoding,
            detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

        char[] buffer = new char[4096];
        var builder = new StringBuilder();
        int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            if (encoding.GetByteCount(builder.ToString()) + encoding.GetByteCount(buffer, 0, read) > MaxBodyBytes)
                throw new RequestBindingException(413, "Размер тела запроса превышает 1 МБ.");

            builder.Append(buffer, 0, read);
        }

        return ParseForm(builder.ToString());
    }

    private static IDictionary<string, string> ParseForm(string body)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(body))
            return result;

        foreach (string pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = pair.IndexOf('=');
            string rawKey = separator >= 0 ? pair[..separator] : pair;
            string rawValue = separator >= 0 ? pair[(separator + 1)..] : string.Empty;

            string key = WebUtility.UrlDecode(rawKey);
            string value = WebUtility.UrlDecode(rawValue);

            if (string.IsNullOrWhiteSpace(key))
                continue;

            result[key] = value;
        }

        return result;
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        Type actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (actualType == typeof(string))
            return value;
        if (actualType == typeof(Guid))
            return Guid.Parse(value);
        if (actualType.IsEnum)
            return Enum.Parse(actualType, value, ignoreCase: true);
        if (actualType == typeof(bool))
        {
            if (value == "1") return true;
            if (value == "0") return false;
            return bool.Parse(value);
        }

        return Convert.ChangeType(value, actualType, CultureInfo.InvariantCulture);
    }

    private static bool IsNullable(Type type) =>
        Nullable.GetUnderlyingType(type) is not null;
}

public sealed class RequestBindingException : Exception
{
    public int StatusCode { get; }

    public RequestBindingException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
