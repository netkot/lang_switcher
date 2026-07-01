using KeySwitcher.Dictionaries;

namespace KeySwitcher.Core;

public enum DetectionConfidence
{
    None,       // не конвертировать
    Heuristic,  // конвертировать по эвристике (слова нет в словарях)
    Dictionary, // конвертировать: результат — словарное слово
}

public readonly record struct DetectionResult(
    bool ShouldConvert,
    Language Target,
    DetectionConfidence Confidence)
{
    public static readonly DetectionResult NoChange = new(false, default, DetectionConfidence.None);
}

/// <summary>
/// Гибридный детектор: сначала проверяет слово по словарям, при отсутствии совпадений
/// использует эвристику плавучести (алфавит, наличие гласных, буквы-сигнатуры языка).
/// </summary>
public sealed class LayoutDetector
{
    private readonly WordDictionary _dictionary;
    private readonly NgramModel? _ngram;

    // Порог превосходства эвристики для авто-конвертации незнакомых слов.
    private const double HeuristicMargin = 25.0;

    private static readonly Dictionary<Language, string> Alphabets = new()
    {
        [Language.English] = "abcdefghijklmnopqrstuvwxyz",
        [Language.Russian] = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя",
        [Language.Ukrainian] = "абвгґдеєжзиіїйклмнопрстуфхцчшщьюя",
    };

    private static readonly Dictionary<Language, string> Vowels = new()
    {
        [Language.English] = "aeiou",
        [Language.Russian] = "аеёиоуыэюя",
        [Language.Ukrainian] = "аеєиіїоуюя",
    };

    // Буквы, характерные только для конкретного языка (сигнатура).
    private static readonly Dictionary<Language, string> Signature = new()
    {
        [Language.English] = "",
        [Language.Russian] = "ыъэё",
        [Language.Ukrainian] = "іїєґ",
    };

    public LayoutDetector(WordDictionary dictionary, NgramModel? ngram = null)
    {
        _dictionary = dictionary;
        _ngram = ngram;
    }

    /// <summary>
    /// Языки, доступные как цель конвертации. Меняется из настроек на лету.
    /// Пустой набор трактуется как «все языки».
    /// </summary>
    public ISet<Language> EnabledLanguages { get; set; } =
        new HashSet<Language>(Enum.GetValues<Language>());

    private bool IsEnabled(Language lang) =>
        EnabledLanguages.Count == 0 || EnabledLanguages.Contains(lang);

    /// <summary>Решение для авто-режима: конвертировать ли и на какой язык.</summary>
    public DetectionResult Detect(string word, Language current)
    {
        string core = ExtractLetters(word);
        if (core.Length < 2) return DetectionResult.NoChange;

        // 1. Слово валидно в текущем языке — не трогаем.
        if (_dictionary.Contains(core, current))
            return DetectionResult.NoChange;

        // 2. Ищем словарное совпадение среди конвертаций.
        var ranked = RankCandidates(core, current);
        if (ranked.Count == 0) return DetectionResult.NoChange; // все прочие языки отключены
        var top = ranked[0];

        if (top.dictHit)
            return new DetectionResult(true, top.language, DetectionConfidence.Dictionary);

        // 3. Эвристика: конвертируем, только если альтернатива заметно правдоподобнее.
        double currentScore = Plausibility(core, current);
        if (top.score > currentScore + HeuristicMargin)
            return new DetectionResult(true, top.language, DetectionConfidence.Heuristic);

        return DetectionResult.NoChange;
    }

    /// <summary>
    /// Выбор цели для ручного режима: пользователь явно просит конвертацию, поэтому
    /// возвращаем наиболее правдоподобный язык, даже без словарного совпадения.
    /// </summary>
    public Language PickManualTarget(string word, Language current)
    {
        string core = ExtractLetters(word);
        var ranked = RankCandidates(core.Length > 0 ? core : word, current);
        return ranked.Count > 0 ? ranked[0].language : current;
    }

    /// <summary>Кандидаты-конвертации, отсортированные по убыванию правдоподобия.</summary>
    public List<(Language language, string text, bool dictHit, double score)> RankCandidates(
        string word, Language current)
    {
        var list = new List<(Language language, string text, bool dictHit, double score)>();
        foreach (var (lang, text) in LayoutConverter.ConvertToOthers(word, current))
        {
            if (!IsEnabled(lang)) continue; // отключённый язык не предлагаем
            bool dictHit = _dictionary.Contains(ExtractLetters(text), lang);
            double score = dictHit ? 1000.0 : Plausibility(text, lang);
            list.Add((lang, text, dictHit, score));
        }
        // По убыванию score; при равенстве — стабильный порядок по enum.
        list.Sort((a, b) =>
        {
            int cmp = b.score.CompareTo(a.score);
            return cmp != 0 ? cmp : ((int)a.language).CompareTo((int)b.language);
        });
        return list;
    }

    /// <summary>Оценка правдоподобия слова в языке (0..~115).</summary>
    private double Plausibility(string word, Language lang)
    {
        string letters = ExtractLetters(word).ToLowerInvariant();
        if (letters.Length == 0) return 0;

        string alphabet = Alphabets[lang];
        string vowels = Vowels[lang];

        int inAlphabet = letters.Count(alphabet.Contains);
        double alphabetScore = 100.0 * inAlphabet / letters.Length;

        // Биграммная модель (если словарь языка достаточно велик) уточняет оценку:
        // осмысленные буквосочетания повышают правдоподобие, «мусорные» — понижают.
        double score = alphabetScore;
        double ngram = _ngram?.PlausibilityPercent(letters, lang) ?? -1;
        if (ngram >= 0)
            score = 0.5 * alphabetScore + 0.5 * ngram;

        bool hasVowel = letters.Any(vowels.Contains);
        if (!hasVowel && letters.Length >= 3)
            score -= 40.0;

        if (letters.Any(Signature[lang].Contains))
            score += 15.0;

        return score;
    }

    private static string ExtractLetters(string s) =>
        new(s.Where(char.IsLetter).ToArray());
}
