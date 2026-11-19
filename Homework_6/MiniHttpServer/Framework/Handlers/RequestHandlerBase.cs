using System.Net;

namespace MiniHttpServer.Framework.Handlers;

public abstract class RequestHandlerBase
{
    private RequestHandlerBase? _next;

    public RequestHandlerBase SetNext(RequestHandlerBase next)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (ReferenceEquals(this, next))
            throw new InvalidOperationException("Обработчик не может ссылаться сам на себя.");

        _next = next;
        return next;
    }

    public abstract Task HandleAsync(HttpListenerContext context);

    protected Task HandleNextAsync(HttpListenerContext context) =>
        _next is null ? Task.CompletedTask : _next.HandleAsync(context);

    protected bool HasNext => _next is not null;
}
