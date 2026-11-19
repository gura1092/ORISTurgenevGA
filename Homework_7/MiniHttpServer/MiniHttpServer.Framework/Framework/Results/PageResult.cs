using System.Net;
using MiniHttpServer.HtmlTemplating;
namespace MiniHttpServer.Framework.Results;
public sealed class PageResult : IHttpResult
{
    private readonly IHtmlTemplateRenderer renderer; private readonly string root,path; private readonly object model; private readonly int status; private int executed;
    public PageResult(IHtmlTemplateRenderer renderer,string templateRoot,string templatePath,object model,int statusCode=200)
    {
        this.renderer=renderer??throw new ArgumentNullException(nameof(renderer));root=Path.GetFullPath(templateRoot??throw new ArgumentNullException(nameof(templateRoot)));ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);
        if(Path.IsPathRooted(templatePath))throw new ArgumentException("Путь к шаблону должен быть относительным.",nameof(templatePath));path=Path.GetFullPath(Path.Combine(root,templatePath));
        if(!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Путь выходит за каталог Templates.",nameof(templatePath));
        this.model=model??throw new ArgumentNullException(nameof(model));status=HtmlResult.ValidateStatusCode(statusCode);
    }
    public async Task ExecuteAsync(HttpListenerResponse response,CancellationToken cancellationToken=default)
    {ArgumentNullException.ThrowIfNull(response);if(Interlocked.Exchange(ref executed,1)!=0)throw new InvalidOperationException("HTTP-result уже выполнен.");string html=renderer.RenderFromFile(path,model);await new HtmlResult(html,status).ExecuteAsync(response,cancellationToken).ConfigureAwait(false);}
}
