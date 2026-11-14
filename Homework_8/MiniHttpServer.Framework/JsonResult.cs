using System.Net;
using System.Text;
using System.Text.Json;

namespace MiniHttpServer.Framework.Results
{
    public class JsonResult : IHttpResult
    {
        private readonly object _data;

        public JsonResult(object data) => _data = data;

        public string BuildJson() => JsonSerializer.Serialize(_data);

        public void Execute(HttpListenerContext context)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(BuildJson());
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
    }
}
