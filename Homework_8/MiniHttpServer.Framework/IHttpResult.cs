using System.Net;

namespace MiniHttpServer.Framework
{
    public interface IHttpResult
    {
        // Записывает сформированный ответ в HttpListenerContext
        void Execute(HttpListenerContext context);
    }
}