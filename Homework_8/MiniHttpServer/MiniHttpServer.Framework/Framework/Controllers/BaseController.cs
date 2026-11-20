using System.Net;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.HtmlTemplating;
namespace MiniHttpServer.Framework.Controllers;
public abstract class BaseController
{
    protected HttpListenerContext Context { get; private set; } = null!;
    private IHtmlTemplateRenderer renderer = null!;
    private string templatesRoot = null!;
    internal void InitializeContext(HttpListenerContext context,IHtmlTemplateRenderer htmlRenderer,string root)
    { Context=context??throw new ArgumentNullException(nameof(context)); renderer=htmlRenderer??throw new ArgumentNullException(nameof(htmlRenderer)); templatesRoot=Path.GetFullPath(root); }
    protected IHttpResult Html(string html,int statusCode=200)=>new HtmlResult(html,statusCode);
    protected IHttpResult Json(object? data,int statusCode=200)=>new JsonResult(data,statusCode);
    protected IHttpResult Page(string templatePath,object model,int statusCode=200)=>new PageResult(renderer,templatesRoot,templatePath,model,statusCode);
}
