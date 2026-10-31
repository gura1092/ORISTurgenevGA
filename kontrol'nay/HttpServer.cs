using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static Samostaytelnay.MemoryStorage;

namespace Samostaytelnay
{
    internal class HttpServer
    {
        private readonly HttpListener _listener;
        private readonly InMemoryStorage _storage;
        private readonly MovieLogic _logic;
        private readonly HtmlTemp _template;
        private readonly HttpHandler _handler;
        private bool _isRunning = true;

        public HttpServer(int port, InMemoryStorage storage, MovieLogic logic, HtmlTemp template, HttpHandler handler)
        {
            _storage = storage; _logic = logic; _template = template; _handler = handler;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine("HTTP-сервер успешно запущен.\r\nЧтобы перейти на server, откройте браузер и введите: http://localhost:8080/movies\r\nНажмите клавишу enter для остановки сервера.");
            while (_isRunning)
            {
                try { ProcessRequest(_listener.GetContext()); } catch { }
            }
        }

        public void Stop() { _isRunning = false; _listener.Stop(); }

        private void ProcessRequest(HttpListenerContext context)
        {
            var req = context.Request; var res = context.Response;
            string path = req.Url.AbsolutePath.ToLower(); string method = req.HttpMethod.ToUpper();

            try
            {
                if (path == "/movies" && method == "GET")
                {
                    var query = HttpUtility.ParseQueryString(req.Url.Query);
                    var rawData = _storage.FetchAll();
                    var processedData = _logic.FilterAndSort(rawData, query["genre"]);
                    _handler.SendHtml(res, _template.RenderIndexPage(processedData, query["genre"]), HttpStatusCode.OK);
                    return;
                }

                if (path == "/movies/details" && method == "GET")
                {
                    var query = HttpUtility.ParseQueryString(req.Url.Query);
                    if (!int.TryParse(query["id"], out int id)) { _handler.SendError(res, HttpStatusCode.BadRequest, "Bad Request ID"); return; }
                    var item = _storage.FindById(id);
                    if (item == null) { _handler.SendError(res, HttpStatusCode.NotFound, "Not Found"); return; }
                    _handler.SendHtml(res, _template.RenderDetailsPage(item), HttpStatusCode.OK);
                    return;
                }

                if (path == "/movies/create" && method == "POST")
                {
                    var form = ReadForm(req);
                    if (!_logic.Validate(form["title"], form["genre"], "planned", out string err)) { _handler.SendError(res, HttpStatusCode.BadRequest, err); return; }
                    _storage.Insert(form["title"], form["genre"]);
                    _handler.Redirect303(res, "/movies");
                    return;
                }

                if (path == "/movies/update" && method == "PUT")
                {
                    var query = HttpUtility.ParseQueryString(req.Url.Query);
                    if (!int.TryParse(query["id"], out int id)) { _handler.SendError(res, HttpStatusCode.BadRequest, "Bad ID"); return; }
                    var form = ReadForm(req);
                    if (!_logic.Validate(form["title"], form["genre"], form["status"], out string err)) { _handler.SendError(res, HttpStatusCode.BadRequest, err); return; }
                    if (!_storage.Modify(id, form["title"], form["genre"], form["status"])) { _handler.SendError(res, HttpStatusCode.NotFound, "Not Found"); return; }
                    _handler.Send204NoContent(res);
                    return;
                }

                _handler.SendError(res, HttpStatusCode.NotFound, "Маршрут отсутствует");
            }
            catch (Exception ex) { _handler.SendError(res, HttpStatusCode.InternalServerError, ex.Message); }
        }

        private System.Collections.Specialized.NameValueCollection ReadForm(HttpListenerRequest request)
        {
            using (var r = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
                return HttpUtility.ParseQueryString(r.ReadToEnd());
        }
    }
}
