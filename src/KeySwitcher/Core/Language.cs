namespace KeySwitcher.Core;

/// <summary>Поддерживаемые языки/раскладки.</summary>
public enum Language
{
    English,
    Russian,
    Ukrainian,
}

public static class LanguageInfo
{
    /// <summary>Primary LANGID (младшее слово LANGID) для каждого языка.</summary>
    public static int PrimaryLangId(this Language lang) => lang switch
    {
        Language.English => 0x09,   // LANG_ENGLISH
        Language.Russian => 0x19,   // LANG_RUSSIAN
        Language.Ukrainian => 0x22, // LANG_UKRAINIAN
        _ => 0x09,
    };

    /// <summary>KLID (идентификатор раскладки) по умолчанию — на случай, если раскладка не установлена.</summary>
    public static string DefaultKlid(this Language lang) => lang switch
    {
        Language.English => "00000409",   // US
        Language.Russian => "00000419",   // Russian
        Language.Ukrainian => "00000422", // Ukrainian (Enhanced использует 00020422; базовый 00000422)
        _ => "00000409",
    };

    public static string DisplayName(this Language lang) => lang switch
    {
        Language.English => "English",
        Language.Russian => "Русский",
        Language.Ukrainian => "Українська",
        _ => lang.ToString(),
    };

    /// <summary>Короткий код языка (ISO-639-1-подобный), используется в именах файлов словарей.</summary>
    public static string Code(this Language lang) => lang switch
    {
        Language.English => "en",
        Language.Russian => "ru",
        Language.Ukrainian => "uk",
        _ => "en",
    };
}
