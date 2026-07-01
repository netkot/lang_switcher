using KeySwitcher.Core;

namespace KeySwitcher.App;

/// <summary>
/// Поле захвата горячей клавиши: получив фокус, ждёт нажатия комбинации и запоминает её.
/// Delete/Backspace очищают назначение.
/// </summary>
public sealed class HotkeyBox : TextBox
{
    private Hotkey _value;

    public HotkeyBox()
    {
        ReadOnly = true;
        Cursor = Cursors.Hand;
        BackColor = Color.White;
        ShortcutsEnabled = false;
    }

    public Hotkey Value
    {
        get => _value;
        set { _value = value; Text = value.Describe(); }
    }

    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        Text = "Нажмите клавиши…";
    }

    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        Text = _value.Describe();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Перехватываем даже системные комбинации (Alt+…), пока поле в фокусе.
        if (Focused && TryCapture(keyData))
            return true;
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (TryCapture(e.KeyData))
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }
        base.OnKeyDown(e);
    }

    private bool TryCapture(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;

        // Очистка назначения.
        if (key is Keys.Delete or Keys.Back)
        {
            Value = Hotkey.None;
            return true;
        }

        // Голые модификаторы — ждём основную клавишу.
        if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu
            or Keys.LControlKey or Keys.RControlKey
            or Keys.LShiftKey or Keys.RShiftKey
            or Keys.LMenu or Keys.RMenu or Keys.None)
            return false;

        Value = new Hotkey(
            (int)key,
            Ctrl: (keyData & Keys.Control) != 0,
            Shift: (keyData & Keys.Shift) != 0,
            Alt: (keyData & Keys.Alt) != 0);
        return true;
    }
}
