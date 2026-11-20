using System.Net;
using System.Text;
using MiniHttpServer.Framework.Content;

namespace MiniHttpServer.Framework.Handlers;

public sealed class StaticFilesHandler : RequestHandlerBase
{
    private readonly string _staticRoot;
    private readonly string _rootPrefix;

    public StaticFilesHandler(string staticRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(staticRoot);
        _staticRoot = Path.GetFullPath(staticRoot);
        Directory.CreateDirectory(_staticRoot);
        _rootPrefix = _staticRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        string method = context.Request.HttpMethod.ToUpperInvariant();

        if (method is not ("GET" or "HEAD"))
        {
            await HandleNextAsync(context);
            return;
        }

        string requestPath = context.Request.Url?.AbsolutePath ?? "/";
        string? filePath = ResolveFilePath(requestPath);

        if (filePath is null || !File.Exists(filePath))
        {
            await HandleNextAsync(context);
            return;
        }

        try
        {
            var fileInfo = new FileInfo(filePath);
            byte[]? responseBytes = null;

            string relativeFile = Path.GetRelativePath(_staticRoot, filePath)
                .Replace(Path.DirectorySeparatorChar, '/');

            if (method == "GET" &&
                string.Equals(relativeFile, "Steam/index.html", StringComparison.OrdinalIgnoreCase))
            {
                string html = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                const string scriptTag = "<script src=\"/Steam/SteamFormIntegration.js\" defer></script>";

                if (!html.Contains("/Steam/SteamFormIntegration.js", StringComparison.OrdinalIgnoreCase))
                {
                    int bodyEnd = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
                    html = bodyEnd >= 0
                        ? html.Insert(bodyEnd, scriptTag + Environment.NewLine)
                        : html + Environment.NewLine + scriptTag;
                }

                responseBytes = Encoding.UTF8.GetBytes(html);
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = MimeTypeProvider.GetMimeType(fileInfo.Extension);
            context.Response.ContentLength64 = responseBytes?.LongLength ?? fileInfo.Length;
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Cache-Control"] = "no-cache";

            if (method == "GET")
            {
                if (responseBytes is not null)
                {
                    await context.Response.OutputStream.WriteAsync(responseBytes);
                }
                else
                {
                    await using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read,
                        FileShare.Read, bufferSize: 81920, useAsync: true);
                    await stream.CopyToAsync(context.Response.OutputStream);
                }
            }

            Console.WriteLine($"200 {method} {context.Request.RawUrl}");
        }
        catch (FileNotFoundException)
        {
            await HandleNextAsync(context);
        }
        catch (DirectoryNotFoundException)
        {
            await HandleNextAsync(context);
        }
    }

    private string? ResolveFilePath(string requestPath)
    {
        string decodedPath;
        try { decodedPath = Uri.UnescapeDataString(requestPath); }
        catch (UriFormatException) { return null; }

        if (decodedPath.Contains('\\') || decodedPath.Contains('\0'))
            return null;

        string relativePath = decodedPath.TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        string candidate;
        try
        {
            candidate = Path.GetFullPath(Path.Combine(_staticRoot, relativePath));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        if (!candidate.StartsWith(_rootPrefix, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(candidate, _staticRoot, StringComparison.OrdinalIgnoreCase))
            return null;

        if (Directory.Exists(candidate))
            candidate = Path.Combine(candidate, "index.html");
        else if (string.IsNullOrEmpty(relativePath))
            candidate = Path.Combine(_staticRoot, "index.html");

        return candidate;
    }
}
