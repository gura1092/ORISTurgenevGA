using System.Net;
using System.Text;

namespace MiniHttpServer.Framework.Handlers;

internal sealed class NotFoundHandler : RequestHandlerBase
{
    private readonly string _custom404Path;

    public NotFoundHandler(string staticRoot)
    {
        _custom404Path = Path.Combine(staticRoot, "404.html");
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        string message = File.Exists(_custom404Path)
            ? await File.ReadAllTextAsync(_custom404Path)
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
}
