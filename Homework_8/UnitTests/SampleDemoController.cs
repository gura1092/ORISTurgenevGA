using MiniHttpServer.Framework;
using MiniHttpServer.Framework.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [HttpController]
    public class SampleDemoController : BaseController
    {
        [HttpGet]
        public string Info(int id) => $"Info {id}";

        [HttpPost]
        public string Submit(string name) => $"Saved {name}";
    }
}
