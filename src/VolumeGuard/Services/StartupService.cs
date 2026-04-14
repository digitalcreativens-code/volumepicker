using Microsoft.Win32;

namespace VolumeGuard.Services;

public sealed class StartupService
{
    private const string ValueName = "VolumeGuard";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            var v = key?.GetValue(ValueName) as string;
            return !string.IsNullOrEmpty(v);
        }
        catch
        {
            return false;
        }
    }

    public void SetEnabled(bool enabled, string executablePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)
                  ?? throw new InvalidOperationException("Cannot open HKCU Run registry key.");

        if (enabled)
        {
            var quoted = executablePath.Contains(' ') ? $"\"{executablePath}\"" : executablePath;
            key.SetValue(ValueName, quoted, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
