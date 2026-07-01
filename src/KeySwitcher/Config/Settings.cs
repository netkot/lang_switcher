using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeySwitcher.Config;

/// <summary>Пользовательские настройки резидента (JSON в %AppData%\KeySwitcher).</summary>
public sealed class Settings
{
    /// <summary>Автоматическая замена «на лету». В MVP по умолчанию выключено.</summary>
    public bool AutoModeEnabled { get; set; }

    [JsonIgnore]
    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KeySwitcher",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
        }
        catch
        {
            // Повреждённый файл — стартуем с настроек по умолчанию.
        }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Нет доступа к диску — молча игнорируем (не критично для работы).
        }
    }
}
