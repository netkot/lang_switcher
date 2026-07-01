namespace KeySwitcher.Core;

/// <summary>
/// Эвристики «не трогать этот текст»: URL, e-mail и пути. Такие строки почти никогда
/// не нужно конвертировать между раскладками, поэтому ручная конвертация выделения их
/// пропускает. (В авто-режиме URL/пути защищены иначе: конвертация запускается только
/// по пробелу, а внутри токенов пробелов нет.)
/// </summary>
public static class TextExclusions
{
    /// <summary>true, если строку не следует конвертировать (URL, e-mail, путь).</summary>
    public static bool ShouldSkip(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;
        string t = text.Trim();

        if (t.Contains("://")) return true;                 // http://, ftp:// …
        if (t.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) return true;
        if (LooksLikeEmail(t)) return true;
        if (t.Contains('\\') || t.Contains('/')) return true; // пути / URL-фрагменты

        return false;
    }

    private static bool LooksLikeEmail(string t)
    {
        int at = t.IndexOf('@');
        if (at <= 0 || at >= t.Length - 1) return false;
        // После '@' должна быть точка (домен) и без пробелов во всей строке.
        return t.IndexOf(' ') < 0 && t.IndexOf('.', at) > at;
    }
}
