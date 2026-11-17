using System.Net;
using System.Text;
using MiniHttpServer.Framework.Handlers;

namespace MiniHttpServer.Framework;

internal sealed class HttpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly RequestHandlerBase _firstHandler;
    private readonly string _prefix;
    private bool _stopping;

    public HttpServer(string prefix, RequestHandlerBase firstHandler)
    {
        _prefix = prefix.EndsWith('/') ? prefix : prefix + "/";
        _firstHandler = firstHandler;
        _listener.Prefixes.Add(_prefix);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener.Start();
        Console.WriteLine($"Сервер запущен: {_prefix}");

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

            _ = Task.Run(() => ProcessRequestAsync(context));
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        try
        {
            await _firstHandler.HandleAsync(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ОШИБКА 500] Ошибка обработки запроса: {ex.Message}");
            try
            {
                if (context.Response.OutputStream.CanWrite)
                    await SendTextAsync(context, 500, "Internal Server Error",
                        "text/plain; charset=utf-8");
            }
            catch (Exception responseException)
            {
                Console.WriteLine($"[ОШИБКА] Не удалось отправить ответ: {responseException.Message}");
            }
        }
        finally
        {
            context.Response.Close();
        }
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
