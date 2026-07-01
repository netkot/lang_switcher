using System.Runtime.InteropServices;
using static KeySwitcher.Interop.NativeMethods;

namespace KeySwitcher.Interop;

/// <summary>Аргументы перехваченного события клавиатуры.</summary>
public sealed class KeyboardHookEventArgs : EventArgs
{
    public required int VkCode { get; init; }
    public required int ScanCode { get; init; }
    public required bool IsKeyDown { get; init; }
    public required bool IsInjected { get; init; }
    public required bool ShiftDown { get; init; }
    public required bool CtrlDown { get; init; }
    public required bool AltDown { get; init; }

    /// <summary>Установите true, чтобы «съесть» клавишу (не передавать дальше в систему).</summary>
    public bool Suppress { get; set; }
}

/// <summary>
/// Глобальный низкоуровневый клавиатурный хук (WH_KEYBOARD_LL).
/// Устанавливается из UI-потока с активным циклом сообщений.
/// </summary>
public sealed class LowLevelKeyboardHook : IDisposable
{
    // Держим делегат в поле, чтобы GC не собрал его, пока хук активен.
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookHandle = IntPtr.Zero;

    public event EventHandler<KeyboardHookEventArgs>? KeyIntercepted;

    public LowLevelKeyboardHook() => _proc = HookCallback;

    public bool IsInstalled => _hookHandle != IntPtr.Zero;

    public void Install()
    {
        if (IsInstalled) return;

        IntPtr hModule = GetModuleHandle(null);
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hModule, 0);
        if (_hookHandle == IntPtr.Zero)
            throw new InvalidOperationException(
                $"Не удалось установить клавиатурный хук (код ошибки {Marshal.GetLastWin32Error()}).");
    }

    public void Uninstall()
    {
        if (!IsInstalled) return;
        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode != HC_ACTION)
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        int msg = wParam.ToInt32();
        bool isKeyDown = msg is WM_KEYDOWN or WM_SYSKEYDOWN;
        bool isKeyUp = msg is WM_KEYUP or WM_SYSKEYUP;

        // Игнорируем чужие сообщения и собственный синтетический ввод.
        bool injected = (data.flags & LLKHF_INJECTED) != 0 || data.dwExtraInfo == InjectedMarker;

        if ((isKeyDown || isKeyUp) && KeyIntercepted is { } handler)
        {
            var args = new KeyboardHookEventArgs
            {
                VkCode = (int)data.vkCode,
                ScanCode = (int)data.scanCode,
                IsKeyDown = isKeyDown,
                IsInjected = injected,
                ShiftDown = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0,
                CtrlDown = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0,
                AltDown = (GetAsyncKeyState(0x12) & 0x8000) != 0, // VK_MENU
            };

            handler(this, args);

            if (args.Suppress)
                return 1; // подавляем клавишу
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
