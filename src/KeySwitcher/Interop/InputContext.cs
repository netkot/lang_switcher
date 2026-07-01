using System.Text;
using System.Windows.Automation;
using static KeySwitcher.Interop.NativeMethods;

namespace KeySwitcher.Interop;

/// <summary>Сведения о поле ввода активного окна (для исключений авто-замены).</summary>
public static class InputContext
{
    /// <summary>
    /// true, если фокус ввода находится в поле пароля. Сначала быстрый Win32-путь
    /// (Edit со стилем ES_PASSWORD — диалоги входа), затем UI Automation
    /// (IsPassword) — покрывает UWP-приложения и поля в браузерах.
    /// </summary>
    public static bool IsPasswordFieldFocused() =>
        IsWin32PasswordField() || IsUiaPasswordField();

    private static bool IsWin32PasswordField()
    {
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;

            uint threadId = GetWindowThreadProcessId(hwnd, out _);
            var gui = new GUITHREADINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<GUITHREADINFO>() };
            if (!GetGUIThreadInfo(threadId, ref gui) || gui.hwndFocus == IntPtr.Zero)
                return false;

            var cls = new StringBuilder(32);
            GetClassName(gui.hwndFocus, cls, cls.Capacity);
            string className = cls.ToString();
            if (className is not ("Edit" or "RichEdit" or "RichEdit20W" or "RichEdit20A"))
                return false;

            long style = GetWindowLongPtr(gui.hwndFocus, GWL_STYLE).ToInt64();
            return (style & ES_PASSWORD) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// UI Automation: элемент в фокусе помечен как поле пароля (IsPassword). Кросс-процессный
    /// вызов, поэтому используется только на пути авто-замены (редко). Любая ошибка → false.
    /// </summary>
    private static bool IsUiaPasswordField()
    {
        try
        {
            AutomationElement? focused = AutomationElement.FocusedElement;
            if (focused is null) return false;
            return focused.GetCurrentPropertyValue(AutomationElement.IsPasswordProperty) is true;
        }
        catch
        {
            return false;
        }
    }
}
