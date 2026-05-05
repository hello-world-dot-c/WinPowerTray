using System;
using System.Threading;
using System.Windows.Forms;

namespace WinPowerTray;

internal static class Program
{
    // Local\\ scope: one instance per user session (correct for a per-user tray app).
    private const string MutexName = "Local\\WinPowerTray_SingleInstance_4B8F-A2D6";

    [STAThread]
    private static void Main()
    {
        // Single-instance guard: if another copy is already running, exit silently.
        using var mutex = new Mutex(initiallyOwned: true, name: MutexName, out bool createdNew);
        if (!createdNew)
            return;

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        try
        {
            Application.Run(new TrayApplicationContext());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"WinPowerTray encountered an unhandled error and must exit.\n\n{ex.Message}",
                "WinPowerTray — Fatal Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
