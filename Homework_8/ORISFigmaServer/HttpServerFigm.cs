using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using MiniHttpServer.Framework;
using MiniHttpServer.HtmlTemplating;

namespace ORISFigmaServer
{
    internal class HttpServer
    {
        private readonly HttpListener _listener;
        private readonly string _htmlFile;
        private readonly IHtmlTemplateRenderer _templateRenderer;

        public HttpServer(string url, string htmlFile)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _htmlFile = htmlFile;
            _templateRenderer = new HtmlTemplateRenderer();
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
                string path = context.Request.Url.AbsolutePath.Trim('/');
                var urlParts = path.Split('/');

                if (urlParts.Length >= 2)
                {
                    string controllerUrlName = urlParts[0].Replace("-", "");
                    string methodName = urlParts[1];

                    var assembly = Assembly.GetExecutingAssembly();

                    var controllerType = assembly.GetTypes()
                        .FirstOrDefault(t => (t.Name.Equals(controllerUrlName, StringComparison.OrdinalIgnoreCase) ||
                                              t.Name.Equals(controllerUrlName + "Controller", StringComparison.OrdinalIgnoreCase))
                                              && Attribute.IsDefined(t, typeof(HttpController)));

                    if (controllerType != null)
                    {
                        var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                        if (method != null)
                        {
                            NameValueCollection parametersCollection;

                            if (context.Request.HttpMethod == "POST")
                            {
                                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                                {
                                    string requestBody = reader.ReadToEnd();
                                    parametersCollection = HttpUtility.ParseQueryString(requestBody);
                                }
                            }
                            else
                            {
                                parametersCollection = context.Request.QueryString;
                            }

                            object controllerInstance = Activator.CreateInstance(controllerType);

                            object[] queryParams = method.GetParameters()
                                .Select(p =>
                                {
                                    string value = parametersCollection[p.Name];
                                    return value != null ? Convert.ChangeType(value, p.ParameterType) : null;
                                })
                                .ToArray();

                            object result = method.Invoke(controllerInstance, queryParams);

                            // Ожидаем результат, если метод контроллера асинхронный (Task<IHttpResult>)
                            if (result is System.Threading.Tasks.Task task)
                            {
                                task.GetAwaiter().GetResult();
                                var resultProp = task.GetType().GetProperty("Result");
                                result = resultProp?.GetValue(task);
                            }

                            // Если контроллер вернул IHttpResult (например, PageResult) — выполняем его
                            if (result is IHttpResult httpResult)
                            {
                                httpResult.Execute(context);
                            }
                            else
                            {
                                context.Response.StatusCode = 200;
                                context.Response.ContentType = "text/html; charset=utf-8";
                                byte[] responseBuffer = Encoding.UTF8.GetBytes("<h1>Действие успешно выполнено контроллером!</h1>");
                                context.Response.ContentLength64 = responseBuffer.Length;
                                context.Response.OutputStream.Write(responseBuffer, 0, responseBuffer.Length);
                                context.Response.OutputStream.Close();
                            }

                            Console.WriteLine($"Запрос к [{controllerType.Name}.{method.Name}] успешно обработан");
                            return;
                        }
                    }
                }

                string filePath = GetFilePath(context.Request);

                if (!File.Exists(filePath))
                {
                    SendMistake404(context, filePath);
                    return;
                }

                string extension = Path.GetExtension(filePath);
                string contentType = GetMimeType(extension);
                byte[] buffer = File.ReadAllBytes(filePath);

                // Обработка статических HTML-файлов шаблонизатором
                if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".htm", StringComparison.OrdinalIgnoreCase))
                {
                    string rawHtml = Encoding.UTF8.GetString(buffer);
                    var templateData = new
                    {
                        Title = "Steam",
                        IsAuthorized = false,
                        Games = new[]
                        {
                            new { Name = "Counter-Strike 2", Price = "Бесплатно", IsDiscount = false },
                            new { Name = "Cyberpunk 2077", Price = "1999 руб.", IsDiscount = true }
                        }
                    };

                    string renderedHtml = _templateRenderer.RenderFromString(rawHtml, templateData);
                    buffer = Encoding.UTF8.GetBytes(renderedHtml);
                }

                context.Response.ContentType = contentType;
                context.Response.ContentLength64 = buffer.Length;
                context.Response.StatusCode = 200;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();

                Console.WriteLine("Статический файл успешно отдан");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка сервера: " + ex.Message);
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    byte[] errBuffer = Encoding.UTF8.GetBytes($"<h1>500 Internal Server Error</h1><p>{ex.InnerException?.Message ?? ex.Message}</p>");
                    context.Response.ContentLength64 = errBuffer.Length;
                    context.Response.OutputStream.Write(errBuffer, 0, errBuffer.Length);
                    context.Response.OutputStream.Close();
                }
                catch { }
            }
        }

        private string GetFilePath(HttpListenerRequest request)
        {
            string requestPath = request.Url.AbsolutePath;
            string directory = Path.GetDirectoryName(_htmlFile);

            if (requestPath == "/" || string.IsNullOrEmpty(requestPath))
                return _htmlFile;

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

            Console.WriteLine("Запрос обработан (404)");
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
            Console.WriteLine("Сервер остановлен.");
        }
    }
}
