using System.Net;
using System.Text.Json;

namespace MiniHttpServer.Framework.Results;

public sealed class JsonResult : IHttpResult
{
    private readonly byte[] _body;
    private int _executed;
    public int StatusCode { get; }

    public JsonResult(object? data, int statusCode = 200)
    {
        _body = JsonSerializer.SerializeToUtf8Bytes(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        StatusCode = HtmlResult.ValidateStatusCode(statusCode);
    }

    public async Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (Interlocked.Exchange(ref _executed, 1) != 0)
            throw new InvalidOperationException("Этот HTTP-результат уже был выполнен.");

        response.StatusCode = StatusCode;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = _body.LongLength;
        response.Headers["X-Content-Type-Options"] = "nosniff";
        await response.OutputStream.WriteAsync(_body, cancellationToken).ConfigureAwait(false);
    }
}
