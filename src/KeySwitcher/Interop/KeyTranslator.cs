using System.Text;
using static KeySwitcher.Interop.NativeMethods;

namespace KeySwitcher.Interop;

/// <summary>
/// Преобразует виртуальную клавишу в символ, который она произвела бы в раскладке
/// окна переднего плана, с учётом Shift и CapsLock.
/// </summary>
public static class KeyTranslator
{
    /// <summary>
    /// Пытается получить печатный символ для клавиши. Возвращает false для непечатных
    /// (Ctrl-комбинации, функциональные и навигационные клавиши).
    /// </summary>
    public static bool TryTranslate(int vkCode, int scanCode, out char result)
    {
        result = '\0';

        // Если зажат Ctrl или Alt (кроме AltGr) — это не обычный ввод текста.
        bool ctrl = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
        if (ctrl) return false;

        IntPtr hkl = GetForegroundHkl();

        var keyState = new byte[256];
        if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) keyState[VK_SHIFT] = 0x80;
        if ((GetKeyState(VK_CAPITAL) & 0x0001) != 0) keyState[VK_CAPITAL] = 0x01;

        var sb = new StringBuilder(8);
        int rc = ToUnicodeEx((uint)vkCode, (uint)scanCode, keyState, sb, sb.Capacity, 0, hkl);

        // rc > 0 — получены символы; rc < 0 — «мёртвая» клавиша.
        if (rc <= 0 || sb.Length == 0) return false;

        result = sb[0];
        return !char.IsControl(result);
    }

    private static IntPtr GetForegroundHkl()
    {
        IntPtr hwnd = GetForegroundWindow();
        uint threadId = GetWindowThreadProcessId(hwnd, out _);
        return GetKeyboardLayout(threadId);
    }
}
