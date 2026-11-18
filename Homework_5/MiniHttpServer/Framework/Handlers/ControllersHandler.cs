using System.Net;
using System.Reflection;
using System.Text;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Routing;

namespace MiniHttpServer.Framework.Handlers;

public sealed class ControllersHandler : RequestHandlerBase
{
    private readonly IReadOnlyDictionary<string, Type> _controllers;
    private readonly Func<Type, object> _controllerFactory;

    public ControllersHandler(
        Assembly assembly,
        Func<Type, object>? controllerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _controllerFactory = controllerFactory ?? (type =>
            Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Не удалось создать {type.Name}."));

        var controllers = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        foreach (Type type in GetLoadableTypes(assembly))
        {
            if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
                continue;

            if (type.GetCustomAttribute<ControllerAttribute>() is null)
                continue;

            if (type.GetConstructor(Type.EmptyTypes) is null && controllerFactory is null)
                continue;

            string routeName = type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
                ? type.Name[..^"Controller".Length]
                : type.Name;

            if (!controllers.TryAdd(routeName, type))
                throw new InvalidOperationException($"Обнаружено несколько контроллеров с маршрутом /{routeName}.");
        }

        _controllers = controllers;
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        string[] segments = GetRouteSegments(context.Request.Url?.AbsolutePath);

        if (segments.Length != 2 ||
            !_controllers.TryGetValue(segments[0], out Type? controllerType))
        {
            await HandleNextAsync(context);
            return;
        }

        string methodName = segments[1];
        MethodInfo? action = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(IsValidAction)
            .FirstOrDefault(method =>
                string.Equals(method.Name, methodName, StringComparison.OrdinalIgnoreCase));

        if (action is null)
        {
            await HandleNextAsync(context);
            return;
        }

        bool isGet = action.GetCustomAttribute<GetAttribute>() is not null;
        bool isPost = action.GetCustomAttribute<PostAttribute>() is not null;
        string requestMethod = context.Request.HttpMethod.ToUpperInvariant();

        if ((requestMethod == "GET" && !isGet) ||
            (requestMethod == "POST" && !isPost))
        {
            if (isGet || isPost)
            {
                string allow = string.Join(", ", new[]
                {
                    isGet ? "GET" : null,
                    isPost ? "POST" : null
                }.Where(value => value is not null));

                await WriteErrorAsync(context, 405,
                    "Метод HTTP не поддерживается для этого маршрута.", allow);
                return;
            }

            await HandleNextAsync(context);
            return;
        }

        if (!isGet && !isPost)
        {
            await HandleNextAsync(context);
            return;
        }

        if (requestMethod is not ("GET" or "POST"))
        {
            await WriteErrorAsync(context, 405,
                "Для маршрута разрешены только GET или POST.",
                isGet && isPost ? "GET, POST" : isGet ? "GET" : "POST");
            return;
        }

        try
        {
            object controller = _controllerFactory(controllerType);
            object?[] arguments = await RequestParameterBinder.BindAsync(action, context);
            object? result = action.Invoke(controller, arguments);

            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                result = task.GetType().IsGenericType
                    ? task.GetType().GetProperty("Result")?.GetValue(task)
                    : null;
            }

            string responseText = result?.ToString() ?? string.Empty;
            await WriteResponseAsync(context, 200, responseText, "text/plain; charset=utf-8");
            Console.WriteLine($"200 {context.Request.HttpMethod} {context.Request.RawUrl} -> {controllerType.Name}.{action.Name}");
        }
        catch (RequestBindingException ex)
        {
            await WriteErrorAsync(context, ex.StatusCode, ex.Message);
        }
        catch (ControllerActionException ex)
        {
            await WriteErrorAsync(context, ex.StatusCode, ex.Message);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ControllerActionException actionException)
        {
            await WriteErrorAsync(context, actionException.StatusCode, actionException.Message);
        }
        catch (TargetInvocationException ex)
        {
            Exception cause = ex.InnerException ?? ex;
            Console.WriteLine($"[ОШИБКА КОНТРОЛЛЕРА] {controllerType.Name}.{action.Name}: {cause}");
            await WriteErrorAsync(context, 500, "Внутренняя ошибка при выполнении действия контроллера.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ОШИБКА МАРШРУТА] {controllerType.Name}.{action.Name}: {ex}");
            await WriteErrorAsync(context, 500, "Внутренняя ошибка сервера.");
        }
    }

    private static bool IsValidAction(MethodInfo method)
    {
        if (method.IsSpecialName || method.IsStatic || method.IsGenericMethodDefinition)
            return false;

        if (method.GetCustomAttribute<GetAttribute>() is null &&
            method.GetCustomAttribute<PostAttribute>() is null)
            return false;

        Type returnType = method.ReturnType;
        return returnType == typeof(string) ||
               returnType == typeof(void) ||
               returnType == typeof(Task) ||
               (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>));
    }

    private static string[] GetRouteSegments(string? absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
            return Array.Empty<string>();

        string[] rawSegments = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (rawSegments.Length != 2)
            return Array.Empty<string>();

        try
        {
            string[] decoded = rawSegments.Select(Uri.UnescapeDataString).ToArray();
            if (decoded.Any(segment =>
                string.IsNullOrWhiteSpace(segment) ||
                segment.Contains('/') ||
                segment.Contains('\\') ||
                segment is "." or ".."))
                return Array.Empty<string>();

            return decoded;
        }
        catch (UriFormatException)
        {
            return Array.Empty<string>();
        }
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null).Cast<Type>().ToArray(); }
    }

    internal static async Task WriteResponseAsync(
        HttpListenerContext context, int statusCode, string text, string contentType)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = contentType;
        context.Response.ContentLength64 = bytes.Length;
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (context.Request.HttpMethod != "HEAD")
            await context.Response.OutputStream.WriteAsync(bytes);
    }

    internal static Task WriteErrorAsync(
        HttpListenerContext context, int statusCode, string message, string? allow = null)
    {
        if (!string.IsNullOrWhiteSpace(allow))
            context.Response.Headers[HttpResponseHeader.Allow] = allow;

        return WriteResponseAsync(context, statusCode,
            $"{statusCode} {HttpStatusDescription(statusCode)}\n{message}",
            "text/plain; charset=utf-8");
    }

    private static string HttpStatusDescription(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        404 => "Not Found",
        405 => "Method Not Allowed",
        413 => "Payload Too Large",
        415 => "Unsupported Media Type",
        _ => "Internal Server Error"
    };
}
