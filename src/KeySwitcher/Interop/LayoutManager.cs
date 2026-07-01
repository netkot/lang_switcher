using KeySwitcher.Core;
using static KeySwitcher.Interop.NativeMethods;

namespace KeySwitcher.Interop;

/// <summary>Определение и переключение активной раскладки окна переднего плана.</summary>
public static class LayoutManager
{
    /// <summary>Язык раскладки активного окна, если распознан.</summary>
    public static Language? GetForegroundLanguage()
    {
        IntPtr hkl = GetForegroundHkl();
        int langId = LangIdFromHkl(hkl);
        return LanguageFromLangId(langId);
    }

    /// <summary>Переключает раскладку окна переднего плана на указанный язык.</summary>
    public static void SwitchTo(Language language)
    {
        IntPtr hkl = FindInstalledHkl(language) ?? LoadKeyboardLayout(language.DefaultKlid(), KLF_ACTIVATE);
        if (hkl == IntPtr.Zero) return;

        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return;

        PostMessage(hwnd, WM_INPUTLANGCHANGEREQUEST, new IntPtr(INPUTLANGCHANGE_FORWARD), hkl);
    }

    private static IntPtr GetForegroundHkl()
    {
        IntPtr hwnd = GetForegroundWindow();
        uint threadId = GetWindowThreadProcessId(hwnd, out _);
        return GetKeyboardLayout(threadId);
    }

    private static IntPtr? FindInstalledHkl(Language language)
    {
        int count = GetKeyboardLayoutList(0, null);
        if (count <= 0) return null;

        var list = new IntPtr[count];
        GetKeyboardLayoutList(count, list);

        int wanted = language.PrimaryLangId();
        foreach (IntPtr hkl in list)
        {
            if (LangIdFromHkl(hkl) == wanted)
                return hkl;
        }
        return null;
    }

    private static int LangIdFromHkl(IntPtr hkl) => hkl.ToInt32() & 0xFFFF;

    private static Language? LanguageFromLangId(int langId)
    {
        int primary = langId & 0x3FF; // младшие 10 бит — primary language
        return primary switch
        {
            0x09 => Language.English,
            0x19 => Language.Russian,
            0x22 => Language.Ukrainian,
            _ => null,
        };
    }
}
