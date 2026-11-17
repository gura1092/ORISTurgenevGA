using System.Net;
using System.Text;

namespace ORISFigmaServer;

internal sealed class HttpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _staticRoot;
    private readonly string _prefix;
    private bool _stopping;

    public HttpServer(string prefix, string staticRoot)
    {
        _prefix = prefix.EndsWith('/') ? prefix : prefix + "/";
        _staticRoot = Path.GetFullPath(staticRoot);
        Directory.CreateDirectory(_staticRoot);
        _listener.Prefixes.Add(_prefix);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener.Start();
        Console.WriteLine($"Сервер запущен: {_prefix}");
        Console.WriteLine($"Каталог статических файлов: {_staticRoot}");

        using var registration = cancellationToken.Register(Stop);

        while (!_stopping && _listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException) when (_stopping) { break; }
            catch (ObjectDisposedException) when (_stopping) { break; }

            // Не блокируем приём следующих запросов обработкой текущего.
            _ = Task.Run(() => ProcessRequestAsync(context));
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        try
        {
            if (context.Request.HttpMethod != "GET" &&
                context.Request.HttpMethod != "HEAD")
            {
                context.Response.Headers[HttpResponseHeader.Allow] = "GET, HEAD";
                await SendTextAsync(context, 405, "Method Not Allowed", "text/plain; charset=utf-8");
                return;
            }

            string rawPath = context.Request.Url?.AbsolutePath ?? "/";
            string checkDir = Path.Combine(_staticRoot, rawPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!rawPath.EndsWith('/') && Directory.Exists(checkDir))
            {
                context.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
                context.Response.RedirectLocation = rawPath + "/";
                context.Response.Close();
                return;
            }

            string? filePath = ResolveFilePath(rawPath);

            if (filePath is null || !File.Exists(filePath))
            {
                await SendNotFoundAsync(context);
                return;
            }

            byte[] content = await File.ReadAllBytesAsync(filePath);
            HttpListenerResponse response = context.Response;
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = MimeTypeProvider.GetMimeType(Path.GetExtension(filePath));
            response.ContentLength64 = content.Length;
            response.Headers["Server"] = "MiniHttpServer";

            if (context.Request.HttpMethod == "GET")
                await response.OutputStream.WriteAsync(content);

            Console.WriteLine($"200 {context.Request.RawUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки запроса: {ex.Message}");
            try
            {
                if (context.Response.OutputStream.CanWrite)
                    await SendTextAsync(context, 500, "Internal Server Error",
                        "text/plain; charset=utf-8");
            }
            catch (Exception responseException)
            {
                Console.WriteLine($"Не удалось отправить ответ: {responseException.Message}");
            }
        }
        finally
        {
            context.Response.Close();
        }
    }

    private string? ResolveFilePath(string requestPath)
    {
        string decodedPath;
        try { decodedPath = Uri.UnescapeDataString(requestPath); }
        catch { return null; }

        // Декодируем URL, приводим разделители к формату ОС и отсекаем traversal.
        decodedPath = decodedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string candidate = Path.GetFullPath(Path.Combine(_staticRoot, decodedPath));

        string rootWithSeparator = _staticRoot.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(candidate, _staticRoot, StringComparison.OrdinalIgnoreCase))
            return null;

        if (Directory.Exists(candidate))
            candidate = Path.Combine(candidate, "index.html");
        else if (string.IsNullOrEmpty(decodedPath))
            candidate = Path.Combine(_staticRoot, "index.html");

        return candidate;
    }

    private static async Task SendNotFoundAsync(HttpListenerContext context)
    {
        string custom404 = Path.Combine(AppContext.BaseDirectory, "static", "404.html");
        string message = File.Exists(custom404)
            ? await File.ReadAllTextAsync(custom404)
            : "<!doctype html><html lang=\"ru\"><meta charset=\"utf-8\"><title>404</title><h1>404 Not Found</h1><p>Запрашиваемый ресурс не найден.</p></html>";

        byte[] bytes = Encoding.UTF8.GetBytes(message);
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        context.Response.Headers["Server"] = "MiniHttpServer";
        if (context.Request.HttpMethod != "HEAD")
            await context.Response.OutputStream.WriteAsync(bytes);

        Console.WriteLine($"404 {context.Request.RawUrl}");
    }

    private static async Task SendTextAsync(HttpListenerContext context, int statusCode,
        string message, string contentType)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = contentType;
        context.Response.ContentLength64 = bytes.Length;
        context.Response.Headers["Server"] = "MiniHttpServer";
        await context.Response.OutputStream.WriteAsync(bytes);
    }

    public void Stop()
    {
        if (_stopping) return;
        _stopping = true;
        if (_listener.IsListening) _listener.Stop();
        _listener.Close();
    }

    public void Dispose() => Stop();
}
