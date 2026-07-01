using KeySwitcher.App;

namespace KeySwitcher;

internal static class Program
{
    private const string MutexName = "KeySwitcher.SingleInstance.{6D2C9E1F-3A4B-4C7D-9E8F-1A2B3C4D5E6F}";

    [STAThread]
    private static void Main()
    {
        // Единственный экземпляр резидента.
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "KeySwitcher уже запущен (см. значок в области уведомлений).",
                "KeySwitcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext());

        GC.KeepAlive(mutex);
    }
}
