using System.Net;
using System.Text;
using MiniHttpServer.Framework.Handlers;

namespace MiniHttpServer.Framework;

public sealed class HttpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly RequestHandlerBase _firstHandler;
    private readonly string _prefix;
    private volatile bool _stopping;

    public HttpServer(string prefix, RequestHandlerBase firstHandler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentNullException.ThrowIfNull(firstHandler);

        if (!Uri.TryCreate(prefix, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Укажите корректный HTTP/HTTPS адрес сервера.", nameof(prefix));

        _prefix = prefix.EndsWith('/') ? prefix : prefix + "/";
        _firstHandler = firstHandler;
        _listener.Prefixes.Add(_prefix);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener.Start();
        Console.WriteLine($"Сервер запущен: {_prefix}");

        using CancellationTokenRegistration registration = cancellationToken.Register(Stop);

        while (!_stopping && _listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException) when (_stopping)
            {
                break;
            }
            catch (ObjectDisposedException) when (_stopping)
            {
                break;
            }

            _ = ProcessRequestAsync(context);
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        try
        {
            await _firstHandler.HandleAsync(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ОШИБКА 500] {context.Request.HttpMethod} {context.Request.RawUrl}: {ex}");
            try
            {
                if (context.Response.OutputStream.CanWrite)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes("500 Internal Server Error");
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes);
                }
            }
            catch (Exception responseException)
            {
                Console.WriteLine($"Не удалось отправить ответ: {responseException.Message}");
            }
        }
        finally
        {
            try { context.Response.Close(); }
            catch (Exception ex) { Console.WriteLine($"Ошибка закрытия ответа: {ex.Message}"); }
        }
    }

    public void Stop()
    {
        if (_stopping)
            return;

        _stopping = true;
        try
        {
            if (_listener.IsListening)
                _listener.Stop();
        }
        catch (HttpListenerException) { }
        finally
        {
            _listener.Close();
        }
    }

    public void Dispose() => Stop();
}
