using System.Net;
using System.Text;

namespace MiniHttpServer.Framework.Results;

public sealed class HtmlResult : IHttpResult
{
    private readonly byte[] _body;
    private int _executed;
    public int StatusCode { get; }

    public HtmlResult(string html, int statusCode = 200)
    {
        _body = Encoding.UTF8.GetBytes(html ?? string.Empty);
        StatusCode = ValidateStatusCode(statusCode);
    }

    public async Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (Interlocked.Exchange(ref _executed, 1) != 0)
            throw new InvalidOperationException("Этот HTTP-результат уже был выполнен.");

        response.StatusCode = StatusCode;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = _body.LongLength;
        response.Headers["X-Content-Type-Options"] = "nosniff";
        await response.OutputStream.WriteAsync(_body, cancellationToken).ConfigureAwait(false);
    }

    internal static int ValidateStatusCode(int statusCode)
    {
        if (statusCode is < 100 or > 599)
            throw new ArgumentOutOfRangeException(nameof(statusCode), "HTTP-статус должен быть в диапазоне 100–599.");
        return statusCode;
    }
}
