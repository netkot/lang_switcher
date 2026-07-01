using KeySwitcher.Dictionaries;

namespace KeySwitcher.Core;

/// <summary>
/// Модель символьных биграмм, построенная из словарей. Для незнакомого слова оценивает,
/// насколько его буквосочетания характерны для языка — это отличает осмысленный набор
/// от «мусора», полученного из-за неверной раскладки.
///
/// Модель включается только когда словарь языка достаточно велик (см.
/// <see cref="ReliableDistinctBigrams"/>): на крошечных встроенных списках биграммная
/// статистика неполна, поэтому мы полагаемся на алфавитную эвристику. Как только
/// пользователь подключит полный частотный словарь, модель начинает работать.
/// </summary>
public sealed class NgramModel
{
    // Маркеры начала/конца слова, чтобы учитывать типичные приставки/окончания.
    private const char Start = '^';
    private const char End = '$';

    // Порог «надёжности»: столько различных биграмм должно быть у языка, чтобы доля
    // покрытия стала осмысленной оценкой правдоподобия.
    public const int DefaultReliableDistinctBigrams = 400;

    private readonly Dictionary<Language, HashSet<string>> _bigrams = new();
    private readonly int _reliableThreshold;

    private NgramModel(int reliableThreshold) => _reliableThreshold = reliableThreshold;

    public static NgramModel Build(
        WordDictionary dictionary, int reliableThreshold = DefaultReliableDistinctBigrams)
    {
        var model = new NgramModel(reliableThreshold);
        foreach (Language lang in Enum.GetValues<Language>())
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (string word in dictionary.Words(lang))
                AddBigrams(set, word);
            model._bigrams[lang] = set;
        }
        return model;
    }

    private static void AddBigrams(HashSet<string> set, string word)
    {
        if (word.Length == 0) return;
        string w = Start + word + End;
        for (int i = 0; i + 1 < w.Length; i++)
            set.Add(w.Substring(i, 2));
    }

    /// <summary>Достаточно ли данных у языка, чтобы доверять биграммной оценке.</summary>
    public bool IsReliable(Language lang) =>
        _bigrams.TryGetValue(lang, out var set) && set.Count >= _reliableThreshold;

    /// <summary>
    /// Доля биграмм слова (0..100), встречающихся в языке. Возвращает -1, если модель
    /// для языка ненадёжна и её оценку следует игнорировать.
    /// </summary>
    public double PlausibilityPercent(string letters, Language lang)
    {
        if (!IsReliable(lang) || letters.Length == 0) return -1;
        var set = _bigrams[lang];

        string w = Start + letters.ToLowerInvariant() + End;
        int total = 0, seen = 0;
        for (int i = 0; i + 1 < w.Length; i++)
        {
            total++;
            if (set.Contains(w.Substring(i, 2))) seen++;
        }
        return total == 0 ? -1 : 100.0 * seen / total;
    }
}
