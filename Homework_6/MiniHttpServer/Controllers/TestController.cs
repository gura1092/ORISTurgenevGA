using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Controllers;

[Controller]
public sealed class TestController : BaseController
{
    [Get]
    public IHttpResult Index() => Html("<!doctype html><html lang=\"ru\"><meta charset=\"utf-8\"><h1>Hello from controller!</h1><p>Практика 6: HtmlResult.</p></html>");

    [Get]
    public IHttpResult Hello() => Html("<h1>Hello World!</h1>");

    [Get]
    public IHttpResult Details(int id, string name) => Html($"<h1>Details</h1><p>ID: {System.Net.WebUtility.HtmlEncode(id.ToString())}; Name: {System.Net.WebUtility.HtmlEncode(name)}</p>");

    [Post]
    public IHttpResult Submit(string demoUser, string message) => Json(new { success = true, demoUser, message });

    [Get]
    public IHttpResult Info() => Json(new { id = 1, message = "Hello World!", success = true });

    [Get]
    public IHttpResult RequestInfo() => Json(new
    {
        method = Context.Request.HttpMethod,
        path = Context.Request.Url?.PathAndQuery ?? string.Empty,
        userAgent = Context.Request.Headers["User-Agent"] ?? string.Empty
    });
}
