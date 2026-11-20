namespace MiniHttpServer.Framework.Configuration;
public sealed class ServerSettings
{
    public ServerConfig Server { get; set; } = new();
    public EmailConfig Email { get; set; } = new();
    public bool UseDatabase { get; set; } = false;
    public string ConnectionString { get; set; } = string.Empty;
}
public sealed class ServerConfig { public string Host { get; set; } = "127.0.0.1"; public int Port { get; set; } = 4242; }
public sealed class EmailConfig { public bool UseSmtp { get; set; } public string Host { get; set; } = "localhost"; public int Port { get; set; } = 1025; public bool EnableSsl { get; set; } public string From { get; set; } = "demo@example.local"; public string To { get; set; } = "student@example.local"; }
