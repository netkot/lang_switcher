using System.Diagnostics;
using KeySwitcher.Config;
using KeySwitcher.Core;
using KeySwitcher.Dictionaries;
using KeySwitcher.Interop;

namespace KeySwitcher.App;

/// <summary>Окно настроек: авто-режим, автозапуск, языки, горячие клавиши, папка словарей.</summary>
public sealed class SettingsForm : Form
{
    private readonly Settings _settings;
    private readonly CheckBox _autoMode;
    private readonly CheckBox _autostart;
    private readonly Dictionary<Language, CheckBox> _langChecks = new();
    private readonly HotkeyBox _wordHotkey = new();
    private readonly HotkeyBox _selectionHotkey = new();
    private readonly HotkeyBox _undoHotkey = new();

    /// <summary>Вызывается после сохранения — чтобы применить настройки на лету.</summary>
    public event Action? Saved;

    public SettingsForm(Settings settings)
    {
        _settings = settings;

        Text = "KeySwitcher — настройки";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(360, 470);

        _autoMode = new CheckBox
        {
            Text = "Автозамена «на лету» (по пробелу)",
            Checked = _settings.AutoModeEnabled,
            Location = new Point(16, 16),
            AutoSize = true,
        };

        _autostart = new CheckBox
        {
            Text = "Запускать при входе в систему",
            Checked = AutostartManager.IsEnabled(),
            Location = new Point(16, 44),
            AutoSize = true,
        };

        var langGroup = new GroupBox
        {
            Text = "Языки",
            Location = new Point(16, 80),
            Size = new Size(328, 110),
        };
        int y = 24;
        foreach (Language lang in Enum.GetValues<Language>())
        {
            var cb = new CheckBox
            {
                Text = lang.DisplayName(),
                Checked = _settings.IsLanguageEnabled(lang),
                Location = new Point(16, y),
                AutoSize = true,
            };
            _langChecks[lang] = cb;
            langGroup.Controls.Add(cb);
            y += 26;
        }

        var hotkeyGroup = BuildHotkeyGroup();

        var dictButton = new Button
        {
            Text = "Папка словарей…",
            Location = new Point(16, 370),
            Size = new Size(150, 28),
        };
        dictButton.Click += (_, _) => OpenDictionaryFolder();

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(168, 420),
            Size = new Size(84, 30),
        };
        ok.Click += (_, _) => Apply();

        var cancel = new Button
        {
            Text = "Отмена",
            DialogResult = DialogResult.Cancel,
            Location = new Point(260, 420),
            Size = new Size(84, 30),
        };

        Controls.AddRange(new Control[]
            { _autoMode, _autostart, langGroup, hotkeyGroup, dictButton, ok, cancel });
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private GroupBox BuildHotkeyGroup()
    {
        var group = new GroupBox
        {
            Text = "Горячие клавиши (Del — очистить)",
            Location = new Point(16, 200),
            Size = new Size(328, 150),
        };

        AddHotkeyRow(group, "Сменить раскладку слова:", _wordHotkey, _settings.ConvertWordHotkey, 26);
        AddHotkeyRow(group, "Конвертировать выделение:", _selectionHotkey, _settings.ConvertSelectionHotkey, 62);
        AddHotkeyRow(group, "Отменить замену:", _undoHotkey, _settings.UndoHotkey, 98);

        return group;
    }

    private static void AddHotkeyRow(GroupBox group, string caption, HotkeyBox box, Hotkey value, int y)
    {
        group.Controls.Add(new Label
        {
            Text = caption,
            Location = new Point(12, y + 3),
            AutoSize = true,
        });
        box.Value = value;
        box.Location = new Point(180, y);
        box.Size = new Size(136, 24);
        group.Controls.Add(box);
    }

    private void Apply()
    {
        _settings.AutoModeEnabled = _autoMode.Checked;
        _settings.EnabledLanguages = _langChecks
            .Where(kv => kv.Value.Checked)
            .Select(kv => kv.Key)
            .ToList();
        // Полностью пустой список запрещаем — иначе конвертировать некуда.
        if (_settings.EnabledLanguages.Count == 0)
            _settings.EnabledLanguages = Enum.GetValues<Language>().ToList();

        _settings.ConvertWordHotkey = _wordHotkey.Value;
        _settings.ConvertSelectionHotkey = _selectionHotkey.Value;
        _settings.UndoHotkey = _undoHotkey.Value;

        _settings.Save();
        AutostartManager.Set(_autostart.Checked);
        Saved?.Invoke();
    }

    private static void OpenDictionaryFolder()
    {
        try
        {
            Directory.CreateDirectory(WordDictionary.UserDictDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = WordDictionary.UserDictDirectory,
                UseShellExecute = true,
            });
        }
        catch
        {
            // Не критично, если не удалось открыть проводник.
        }
    }
}
