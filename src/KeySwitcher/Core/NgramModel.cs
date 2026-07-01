using KeySwitcher.Dictionaries;

namespace KeySwitcher.Core;

/// <summary>
/// Символьная n-gram модель (триграммы с откатом на биграммы), построенная из словарей.
/// Для незнакомого слова оценивает, насколько его буквосочетания характерны для языка —
/// это отличает осмысленный набор от «мусора», полученного из-за неверной раскладки.
///
/// Каждая триграмма слова даёт «кредит»: 1.0, если такая тройка встречалась в языке;
/// 0.4 по откату, если встречалась хотя бы её замыкающая пара (биграмма); иначе 0.
/// Итог — средний кредит в процентах (0..100). Триграммы точнее биграмм, а откат не даёт
/// коротким/редким словам обнуляться.
///
/// Модель включается только когда словарь языка достаточно велик (см.
/// <see cref="DefaultReliableDistinctBigrams"/>): на крошечных встроенных списках
/// статистика неполна. Как только подключён полный частотный словарь — модель работает.
/// </summary>
public sealed class NgramModel
{
    private const char Start = '^';
    private const char End = '$';
    private const double BackoffCredit = 0.4;

    // Порог «надёжности»: столько различных биграмм должно быть у языка, чтобы оценка
    // покрытия стала осмысленной.
    public const int DefaultReliableDistinctBigrams = 400;

    private readonly Dictionary<Language, HashSet<string>> _bigrams = new();
    private readonly Dictionary<Language, HashSet<string>> _trigrams = new();
    private readonly int _reliableThreshold;

    private NgramModel(int reliableThreshold) => _reliableThreshold = reliableThreshold;

    public static NgramModel Build(
        WordDictionary dictionary, int reliableThreshold = DefaultReliableDistinctBigrams)
    {
        var model = new NgramModel(reliableThreshold);
        foreach (Language lang in Enum.GetValues<Language>())
        {
            var bi = new HashSet<string>(StringComparer.Ordinal);
            var tri = new HashSet<string>(StringComparer.Ordinal);
            foreach (string word in dictionary.Words(lang))
            {
                AddGrams(bi, PadBigram(word), 2);
                AddGrams(tri, PadTrigram(word), 3);
            }
            model._bigrams[lang] = bi;
            model._trigrams[lang] = tri;
        }
        return model;
    }

    // Внимание: Start/End — char, поэтому строим строку через интерполяцию, иначе
    // Start + Start сложились бы как числа (char + char = int).
    private static string PadBigram(string word) => $"{Start}{word}{End}";
    private static string PadTrigram(string word) => $"{Start}{Start}{word}{End}";

    private static void AddGrams(HashSet<string> set, string padded, int n)
    {
        for (int i = 0; i + n <= padded.Length; i++)
            set.Add(padded.Substring(i, n));
    }

    /// <summary>Достаточно ли данных у языка, чтобы доверять оценке.</summary>
    public bool IsReliable(Language lang) =>
        _bigrams.TryGetValue(lang, out var set) && set.Count >= _reliableThreshold;

    /// <summary>
    /// Правдоподобие слова в языке (0..100) по триграммам с откатом на биграммы.
    /// Возвращает -1, если модель для языка ненадёжна и её оценку следует игнорировать.
    /// </summary>
    public double PlausibilityPercent(string letters, Language lang)
    {
        if (!IsReliable(lang) || letters.Length == 0) return -1;
        var tri = _trigrams[lang];
        var bi = _bigrams[lang];

        string w = PadTrigram(letters.ToLowerInvariant());
        int count = 0;
        double credit = 0;
        for (int i = 0; i + 3 <= w.Length; i++)
        {
            count++;
            string t = w.Substring(i, 3);
            if (tri.Contains(t)) credit += 1.0;
            else if (bi.Contains(t.Substring(1, 2))) credit += BackoffCredit;
        }
        return count == 0 ? -1 : 100.0 * credit / count;
    }
}
