using MiniHttpServer.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    public class ControllerMockHandler : RequestHandlerBase
    {
        public override void Handle(RequestContextMock context)
        {
            if (context.Path.StartsWith("/api"))
            {
                context.IsHandled = true;
                context.ResponseBody = "ControllerResponse";
                return;
            }
            base.Handle(context);
        }
    }
}
