using System.Text.Json;
using System.Text.Json.Serialization;
using KeySwitcher.Core;

namespace KeySwitcher.Config;

/// <summary>Пользовательские настройки резидента (JSON в %AppData%\KeySwitcher).</summary>
public sealed class Settings
{
    /// <summary>Автоматическая замена «на лету». В MVP по умолчанию выключено.</summary>
    public bool AutoModeEnabled { get; set; }

    /// <summary>
    /// Языки, участвующие в детекции и конвертации. Позволяет отключить неиспользуемый
    /// язык (напр. украинский), чтобы он не мешал распознаванию, — и служит точкой
    /// расширения при добавлении новых языков. По умолчанию — все поддерживаемые.
    /// </summary>
    public List<Language> EnabledLanguages { get; set; } =
        Enum.GetValues<Language>().ToList();

    /// <summary>Включён ли язык (при пустом списке считаем включёнными все).</summary>
    public bool IsLanguageEnabled(Language lang) =>
        EnabledLanguages is null || EnabledLanguages.Count == 0 || EnabledLanguages.Contains(lang);

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
