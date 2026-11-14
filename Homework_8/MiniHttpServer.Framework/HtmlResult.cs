using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework
{
    public class HtmlResult : IHttpResult
    {
        public string HtmlContent { get; }

        public HtmlResult(string htmlContent) => HtmlContent = htmlContent ?? string.Empty;

        public void Execute(HttpListenerContext context)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(HtmlContent);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
    }
}

