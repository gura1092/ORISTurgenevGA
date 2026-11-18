using System.Net;
using System.Text;

namespace MiniHttpServer.Framework.Handlers;

public sealed class NotFoundHandler : RequestHandlerBase
{
    private readonly string _custom404Path;

    public NotFoundHandler(string staticRoot)
    {
        _custom404Path = Path.Combine(Path.GetFullPath(staticRoot), "404.html");
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        const string fallback =
            "<!doctype html><html lang=\"ru\"><meta charset=\"utf-8\"><title>404</title>" +
            "<h1>404 Not Found</h1><p>Запрашиваемый ресурс не найден.</p></html>";

        string message = fallback;
        try
        {
            if (File.Exists(_custom404Path))
                message = await File.ReadAllTextAsync(_custom404Path, Encoding.UTF8);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Не удалось прочитать 404.html: {ex.Message}");
        }

        byte[] bytes = Encoding.UTF8.GetBytes(message);
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        if (!context.Request.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            await context.Response.OutputStream.WriteAsync(bytes);

        Console.WriteLine($"404 {context.Request.HttpMethod} {context.Request.RawUrl}");
    }
}
