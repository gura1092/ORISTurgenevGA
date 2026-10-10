using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORISFigmaServer
{
    internal class Settings
    {
        public ServerConfig Server { get; set; } = new ServerConfig();
    }

    internal class ServerConfig
    {
        public string Port { get; set; }
        public string Host { get; set; }
    }
}

