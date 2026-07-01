namespace KeySwitcher.Interop;

/// <summary>
/// Утилиты работы с буфером обмена для ручного режима: копирование выделенного текста
/// и его замена с сохранением/восстановлением исходного содержимого буфера.
/// Выполняется в UI-потоке (STA).
/// </summary>
public static class ClipboardHelper
{
    /// <summary>
    /// Копирует выделенный текст (Ctrl+C), возвращает его. Возвращает null, если ничего не выделено.
    /// Восстанавливать буфер обмена вызывающая сторона может через <see cref="Save"/>/<see cref="Restore"/>.
    /// </summary>
    public static string? CopySelection(int settleDelayMs = 60)
    {
        object? backup = Save();
        try
        {
            ClearText();
            InputSender.SendCtrlCombo(NativeMethods.VK_C);
            Thread.Sleep(settleDelayMs); // ждём, пока целевое приложение положит текст в буфер

            string? text = GetText();
            return string.IsNullOrEmpty(text) ? null : text;
        }
        finally
        {
            Restore(backup);
        }
    }

    /// <summary>Кладёт текст в буфер и вставляет его (Ctrl+V), затем восстанавливает исходный буфер.</summary>
    public static void PasteText(string text, object? backupToRestore, int settleDelayMs = 60)
    {
        SetText(text);
        InputSender.SendCtrlCombo(NativeMethods.VK_V);
        Thread.Sleep(settleDelayMs);
        Restore(backupToRestore);
    }

    /// <summary>Сохраняет текущее содержимое буфера обмена (для последующего восстановления).</summary>
    public static object? Save()
    {
        try
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
        catch
        {
            return null;
        }
    }

    public static void Restore(object? backup)
    {
        try
        {
            if (backup is string s && s.Length > 0)
                Clipboard.SetText(s);
            else
                ClearText();
        }
        catch
        {
            // Буфер может быть временно заблокирован другим процессом — игнорируем.
        }
    }

    private static string? GetText()
    {
        try { return Clipboard.ContainsText() ? Clipboard.GetText() : null; }
        catch { return null; }
    }

    private static void SetText(string text)
    {
        try { Clipboard.SetText(text); } catch { /* занят — пропускаем */ }
    }

    private static void ClearText()
    {
        try { Clipboard.Clear(); } catch { /* занят — пропускаем */ }
    }
}
