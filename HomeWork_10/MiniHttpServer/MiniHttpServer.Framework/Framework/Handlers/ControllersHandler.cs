using System.Net;
using System.Reflection;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Authentication;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Routing;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.Framework.Handlers;

public sealed class ControllersHandler : RequestHandlerBase
{
    private readonly IReadOnlyDictionary<string, Type> _controllers;
    private readonly Func<Type, object> _controllerFactory;
    private readonly IHtmlTemplateRenderer _renderer;
    private readonly string _templatesRoot;
    private readonly IUserSessionService _sessionService;

    public ControllersHandler(
        Assembly assembly,
        IHtmlTemplateRenderer renderer,
        string templatesRoot,
        Func<Type, object>? controllerFactory = null,
        IUserSessionService? sessionService = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _templatesRoot = Path.GetFullPath(templatesRoot ?? throw new ArgumentNullException(nameof(templatesRoot)));
        _controllerFactory = controllerFactory ?? (type => Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Не удалось создать {type.Name}."));
        _sessionService = sessionService ?? AnonymousSessionService.Instance;
        var found = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (Type type in GetLoadableTypes(assembly))
        {
            if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters ||
                type.GetCustomAttribute<ControllerAttribute>() is null || !typeof(BaseController).IsAssignableFrom(type))
                continue;
            if (type.GetConstructor(Type.EmptyTypes) is null && controllerFactory is null) continue;
            string route = type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
                ? type.Name[..^"Controller".Length] : type.Name;
            if (!found.TryAdd(route, type)) throw new InvalidOperationException($"Дублирующий контроллер /{route}.");
        }
        _controllers = found;
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        string[] segments = GetRouteSegments(context.Request.Url?.AbsolutePath);
        if (segments.Length != 2 || !_controllers.TryGetValue(segments[0], out Type? controllerType))
        { await HandleNextAsync(context); return; }

        MethodInfo? action = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(IsValidAction).FirstOrDefault(m => m.Name.Equals(segments[1], StringComparison.OrdinalIgnoreCase));
        if (action is null) { await HandleNextAsync(context); return; }

        bool get = action.GetCustomAttribute<GetAttribute>() is not null;
        bool post = action.GetCustomAttribute<PostAttribute>() is not null;
        string method = context.Request.HttpMethod.ToUpperInvariant();
        string allowed = get && post ? "GET, POST" : get ? "GET" : "POST";
        if ((method == "GET" && !get) || (method == "POST" && !post) || method is not ("GET" or "POST"))
        { await WriteErrorAsync(context, 405, "HTTP-метод не разрешён для этого маршрута.", allowed); return; }

        string? sessionId = context.Request.Cookies["sessionId"]?.Value;
        if (!AuthorizationPolicy.IsAuthorized(controllerType, action, sessionId, _sessionService))
        {
            await WriteErrorAsync(context, 401, "Требуется аутентификация.");
            return;
        }

        try
        {
            object controller = _controllerFactory(controllerType);
            if (controller is not BaseController baseController)
                throw new InvalidOperationException("Контроллер должен наследоваться от BaseController.");
            baseController.InitializeContext(context, _renderer, _templatesRoot, _sessionService);

            object?[] args = await RequestParameterBinder.BindAsync(action, context);
            object? result = action.Invoke(controller, args);

            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                result = task.GetType()
                    .GetProperty("Result")
                    ?.GetValue(task);
            }

            if (result is not IHttpResult httpResult)
                throw new InvalidOperationException(
                    "Action должен возвращать IHttpResult или Task<IHttpResult>.");

            await httpResult.ExecuteAsync(context.Response);

            Console.WriteLine($"{context.Response.StatusCode} {method} {context.Request.RawUrl} -> {controllerType.Name}.{action.Name}");
        }
        catch (RequestBindingException ex) { await WriteErrorAsync(context, ex.StatusCode, ex.Message); }
        catch (ControllerActionException ex) { await WriteErrorAsync(context, ex.StatusCode, ex.Message); }
        catch (TargetInvocationException ex) when (ex.InnerException is ControllerActionException actionEx)
        { await WriteErrorAsync(context, actionEx.StatusCode, actionEx.Message); }
        catch (Exception ex)
        {
            Console.WriteLine($"[ОШИБКА ACTION] {controllerType.Name}.{action.Name}: {ex}");
            await WriteErrorAsync(context, 500, "Внутренняя ошибка сервера.");
        }
    }

    private static bool IsValidAction(MethodInfo method)
    {
        if (method.IsSpecialName || method.IsStatic || method.IsGenericMethodDefinition ||
            method.GetCustomAttribute<GetAttribute>() is null && method.GetCustomAttribute<PostAttribute>() is null)
            return false;
        Type t = method.ReturnType;
        return typeof(IHttpResult).IsAssignableFrom(t) ||
            (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>) && typeof(IHttpResult).IsAssignableFrom(t.GetGenericArguments()[0]));
    }

    private static string[] GetRouteSegments(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return Array.Empty<string>();
        string[] raw = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (raw.Length != 2) return Array.Empty<string>();
        try
        {
            string[] parts = raw.Select(Uri.UnescapeDataString).ToArray();
            if (parts.Any(x => string.IsNullOrWhiteSpace(x) || x.Contains('/') || x.Contains('\\') || x is "." or ".."))
                return Array.Empty<string>();
            return parts;
        }
        catch (UriFormatException) { return Array.Empty<string>(); }
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null).Cast<Type>().ToArray(); }
    }

    private sealed class AnonymousSessionService : IUserSessionService
    {
        internal static readonly AnonymousSessionService Instance = new();
        private AnonymousSessionService() { }
        public string CreateSession(int userId) => throw new InvalidOperationException("Сервис сеансов не передан в ControllersHandler.");
        public int? GetUserId(string? sessionId) => null;
        public void RemoveSession(string? sessionId) { }
    }

    private static async Task WriteErrorAsync(HttpListenerContext context, int status, string message, string? allow = null)
    {
        if (!string.IsNullOrWhiteSpace(allow)) context.Response.Headers[HttpResponseHeader.Allow] = allow;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes($"{status} {message}");
        context.Response.StatusCode = status;
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (!context.Request.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            await context.Response.OutputStream.WriteAsync(bytes);
    }
}
