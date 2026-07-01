namespace KeySwitcher.Core;

/// <summary>
/// Позиционные таблицы раскладок: символы выровнены по физическим клавишам US QWERTY.
/// Индекс i во всех строках соответствует одной и той же физической клавише, поэтому
/// конвертация = взять символ на той же позиции в целевой раскладке.
/// </summary>
public static class LayoutMap
{
    // Порядок физических клавиш (нижний регистр), одинаковый для всех языков:
    //  `  q w e r t y u i o p [ ]  \  a s d f g h j k l ;  '  z x c v b n m , . /
    private const string En = "`qwertyuiop[]\\asdfghjkl;'zxcvbnm,./";
    private const string Ru = "ёйцукенгшщзхъ\\фывапролджэячсмитьбю.";
    private const string Uk = "'йцукенгшщзхїґфівапролджєячсмитьбю.";

    private static readonly Dictionary<Language, string> Tables = new()
    {
        [Language.English] = En,
        [Language.Russian] = Ru,
        [Language.Ukrainian] = Uk,
    };

    // Для каждого языка: символ (нижний регистр) -> индекс физической клавиши.
    private static readonly Dictionary<Language, Dictionary<char, int>> CharToIndex = BuildIndexes();

    private static Dictionary<Language, Dictionary<char, int>> BuildIndexes()
    {
        var result = new Dictionary<Language, Dictionary<char, int>>();
        foreach (var (lang, table) in Tables)
        {
            var map = new Dictionary<char, int>();
            for (int i = 0; i < table.Length; i++)
                map.TryAdd(table[i], i); // первое вхождение выигрывает
            result[lang] = map;
        }
        return result;
    }

    /// <summary>
    /// Конвертирует один символ из раскладки <paramref name="from"/> в раскладку <paramref name="to"/>,
    /// сохраняя регистр. Символы, которых нет в исходной таблице (цифры, пробел и т.п.),
    /// возвращаются без изменений.
    /// </summary>
    public static char Convert(char c, Language from, Language to)
    {
        if (from == to) return c;

        char lower = char.ToLowerInvariant(c);
        bool wasUpper = c != lower && char.ToUpperInvariant(lower) == c;

        if (!CharToIndex[from].TryGetValue(lower, out int index))
            return c; // нет в таблице — не трогаем

        char mapped = Tables[to][index];
        return wasUpper ? char.ToUpperInvariant(mapped) : mapped;
    }

    /// <summary>true, если символ (в любом регистре) присутствует в таблице языка.</summary>
    public static bool Contains(char c, Language lang) =>
        CharToIndex[lang].ContainsKey(char.ToLowerInvariant(c));
}
