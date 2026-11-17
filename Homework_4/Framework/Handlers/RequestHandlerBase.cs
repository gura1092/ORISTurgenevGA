using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MiniHttpServer.Framework
{
    public abstract class RequestHandlerBase
    {
        private RequestHandlerBase _next;

        public RequestHandlerBase SetNext(RequestHandlerBase next)
        {
            _next = next;
            return next;
        }

        public abstract Task HandleAsync(HttpListenerContext context);

        protected Task HandleNextAsync(HttpListenerContext context) =>
            _next is not null ? _next.HandleAsync(context) : Task.CompletedTask;
    }
}
