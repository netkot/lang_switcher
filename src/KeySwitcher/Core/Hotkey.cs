namespace KeySwitcher.Core;

/// <summary>
/// Горячая клавиша: виртуальный код + набор модификаторов. Пустой (<see cref="None"/>)
/// означает «не назначено». Сравнение модификаторов точное, поэтому Break и Shift+Break
/// не пересекаются.
/// </summary>
public readonly record struct Hotkey(int VkCode, bool Ctrl, bool Shift, bool Alt)
{
    public static readonly Hotkey None = new(0, false, false, false);

    public bool IsSet => VkCode != 0;

    /// <summary>Совпадает ли клавиша с нажатой (код и точный набор модификаторов).</summary>
    public bool Matches(int vk, bool ctrl, bool shift, bool alt) =>
        IsSet && vk == VkCode && ctrl == Ctrl && shift == Shift && alt == Alt;

    /// <summary>Человекочитаемое описание, напр. «Shift+Pause».</summary>
    public string Describe()
    {
        if (!IsSet) return "—";
        var parts = new List<string>(4);
        if (Ctrl) parts.Add("Ctrl");
        if (Shift) parts.Add("Shift");
        if (Alt) parts.Add("Alt");
        parts.Add(((Keys)VkCode).ToString());
        return string.Join("+", parts);
    }
}
