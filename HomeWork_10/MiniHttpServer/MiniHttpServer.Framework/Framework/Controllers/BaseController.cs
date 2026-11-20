using System.Net;
using MiniHttpServer.Framework.Authentication;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.Framework.Controllers;

public abstract class BaseController
{
    protected HttpListenerContext Context { get; private set; } = null!;
    protected IUserSessionService SessionService { get; private set; } = null!;

    private IHtmlTemplateRenderer _renderer = null!;
    private string _templatesRoot = null!;

    internal void InitializeContext(
        HttpListenerContext context,
        IHtmlTemplateRenderer htmlRenderer,
        string root,
        IUserSessionService sessionService)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _renderer = htmlRenderer ?? throw new ArgumentNullException(nameof(htmlRenderer));
        _templatesRoot = Path.GetFullPath(root ?? throw new ArgumentNullException(nameof(root)));
        SessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
    }

    protected IHttpResult Html(string html, int statusCode = 200) => new HtmlResult(html, statusCode);
    protected IHttpResult Json(object? data, int statusCode = 200) => new JsonResult(data, statusCode);
    protected IHttpResult Page(string templatePath, object model, int statusCode = 200)
        => new PageResult(_renderer, _templatesRoot, templatePath, model, statusCode);
}
