using System.Text.Json;

namespace MiniHttpServer.Framework.Configuration;

internal sealed class ConfigurationManager
{
    private static readonly Lazy<ConfigurationManager> _instance =
        new(() => new ConfigurationManager());

    public static ConfigurationManager Instance => _instance.Value;
    public Settings Settings { get; }

    private ConfigurationManager()
    {
        string settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
        if (!File.Exists(settingsPath))
            throw new FileNotFoundException("Не найден settings.json", settingsPath);

        string json = File.ReadAllText(settingsPath);
        Settings = JsonSerializer.Deserialize<Settings>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Не удалось прочитать settings.json");

        if (string.IsNullOrWhiteSpace(Settings.Server.Host))
            throw new InvalidDataException("В settings.json не указан Server.Host.");

        if (Settings.Server.Port is < 1 or > 65535)
            throw new InvalidDataException("Порт должен быть в диапазоне 1–65535.");

        if (Settings.Email.Port is < 1 or > 65535)
            throw new InvalidDataException("Email.Port должен быть в диапазоне 1–65535.");
    }
}
