using Microsoft.Win32;

namespace KeySwitcher.Interop;

/// <summary>
/// Автозапуск при входе в систему через ключ реестра
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>. Источник истины — реестр.
/// </summary>
public static class AutostartManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "KeySwitcher";

    /// <summary>Путь к исполняемому файлу текущего процесса (в кавычках).</summary>
    private static string? ExePath()
    {
        string? path = Environment.ProcessPath;
        return string.IsNullOrEmpty(path) ? null : $"\"{path}\"";
    }

    public static bool IsEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Включает/выключает автозапуск. Возвращает итоговое состояние.</summary>
    public static bool Set(bool enabled)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled)
            {
                string? exe = ExePath();
                if (exe is null) return IsEnabled();
                key.SetValue(ValueName, exe);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Нет доступа к реестру — оставляем как есть.
        }
        return IsEnabled();
    }
}
