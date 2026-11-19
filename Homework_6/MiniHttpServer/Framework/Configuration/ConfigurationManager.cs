using System.Text.Json;

namespace MiniHttpServer.Framework.Configuration;

internal sealed class ConfigurationManager
{
    private static readonly Lazy<ConfigurationManager> _instance = new(() => new ConfigurationManager());
    public static ConfigurationManager Instance => _instance.Value;
    public ServerSettings Settings { get; }

    private ConfigurationManager()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "settings.json");
        if (!File.Exists(path)) throw new FileNotFoundException("Не найден settings.json", path);
        Settings = JsonSerializer.Deserialize<ServerSettings>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Не удалось прочитать settings.json.");
        if (string.IsNullOrWhiteSpace(Settings.Server.Host)) throw new InvalidDataException("Server.Host не указан.");
        if (Settings.Server.Port is < 1 or > 65535) throw new InvalidDataException("Server.Port должен быть 1–65535.");
        if (Settings.Email.Port is < 1 or > 65535) throw new InvalidDataException("Email.Port должен быть 1–65535.");
    }
}
