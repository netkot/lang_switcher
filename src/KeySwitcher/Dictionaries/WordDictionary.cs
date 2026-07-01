using System.Reflection;
using KeySwitcher.Core;

namespace KeySwitcher.Dictionaries;

/// <summary>
/// Словари слов по языкам, загружаемые из встроенных ресурсов. Используются детектором
/// для проверки валидности слова в конкретном языке. Регистр игнорируется.
/// </summary>
public sealed class WordDictionary
{
    private readonly Dictionary<Language, HashSet<string>> _words = new();

    private static readonly Dictionary<Language, string> ResourceNames = new()
    {
        [Language.English] = "KeySwitcher.Dictionaries.resources.en.txt",
        [Language.Russian] = "KeySwitcher.Dictionaries.resources.ru.txt",
        [Language.Ukrainian] = "KeySwitcher.Dictionaries.resources.uk.txt",
    };

    private WordDictionary() { }

    /// <summary>Загружает встроенные словари из указанной (или текущей) сборки.</summary>
    public static WordDictionary LoadEmbedded(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        var dict = new WordDictionary();

        foreach (var (lang, resource) in ResourceNames)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            using Stream? stream = assembly.GetManifestResourceStream(resource);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    string word = line.Trim();
                    if (word.Length > 0 && !word.StartsWith('#'))
                        set.Add(word.ToLowerInvariant());
                }
            }
            dict._words[lang] = set;
        }
        return dict;
    }

    /// <summary>Создаёт словарь из готовых наборов (для тестов).</summary>
    public static WordDictionary FromSets(IDictionary<Language, IEnumerable<string>> sets)
    {
        var dict = new WordDictionary();
        foreach (Language lang in Enum.GetValues<Language>())
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (sets.TryGetValue(lang, out var words))
                foreach (string w in words)
                    set.Add(w.ToLowerInvariant());
            dict._words[lang] = set;
        }
        return dict;
    }

    public bool Contains(string word, Language language) =>
        _words.TryGetValue(language, out var set) && set.Contains(word.ToLowerInvariant());

    public int Count(Language language) =>
        _words.TryGetValue(language, out var set) ? set.Count : 0;
}
