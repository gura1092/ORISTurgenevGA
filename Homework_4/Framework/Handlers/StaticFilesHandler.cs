using System.Net;
using System.Text;
using MiniHttpServer.Framework.Content;

namespace MiniHttpServer.Framework.Handlers;

internal sealed class StaticFilesHandler : RequestHandlerBase
{
    private readonly string _staticRoot;

    public StaticFilesHandler(string staticRoot)
    {
        _staticRoot = Path.GetFullPath(staticRoot);
        Directory.CreateDirectory(_staticRoot);
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        if (context.Request.HttpMethod != "GET" &&
            context.Request.HttpMethod != "HEAD")
        {
            context.Response.Headers[HttpResponseHeader.Allow] = "GET, HEAD";
            byte[] bytes = Encoding.UTF8.GetBytes("Method Not Allowed");
            context.Response.StatusCode = 405;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.Headers["Server"] = "MiniHttpServer";
            await context.Response.OutputStream.WriteAsync(bytes);

            Console.WriteLine($"[ОШИБКА 405] {context.Request.HttpMethod} {context.Request.RawUrl}");
            return;
        }

        string rawPath = context.Request.Url?.AbsolutePath ?? "/";
        string checkDir = Path.Combine(_staticRoot, rawPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!rawPath.EndsWith('/') && Directory.Exists(checkDir))
        {
            context.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
            context.Response.RedirectLocation = rawPath + "/";
            return;
        }

        string? filePath = ResolveFilePath(rawPath);

        if (filePath is null || !File.Exists(filePath))
        {
            await HandleNextAsync(context);
            return;
        }

        byte[] content = await File.ReadAllBytesAsync(filePath);
        HttpListenerResponse response = context.Response;
        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = MimeTypeProvider.GetMimeType(Path.GetExtension(filePath));
        response.ContentLength64 = content.Length;
        response.Headers["Server"] = "MiniHttpServer";

        if (context.Request.HttpMethod == "GET")
            await response.OutputStream.WriteAsync(content);

        Console.WriteLine($"200 {context.Request.RawUrl}");
    }

    private string? ResolveFilePath(string requestPath)
    {
        string decodedPath;
        try { decodedPath = Uri.UnescapeDataString(requestPath); }
        catch { return null; }

        decodedPath = decodedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string candidate = Path.GetFullPath(Path.Combine(_staticRoot, decodedPath));

        string rootWithSeparator = _staticRoot.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(candidate, _staticRoot, StringComparison.OrdinalIgnoreCase))
            return null;

        if (Directory.Exists(candidate))
            candidate = Path.Combine(candidate, "index.html");
        else if (string.IsNullOrEmpty(decodedPath))
            candidate = Path.Combine(_staticRoot, "index.html");

        return candidate;
    }
}
