namespace ORISFigmaServer;

internal static class MimeTypeProvider
{
    private static readonly Dictionary<string, string> MimeTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".html"] = "text/html; charset=utf-8",
            [".htm"] = "text/html; charset=utf-8",
            [".css"] = "text/css; charset=utf-8",
            [".js"] = "text/javascript; charset=utf-8",
            [".mjs"] = "text/javascript; charset=utf-8",
            [".json"] = "application/json; charset=utf-8",
            [".xml"] = "application/xml; charset=utf-8",
            [".txt"] = "text/plain; charset=utf-8",
            [".csv"] = "text/csv; charset=utf-8",
            [".pdf"] = "application/pdf",
            [".zip"] = "application/zip",
            [".wasm"] = "application/wasm",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".avif"] = "image/avif",
            [".bmp"] = "image/bmp",
            [".ico"] = "image/vnd.microsoft.icon",
            [".svg"] = "image/svg+xml",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff",
            [".mp3"] = "audio/mpeg",
            [".wav"] = "audio/wav",
            [".ogg"] = "audio/ogg",
            [".mp4"] = "video/mp4",
            [".webm"] = "video/webm",
            [".woff"] = "font/woff",
            [".woff2"] = "font/woff2",
            [".ttf"] = "font/ttf",
            [".otf"] = "font/otf",
            [".eot"] = "application/vnd.ms-fontobject"
        };

    public static string GetMimeType(string extension) =>
        MimeTypes.TryGetValue(extension, out string? mimeType)
            ? mimeType
            : "application/octet-stream";
}
