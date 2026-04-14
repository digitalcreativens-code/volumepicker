using System.Diagnostics;

namespace VolumeGuard;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Any(a => a.Equals("--watchdog", StringComparison.OrdinalIgnoreCase)))
        {
            Watchdog.Run();
            return;
        }

        var mutex = new Mutex(true, Constants.MainMutex, out var created);
        if (!created)
        {
            MessageBox.Show("VolumeGuard je već pokrenut.", "VolumeGuard",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        try
        {
            ConfigService.Instance.Load();
            PasswordService.Instance.LoadHash(ConfigService.Instance.Data.PasswordHash);

            if (!PasswordService.Instance.HasPassword)
            {
                using var frm = new FirstRunForm();
                if (frm.ShowDialog() != DialogResult.OK)
                    return;
            }

            Watchdog.EnsureRunning();

            Application.Run(new AppContext());
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex);
            MessageBox.Show($"Greška:\n{ex.Message}\n\nLog: {ErrorLog.Path}",
                "VolumeGuard", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            mutex.ReleaseMutex();
            mutex.Dispose();
        }
    }
}
