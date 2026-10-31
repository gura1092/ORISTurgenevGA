using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Samostaytelnay
{
    internal class HttpHandler
    {
        public void SendHtml(HttpListenerResponse response, string html, HttpStatusCode status)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            response.StatusCode = (int)status;
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        public void SendError(HttpListenerResponse response, HttpStatusCode status, string msg)
        {
            string html = $"<html><body><h1>Ошибка {(int)status}</h1><p>{WebUtility.HtmlEncode(msg)}</p></body></html>";
            SendHtml(response, html, status);
        }

        public void Redirect303(HttpListenerResponse response, string location)
        {
            response.StatusCode = (int)HttpStatusCode.SeeOther;
            response.Headers.Add("Location", location);
            response.Close();
        }

        public void Send204NoContent(HttpListenerResponse response)
        {
            response.StatusCode = (int)HttpStatusCode.NoContent;
            response.Close();
        }
    }
}
