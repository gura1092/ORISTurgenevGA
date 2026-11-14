namespace ORISFigmaServer
{
    public class Settings
    {
        public ServerConfig Server { get; set; }
    }

    public class ServerConfig
    {
        public string Port { get; set; }
        public string Host { get; set; }
        public string Path { get; set; }
        public string ConnectionString { get; set; }
    }
}
