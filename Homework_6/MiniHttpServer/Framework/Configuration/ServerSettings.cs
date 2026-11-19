namespace MiniHttpServer.Framework.Configuration;

internal sealed class ServerSettings
{
    public ServerConfig Server { get; set; } = new();
    public EmailConfig Email { get; set; } = new();
}

internal sealed class ServerConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 4242;
}

internal sealed class EmailConfig
{
    public bool UseSmtp { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool EnableSsl { get; set; }
    public string From { get; set; } = "demo@example.local";
    public string To { get; set; } = "student@example.local";
}
