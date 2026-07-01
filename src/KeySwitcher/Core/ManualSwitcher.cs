using KeySwitcher.Interop;

namespace KeySwitcher.Core;

/// <summary>
/// Ручной режим конвертации:
///   • <see cref="ConvertLastWord"/> — клавиша Break: переписать последнее слово в другой раскладке;
///   • <see cref="ConvertSelection"/> — Shift+Break: конвертировать выделенный текст через буфер обмена.
/// Все методы должны вызываться в UI-потоке после возврата из хука.
/// </summary>
public sealed class ManualSwitcher
{
    private readonly WordBuffer _buffer;
    private readonly LayoutDetector _detector;

    public ManualSwitcher(WordBuffer buffer, LayoutDetector detector)
    {
        _buffer = buffer;
        _detector = detector;
    }

    /// <summary>Break: переписать активное слово в наиболее правдоподобной раскладке.</summary>
    public void ConvertLastWord()
    {
        ActiveWord active = _buffer.GetActiveWord();
        if (active.IsEmpty) return;

        Language target = _detector.PickManualTarget(active.Text, active.Language);
        if (target == active.Language) return;

        string converted = LayoutConverter.Convert(active.Text, active.Language, target);

        InputSender.SendBackspaces(active.BackspaceCount);
        InputSender.SendText(converted + active.Trailing);

        LayoutManager.SwitchTo(target);
        _buffer.ReplaceActive(converted, target);
    }

    /// <summary>
    /// Авто-режим: вызывается на клавише-разделителе. При уверенной детекции конвертирует
    /// набираемое слово, затем в любом случае вставляет разделитель (он был подавлён в хуке).
    /// </summary>
    public void HandleAutoSeparator(char separator)
    {
        ActiveWord active = _buffer.GetActiveWord();
        if (!active.IsEmpty)
        {
            DetectionResult result = _detector.Detect(active.Text, active.Language);
            if (result.ShouldConvert && result.Target != active.Language)
            {
                string converted = LayoutConverter.Convert(active.Text, active.Language, result.Target);
                InputSender.SendBackspaces(active.BackspaceCount);
                InputSender.SendText(converted + separator);
                LayoutManager.SwitchTo(result.Target);
                _buffer.ReplaceActive(converted, result.Target);
                _buffer.FeedSeparator(separator);
                return;
            }
        }

        // Без конвертации: возвращаем подавленный разделитель.
        InputSender.SendText(separator.ToString());
        _buffer.FeedSeparator(separator);
    }

    /// <summary>Shift+Break: конвертировать выделенный текст.</summary>
    public void ConvertSelection()
    {
        object? backup = ClipboardHelper.Save();
        string? selected = ClipboardHelper.CopySelection();
        if (string.IsNullOrEmpty(selected))
        {
            ClipboardHelper.Restore(backup);
            return;
        }

        Language source = GuessLanguage(selected);
        Language target = _detector.PickManualTarget(selected, source);
        if (target == source)
        {
            ClipboardHelper.Restore(backup);
            return;
        }

        string converted = LayoutConverter.Convert(selected, source, target);
        ClipboardHelper.PasteText(converted, backup);
        LayoutManager.SwitchTo(target);
    }

    /// <summary>Грубое определение языка произвольного текста для конвертации выделения.</summary>
    private static Language GuessLanguage(string text)
    {
        bool hasCyrillic = text.Any(c => c is >= 'А' and <= 'я' || c is 'ё' or 'Ё'
            or 'і' or 'І' or 'ї' or 'Ї' or 'є' or 'Є' or 'ґ' or 'Ґ');
        if (!hasCyrillic)
            return Language.English;

        bool ukrainianSignature = text.Any(c =>
            c is 'і' or 'І' or 'ї' or 'Ї' or 'є' or 'Є' or 'ґ' or 'Ґ');
        return ukrainianSignature ? Language.Ukrainian : Language.Russian;
    }
}
