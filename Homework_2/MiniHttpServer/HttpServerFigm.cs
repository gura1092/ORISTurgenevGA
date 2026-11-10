using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer
{
    internal class HttpServerFigm
    {
        private readonly HttpListener _listener;
        private readonly string _staticFilesPath;
        private readonly string _defaultHtmlFile;
        private bool _isRunning;

        public HttpServerFigm(string url, string htmlFile)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _staticFilesPath = Path.GetDirectoryName(htmlFile);
            _defaultHtmlFile = Path.GetFileName(htmlFile);
        }

        public void Start()
        {
            _listener.Start();
            _isRunning = true;

            Console.WriteLine("Сервер запущен!");

            while (_isRunning)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();

                    Task.Run(() => ProcessRequest(context));
                }
                catch (HttpListenerException) when (!_isRunning)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Ошибка в цикле прослушивания: {ex.Message}");
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Запрос: {request.HttpMethod} {request.Url.LocalPath}");

            try
            {
                string localPath = request.Url.LocalPath.Trim('/');

                if (string.IsNullOrEmpty(localPath) || localPath.Equals("bratuha", StringComparison.OrdinalIgnoreCase))
                {
                    localPath = _defaultHtmlFile;
                }
                else
                {
                    if (localPath.StartsWith("bratuha/", StringComparison.OrdinalIgnoreCase))
                    {
                        localPath = localPath.Substring("bratuha/".Length);
                    }

                    localPath = localPath.Replace('/', Path.DirectorySeparatorChar);
                }

                string absolutePath = Path.Combine(_staticFilesPath, localPath);

                if (File.Exists(absolutePath))
                {
                    byte[] buffer = File.ReadAllBytes(absolutePath);

                    response.ContentType = GetContentType(Path.GetExtension(absolutePath));
                    response.ContentLength64 = buffer.Length;
                    response.StatusCode = (int)HttpStatusCode.OK;

                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    Console.WriteLine($"[WARNING] Файл не найден по пути: {absolutePath}");

                    string errorText = "<h1>404 Not Found</h1><p>Файл не найден сервером.</p>";
                    byte[] errorBuffer = Encoding.UTF8.GetBytes(errorText);

                    response.ContentType = "text/html; charset=utf-8";
                    response.ContentLength64 = errorBuffer.Length;
                    response.StatusCode = (int)HttpStatusCode.NotFound;

                    response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Исключение при обработке: {ex.Message}");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                try
                {
                    response.OutputStream.Close();
                }
                catch { }
            }
        }

        private string GetContentType(string extension)
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

        public void Stop()
        {
            if (_isRunning)
            {
                _isRunning = false;
                _listener.Stop();
                _listener.Close();
                Console.WriteLine("Сервер остановлен.");
            }
        }
    }
}
