using KeySwitcher.Config;
using KeySwitcher.Core;
using KeySwitcher.Dictionaries;
using KeySwitcher.Interop;

namespace KeySwitcher.App;

/// <summary>
/// Контекст фонового приложения: значок в трее, клавиатурный хук и маршрутизация
/// событий клавиш в буфер слов и ручной/авто-режимы.
/// </summary>
public sealed class TrayAppContext : ApplicationContext
{
    private readonly Settings _settings;
    private readonly WordBuffer _buffer = new();
    private readonly LayoutDetector _detector;
    private readonly ManualSwitcher _switcher;
    private readonly LowLevelKeyboardHook _hook = new();

    private readonly NotifyIcon _tray;
    private readonly ToolStripMenuItem _autoModeItem;
    private readonly ToolStripMenuItem _wordHintItem;
    private readonly ToolStripMenuItem _selectionHintItem;

    // Скрытый control — для отложенного выполнения действий после возврата из хука.
    private readonly Control _sync = new();

    public TrayAppContext()
    {
        _settings = Settings.Load();
        WordDictionary dictionary = WordDictionary.LoadEmbedded();
        _detector = new LayoutDetector(dictionary, NgramModel.Build(dictionary));
        _switcher = new ManualSwitcher(_buffer, _detector);
        ApplyEnabledLanguages();

        _ = _sync.Handle; // принудительно создаём хендл для BeginInvoke

        _autoModeItem = new ToolStripMenuItem("Автозамена «на лету»", null, OnToggleAutoMode)
        {
            CheckOnClick = true,
            Checked = _settings.AutoModeEnabled,
        };

        _wordHintItem = new ToolStripMenuItem { Enabled = false };
        _selectionHintItem = new ToolStripMenuItem { Enabled = false };
        RefreshHotkeyHints();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_autoModeItem);
        menu.Items.Add(new ToolStripMenuItem("Отменить последнюю замену", null, OnUndo));
        menu.Items.Add(new ToolStripMenuItem("Настройки…", null, OnOpenSettings));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_wordHintItem);
        menu.Items.Add(_selectionHintItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Выход", null, OnExit));

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "KeySwitcher",
            Visible = true,
            ContextMenuStrip = menu,
        };

        _hook.KeyIntercepted += OnKeyIntercepted;
        _hook.Install();
    }

    private void OnToggleAutoMode(object? sender, EventArgs e)
    {
        _settings.AutoModeEnabled = _autoModeItem.Checked;
        _settings.Save();
    }

    private void OnUndo(object? sender, EventArgs e) => Defer(() => _switcher.UndoLast());

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        using var form = new SettingsForm(_settings);
        form.Saved += () =>
        {
            _autoModeItem.Checked = _settings.AutoModeEnabled;
            ApplyEnabledLanguages();
            RefreshHotkeyHints();
        };
        form.ShowDialog();
    }

    private void RefreshHotkeyHints()
    {
        _wordHintItem.Text = $"{_settings.ConvertWordHotkey.Describe()} — сменить раскладку слова";
        _selectionHintItem.Text =
            $"{_settings.ConvertSelectionHotkey.Describe()} — конвертировать выделение";
    }

    /// <summary>Переносит набор включённых языков из настроек в детектор.</summary>
    private void ApplyEnabledLanguages() =>
        _detector.EnabledLanguages = new HashSet<Language>(
            _settings.EnabledLanguages ?? Enum.GetValues<Language>().ToList());

    private void OnKeyIntercepted(object? sender, KeyboardHookEventArgs e)
    {
        if (e.IsInjected) return;

        // --- Настраиваемые горячие клавиши ---
        if (TryHandleHotkey(e)) return;

        if (!e.IsKeyDown) return;

        // Любой новый ввод (кроме Break/Shift+Break выше) делает прошлую замену
        // неотменяемой: курсор уже не там. Отложенные конвертации ниже зададут undo заново.
        _switcher.ClearUndo();

        int vk = e.VkCode;

        // Backspace.
        if (vk == NativeMethods.VK_BACK)
        {
            _buffer.Backspace();
            return;
        }

        // Модификаторы — не влияют на буфер.
        if (IsModifier(vk)) return;

        // Навигация / редактирование — сбрасываем набранное слово.
        if (IsResetKey(vk))
        {
            _buffer.ResetHard();
            return;
        }

        // Enter/Tab — завершают ввод; трактуем как жёсткий сброс (новая строка/поле).
        if (vk is 0x0D or 0x09)
        {
            _buffer.ResetHard();
            return;
        }

        // Обычный печатный ввод.
        if (!KeyTranslator.TryTranslate(vk, e.ScanCode, out char c))
            return;

        if (char.IsLetter(c))
        {
            Language? lang = LayoutManager.GetForegroundLanguage();
            if (lang is { } l)
                _buffer.FeedLetter(c, l);
            else
                _buffer.ResetHard(); // неизвестная раскладка — не рискуем
            return;
        }

        // Разделитель (пробел, пунктуация, цифра).
        // Авто-замену запускаем только по пробелу: так внутри-токенные разделители
        // (точка/слэш/@/дефис в URL, e-mail, путях, числах) не рвут и не искажают токен,
        // а в поле ввода пароля замена не срабатывает вовсе.
        if (_settings.AutoModeEnabled
            && char.IsWhiteSpace(c)
            && _buffer.CurrentWord.Length > 0
            && !InputContext.IsPasswordFieldFocused())
        {
            e.Suppress = true; // сами вставим разделитель после возможной конвертации
            char sep = c;
            Defer(() => _switcher.HandleAutoSeparator(sep));
        }
        else
        {
            _buffer.FeedSeparator(c);
        }
    }

    /// <summary>
    /// Обрабатывает настраиваемые горячие клавиши. Возвращает true, если событие относится
    /// к клавише-триггеру (и его следует поглотить/не обрабатывать дальше как обычный ввод).
    /// </summary>
    private bool TryHandleHotkey(KeyboardHookEventArgs e)
    {
        int vk = e.VkCode;

        // Отпускание клавиши-триггера тоже подавляем, чтобы она не «просочилась» в приложение.
        if (!e.IsKeyDown)
        {
            if (IsHotkeyTriggerVk(vk)) { e.Suppress = true; return true; }
            return false;
        }

        bool ctrl = e.CtrlDown, shift = e.ShiftDown, alt = e.AltDown;

        if (_settings.ConvertSelectionHotkey.Matches(vk, ctrl, shift, alt))
        {
            e.Suppress = true;
            Defer(() => _switcher.ConvertSelection());
            return true;
        }

        if (_settings.ConvertWordHotkey.Matches(vk, ctrl, shift, alt))
        {
            e.Suppress = true;
            // Punto-стиль: свежую замену повторное нажатие отменяет, иначе — конвертируем.
            Defer(() =>
            {
                if (_switcher.CanUndo) _switcher.UndoLast();
                else _switcher.ConvertLastWord();
            });
            return true;
        }

        if (_settings.UndoHotkey.Matches(vk, ctrl, shift, alt))
        {
            e.Suppress = true;
            Defer(() => _switcher.UndoLast());
            return true;
        }

        // Тот же код, но другие модификаторы: для непечатных триггеров (Pause, F-клавиши…)
        // всё равно поглощаем, чтобы клавиша никогда не попадала в приложение.
        if (IsHotkeyTriggerVk(vk) && IsNonTypingKey(vk))
        {
            e.Suppress = true;
            return true;
        }

        return false;
    }

    private bool IsHotkeyTriggerVk(int vk) =>
        _settings.Hotkeys().Any(h => h.IsSet && h.VkCode == vk);

    // Печатные клавиши: буквы/цифры (0x30–0x5A), numpad (0x60–0x6F), OEM-пунктуация (0xBA–0xE2).
    private static bool IsNonTypingKey(int vk) =>
        !(vk is >= 0x30 and <= 0x5A) &&
        !(vk is >= 0x60 and <= 0x6F) &&
        !(vk is >= 0xBA and <= 0xE2);

    private void Defer(Action action) => _sync.BeginInvoke(action);

    private static bool IsModifier(int vk) => vk is
        0x10 or 0x11 or 0x12 or          // Shift / Ctrl / Alt
        0xA0 or 0xA1 or 0xA2 or 0xA3 or  // L/R Shift, L/R Ctrl
        0xA4 or 0xA5 or                  // L/R Alt
        0x14 or                          // CapsLock
        0x5B or 0x5C or 0x5D;            // L/R Win, Menu

    private static bool IsResetKey(int vk) => vk is
        >= 0x21 and <= 0x28 or // PageUp/Down, End, Home, Left/Up/Right/Down
        0x2D or 0x2E or        // Insert, Delete
        0x1B;                  // Esc

    private void OnExit(object? sender, EventArgs e) => ExitThread();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hook.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _sync.Dispose();
        }
        base.Dispose(disposing);
    }
}
