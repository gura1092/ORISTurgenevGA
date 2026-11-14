using MiniHttpServer.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    public class StaticFileMockHandler : RequestHandlerBase
    {
        public override void Handle(RequestContextMock context)
        {
            context.IsHandled = true;
            context.ResponseBody = "StaticFileResponse";
        }
    }
}
