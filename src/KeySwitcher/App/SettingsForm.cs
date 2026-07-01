using System.Diagnostics;
using KeySwitcher.Config;
using KeySwitcher.Core;
using KeySwitcher.Dictionaries;
using KeySwitcher.Interop;

namespace KeySwitcher.App;

/// <summary>Окно настроек: авто-режим, автозапуск, набор языков, папка словарей.</summary>
public sealed class SettingsForm : Form
{
    private readonly Settings _settings;
    private readonly CheckBox _autoMode;
    private readonly CheckBox _autostart;
    private readonly Dictionary<Language, CheckBox> _langChecks = new();

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
        ClientSize = new Size(340, 300);

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
            Size = new Size(308, 110),
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

        var dictButton = new Button
        {
            Text = "Папка словарей…",
            Location = new Point(16, 200),
            Size = new Size(140, 28),
            AutoSize = false,
        };
        dictButton.Click += (_, _) => OpenDictionaryFolder();

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(148, 250),
            Size = new Size(84, 30),
        };
        ok.Click += (_, _) => Apply();

        var cancel = new Button
        {
            Text = "Отмена",
            DialogResult = DialogResult.Cancel,
            Location = new Point(240, 250),
            Size = new Size(84, 30),
        };

        Controls.AddRange(new Control[] { _autoMode, _autostart, langGroup, dictButton, ok, cancel });
        AcceptButton = ok;
        CancelButton = cancel;
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
