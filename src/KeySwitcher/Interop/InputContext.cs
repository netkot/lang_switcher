using System.Text;
using static KeySwitcher.Interop.NativeMethods;

namespace KeySwitcher.Interop;

/// <summary>Сведения о поле ввода активного окна (для исключений авто-замены).</summary>
public static class InputContext
{
    /// <summary>
    /// true, если фокус ввода находится в классическом поле пароля (Edit со стилем
    /// ES_PASSWORD). Best-effort: работает для Win32-полей (диалоги входа и т.п.);
    /// для UWP/браузерных полей определить стиль нельзя — там вернёт false.
    /// </summary>
    public static bool IsPasswordFieldFocused()
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
}
