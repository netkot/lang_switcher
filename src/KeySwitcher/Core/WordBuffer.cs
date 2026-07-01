using System.Text;

namespace KeySwitcher.Core;

/// <summary>Активное слово для операции конвертации.</summary>
public readonly record struct ActiveWord(
    string Text,
    Language Language,
    int BackspaceCount,
    string Trailing)
{
    public bool IsEmpty => Text.Length == 0;
}

/// <summary>
/// Отслеживает текущее (набираемое) и последнее завершённое слово по потоку символов
/// из клавиатурного хука. Хранит язык раскладки на момент ввода и хвостовые разделители
/// после последнего слова (чтобы корректно переписать слово после пробела).
/// </summary>
public sealed class WordBuffer
{
    private readonly StringBuilder _current = new();
    private Language _currentLang;

    private string _last = "";
    private Language _lastLang;
    private string _lastTrailing = "";

    public string CurrentWord => _current.ToString();

    /// <summary>Добавляет символ-букву, набранный в раскладке <paramref name="lang"/>.</summary>
    public void FeedLetter(char c, Language lang)
    {
        // Началось новое слово после завершённого — прежнее «последнее» уже не у курсора.
        if (_current.Length == 0 && _lastTrailing.Length > 0)
        {
            _last = "";
            _lastTrailing = "";
        }
        _current.Append(c);
        _currentLang = lang;
    }

    /// <summary>Обрабатывает символ-разделитель (пробел, перевод строки, пунктуация).</summary>
    public void FeedSeparator(char c)
    {
        if (_current.Length > 0)
        {
            _last = _current.ToString();
            _lastLang = _currentLang;
            _current.Clear();
            _lastTrailing = c.ToString();
        }
        else if (_last.Length > 0)
        {
            _lastTrailing += c;
        }
    }

    public void Backspace()
    {
        if (_current.Length > 0)
            _current.Length--;
        else if (_lastTrailing.Length > 0)
            _lastTrailing = _lastTrailing[..^1];
        else if (_last.Length > 0)
            _last = _last[..^1];
    }

    /// <summary>Полный сброс (навигация, Ctrl-комбинации, смена окна).</summary>
    public void ResetHard()
    {
        _current.Clear();
        _last = "";
        _lastTrailing = "";
    }

    /// <summary>
    /// Слово, на которое действует ручная конвертация: набираемое (если есть),
    /// иначе последнее завершённое вместе с хвостовыми разделителями у курсора.
    /// </summary>
    public ActiveWord GetActiveWord()
    {
        if (_current.Length > 0)
            return new ActiveWord(_current.ToString(), _currentLang, _current.Length, "");

        if (_last.Length > 0)
            return new ActiveWord(_last, _lastLang, _last.Length + _lastTrailing.Length, _lastTrailing);

        return new ActiveWord("", _currentLang, 0, "");
    }

    /// <summary>После конвертации активного слова обновляем буфер новым текстом/языком.</summary>
    public void ReplaceActive(string newText, Language newLang)
    {
        if (_current.Length > 0)
        {
            _current.Clear();
            _current.Append(newText);
            _currentLang = newLang;
        }
        else if (_last.Length > 0)
        {
            _last = newText;
            _lastLang = newLang;
        }
    }
}
