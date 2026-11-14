using System.IO;

namespace MiniHttpServer.Framework
{
    public static class MimeTypeResolver
    {
        public static string GetMimeType(string filePathOrExtension)
        {
            string ext = filePathOrExtension.StartsWith(".")
                ? filePathOrExtension
                : Path.GetExtension(filePathOrExtension);

            switch (ext?.ToLowerInvariant())
            {
                case ".html":
                case ".htm":
                    return "text/html; charset=utf-8";
                case ".css":
                    return "text/css; charset=utf-8";
                case ".js":
                    return "text/javascript; charset=utf-8";
                case ".json":
                    return "application/json; charset=utf-8";
                case ".png":
                    return "image/png";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                case ".ico":
                    return "image/x-icon";
                case ".svg":
                    return "image/svg+xml";
                default:
                    return "application/octet-stream";
            }
        }
    }
}
