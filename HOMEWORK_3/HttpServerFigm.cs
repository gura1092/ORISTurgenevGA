using System;
using System.IO;
using System.Net;
using System.Text;

namespace ORISFigmaServer
{
    internal class HttpServer
    {
        private readonly HttpListener _listener;
        private readonly string _htmlFile;

        public HttpServer(string url, string htmlFile)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _htmlFile = htmlFile;
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine("Сервер запущен!");

            while (true)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    ProcessRequest(context);
                }
                catch (Exception ex) when (ex is HttpListenerException || ex is ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                string filePath = GetFilePath(context.Request);

                if (!File.Exists(filePath))
                {
                    SendMistake404(context, filePath);
                    return;
                }

                string extension = Path.GetExtension(filePath);
                string contentType = GetMimeType(extension);
                byte[] buffer = File.ReadAllBytes(filePath);

                context.Response.ContentType = contentType;
                context.Response.ContentLength64 = buffer.Length;
                context.Response.StatusCode = 200;

                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();

                Console.WriteLine("Запрос обработан");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при обработке запроса: " + ex.Message);
                try
                {
                    string errorMessage = $"<h1>500 Внутренняя ошибка сервера</h1><p>Подробности: {ex.Message}</p>";
                    byte[] buffer = Encoding.UTF8.GetBytes(errorMessage);

                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.ContentLength64 = buffer.Length;

                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Close();
                }
                catch (Exception responseEx)
                {
                    Console.WriteLine("Не удалось закрыть поток ответа: " + responseEx.Message);
                }
            }
        }

        private string GetFilePath(HttpListenerRequest request)
        {
            string requestPath = request.Url.AbsolutePath;
            string directory = Path.GetDirectoryName(_htmlFile);

            if (requestPath == "/" || string.IsNullOrEmpty(requestPath))
            {
                return _htmlFile;
            }

            string fileName = requestPath.TrimStart('/');
            return Path.Combine(directory, fileName);
        }

        private string GetMimeType(string extension)
        {
            switch (extension.ToLower())
            {
                case ".html": case ".htm": return "text/html; charset=utf-8";
                case ".css": return "text/css; charset=utf-8";
                case ".js": return "text/javascript; charset=utf-8";
                case ".json": return "application/json; charset=utf-8";
                case ".png": return "image/png";
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".ico": return "image/x-icon";
                case ".svg": return "image/svg+xml";
                default: return "application/octet-stream";
            }
        }

        private void SendMistake404(HttpListenerContext context, string filePath)
        {
            string message = $"<h1>404 Файл не найден</h1><p>Путь: {filePath}</p>";
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            context.Response.StatusCode = 404;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;

            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();

            Console.WriteLine("Запрос обработан");
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
            Console.WriteLine("Сервер остановлен.");
        }
    }
}
