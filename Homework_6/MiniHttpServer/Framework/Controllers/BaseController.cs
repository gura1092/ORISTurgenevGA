using System.Net;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Framework.Controllers;

public abstract class BaseController
{
    protected HttpListenerContext Context { get; private set; } = null!;

    internal void InitializeContext(HttpListenerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Context = context;
    }

    protected IHttpResult Html(string html, int statusCode = 200) => new HtmlResult(html, statusCode);
    protected IHttpResult Json(object? data, int statusCode = 200) => new JsonResult(data, statusCode);
}
