using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VolumeGuard.Services;

namespace VolumeGuard.Views;

public static class PasswordGate
{
    public static bool VerifyOrCancel()
    {
        var sp = App.Services ?? throw new InvalidOperationException("Services not initialized.");
        var pwd = sp.GetRequiredService<PasswordService>();
        var dlg = sp.GetRequiredService<PasswordPromptWindow>();
        dlg.Owner = Application.Current.MainWindow;
        if (dlg.ShowDialog() != true)
            return false;

        if (string.IsNullOrEmpty(dlg.Password))
        {
            MessageBox.Show("Šifra je obavezna.", "VolumeGuard", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!pwd.Verify(dlg.Password))
        {
            MessageBox.Show("Pogrešna šifra.", "VolumeGuard", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }
}
