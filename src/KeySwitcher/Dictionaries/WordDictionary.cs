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

    /// <summary>
    /// Каталог пользовательских словарей: <c>%AppData%\KeySwitcher\dict</c>.
    /// Пользователь может положить сюда полные частотные списки <c>{code}.txt</c>
    /// (по одному слову на строку) — они сливаются со встроенными без пересборки.
    /// </summary>
    public static string UserDictDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KeySwitcher",
        "dict");

    /// <summary>Путь к пользовательскому словарю конкретного языка.</summary>
    public static string UserDictPath(Language lang) =>
        Path.Combine(UserDictDirectory, $"{lang.Code()}.txt");

    /// <summary>
    /// Загружает встроенные словари из сборки и, если они есть, сливает с ними
    /// пользовательские словари с диска (<see cref="UserDictDirectory"/>).
    /// </summary>
    public static WordDictionary LoadEmbedded(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        var dict = new WordDictionary();

        foreach (var (lang, resource) in ResourceNames)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            using (Stream? stream = assembly.GetManifestResourceStream(resource))
                AddLines(set, stream);

            // Пользовательский словарь (полный частотный список и/или добавленные слова).
            try
            {
                string userPath = UserDictPath(lang);
                if (File.Exists(userPath))
                    using (Stream fs = File.OpenRead(userPath))
                        AddLines(set, fs);
            }
            catch
            {
                // Недоступен пользовательский файл — работаем на встроенном словаре.
            }

            dict._words[lang] = set;
        }
        return dict;
    }

    private static void AddLines(HashSet<string> set, Stream? stream)
    {
        if (stream is null) return;
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string word = line.Trim();
            // Частотные списки Hermit Dave имеют формат "слово частота" — берём первый токен.
            int sp = word.IndexOf(' ');
            if (sp > 0) word = word[..sp];
            if (word.Length > 0 && !word.StartsWith('#'))
                set.Add(word.ToLowerInvariant());
        }
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

    /// <summary>Все слова языка (для построения n-gram модели).</summary>
    public IReadOnlyCollection<string> Words(Language language) =>
        _words.TryGetValue(language, out var set) ? set : Array.Empty<string>();

    /// <summary>
    /// Добавляет слово в словарь языка в памяти и дописывает его в пользовательский
    /// файл на диске, чтобы оно сохранилось между запусками. Возвращает true, если слово
    /// было новым.
    /// </summary>
    public bool AddUserWord(string word, Language language)
    {
        string normalized = word.Trim().ToLowerInvariant();
        if (normalized.Length == 0) return false;

        if (!_words.TryGetValue(language, out var set))
            _words[language] = set = new HashSet<string>(StringComparer.Ordinal);
        if (!set.Add(normalized)) return false;

        try
        {
            Directory.CreateDirectory(UserDictDirectory);
            File.AppendAllText(UserDictPath(language), normalized + Environment.NewLine);
        }
        catch
        {
            // Не удалось сохранить на диск — слово всё равно активно до перезапуска.
        }
        return true;
    }
}
