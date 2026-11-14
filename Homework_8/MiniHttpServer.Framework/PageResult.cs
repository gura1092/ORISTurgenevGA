using System.Net;
using System.Text;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.Framework
{
    public class PageResult : IHttpResult
    {
        private readonly string _templatePath;
        private readonly object _dataModel;
        private readonly IHtmlTemplateRenderer _renderer;

        public PageResult(string templatePath, object dataModel = null)
            : this(templatePath, dataModel, new HtmlTemplateRenderer())
        {
        }

        // Конструктор для внедрения поддельного шаблонизатора в юнит-тестах
        public PageResult(string templatePath, object dataModel, IHtmlTemplateRenderer renderer)
        {
            _templatePath = templatePath;
            _dataModel = dataModel;
            _renderer = renderer;
        }

        public string RenderContent()
        {
            return _renderer.RenderFromFile(_templatePath, _dataModel);
        }

        public void Execute(HttpListenerContext context)
        {
            string html = RenderContent();
            byte[] buffer = Encoding.UTF8.GetBytes(html);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
    }
}
