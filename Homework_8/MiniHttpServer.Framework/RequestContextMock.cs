using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework
{
    public class RequestContextMock
    {
        public string Path { get; set; }
        public bool IsHandled { get; set; }
        public string ResponseBody { get; set; }
    }
}
