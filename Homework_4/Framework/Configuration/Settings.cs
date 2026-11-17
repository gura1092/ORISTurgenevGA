namespace MiniHttpServer.Framework.Configuration;

internal sealed class Settings
{
    public ServerConfig Server { get; set; } = new();
}

internal sealed class ServerConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 4242;
}
