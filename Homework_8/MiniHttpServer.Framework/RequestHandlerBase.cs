using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public virtual void Handle(RequestContextMock context)
        {
            if (!context.IsHandled && _next != null)
                _next.Handle(context);
        }
    }
}
