using System.Runtime.InteropServices;
using static KeySwitcher.Interop.NativeMethods;

namespace KeySwitcher.Interop;

/// <summary>
/// Отправка синтетического ввода через SendInput. Символы вводятся как Unicode
/// (независимо от активной раскладки); удаление — клавишей Backspace.
/// Весь ввод помечается <see cref="NativeMethods.InjectedMarker"/>, чтобы хук его игнорировал.
/// </summary>
public static class InputSender
{
    /// <summary>Нажимает Backspace указанное число раз.</summary>
    public static void SendBackspaces(int count)
    {
        if (count <= 0) return;

        var inputs = new INPUT[count * 2];
        for (int i = 0; i < count; i++)
        {
            inputs[i * 2] = KeyDownVk(VK_BACK);
            inputs[i * 2 + 1] = KeyUpVk(VK_BACK);
        }
        Send(inputs);
    }

    /// <summary>Вводит строку посимвольно как Unicode.</summary>
    public static void SendText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var inputs = new List<INPUT>(text.Length * 2);
        foreach (char c in text)
        {
            inputs.Add(UnicodeDown(c));
            inputs.Add(UnicodeUp(c));
        }
        Send(inputs.ToArray());
    }

    /// <summary>Посылает комбинацию Ctrl+&lt;vk&gt; (например Ctrl+C, Ctrl+V).</summary>
    public static void SendCtrlCombo(ushort vk)
    {
        var inputs = new[]
        {
            KeyDownVk(VK_CONTROL),
            KeyDownVk(vk),
            KeyUpVk(vk),
            KeyUpVk(VK_CONTROL),
        };
        Send(inputs);
    }

    private static void Send(INPUT[] inputs)
    {
        uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent != inputs.Length)
            throw new InvalidOperationException(
                $"SendInput отправил {sent} из {inputs.Length} событий (ошибка {Marshal.GetLastWin32Error()}).");
    }

    private static INPUT KeyDownVk(ushort vk) => MakeVk(vk, 0);
    private static INPUT KeyUpVk(ushort vk) => MakeVk(vk, KEYEVENTF_KEYUP);

    private static INPUT MakeVk(ushort vk, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                wScan = 0,
                dwFlags = flags,
                time = 0,
                dwExtraInfo = InjectedMarker,
            },
        },
    };

    private static INPUT UnicodeDown(char c) => MakeUnicode(c, KEYEVENTF_UNICODE);
    private static INPUT UnicodeUp(char c) => MakeUnicode(c, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP);

    private static INPUT MakeUnicode(char c, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = 0,
                wScan = c,
                dwFlags = flags,
                time = 0,
                dwExtraInfo = InjectedMarker,
            },
        },
    };
}
