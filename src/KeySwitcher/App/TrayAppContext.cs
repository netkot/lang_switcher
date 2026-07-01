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

    // Скрытый control — для отложенного выполнения действий после возврата из хука.
    private readonly Control _sync = new();

    public TrayAppContext()
    {
        _settings = Settings.Load();
        _detector = new LayoutDetector(WordDictionary.LoadEmbedded());
        _switcher = new ManualSwitcher(_buffer, _detector);

        _ = _sync.Handle; // принудительно создаём хендл для BeginInvoke

        _autoModeItem = new ToolStripMenuItem("Автозамена «на лету»", null, OnToggleAutoMode)
        {
            CheckOnClick = true,
            Checked = _settings.AutoModeEnabled,
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_autoModeItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Break — сменить раскладку слова") { Enabled = false });
        menu.Items.Add(new ToolStripMenuItem("Shift+Break — конвертировать выделение") { Enabled = false });
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

    private void OnKeyIntercepted(object? sender, KeyboardHookEventArgs e)
    {
        if (e.IsInjected) return;

        // --- Горячие клавиши Break / Shift+Break ---
        // Клавиша Pause/Break даёт VK_PAUSE (0x13); с Ctrl — VK_CANCEL (0x03).
        if (e.VkCode is NativeMethods.VK_PAUSE or 0x03)
        {
            e.Suppress = true; // не пропускаем Break в приложение
            if (e.IsKeyDown)
            {
                if (e.ShiftDown)
                    Defer(() => _switcher.ConvertSelection());
                else
                    Defer(() => _switcher.ConvertLastWord());
            }
            return;
        }

        if (!e.IsKeyDown) return;

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
        if (_settings.AutoModeEnabled && _buffer.CurrentWord.Length > 0)
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
