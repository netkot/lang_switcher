using System.Text;

namespace KeySwitcher.Core;

/// <summary>Конвертация слов/текста между раскладками на базе <see cref="LayoutMap"/>.</summary>
public static class LayoutConverter
{
    /// <summary>Конвертирует строку из раскладки <paramref name="from"/> в <paramref name="to"/>.</summary>
    public static string Convert(string text, Language from, Language to)
    {
        if (from == to || string.IsNullOrEmpty(text)) return text;

        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
            sb.Append(LayoutMap.Convert(c, from, to));
        return sb.ToString();
    }

    /// <summary>Все возможные варианты конвертации в другие языки (кроме исходного).</summary>
    public static IEnumerable<(Language language, string text)> ConvertToOthers(string text, Language from)
    {
        foreach (Language to in Enum.GetValues<Language>())
        {
            if (to == from) continue;
            yield return (to, Convert(text, from, to));
        }
    }
}
