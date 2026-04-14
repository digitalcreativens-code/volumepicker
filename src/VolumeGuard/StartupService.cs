using Microsoft.Win32;

namespace VolumeGuard;

internal static class StartupService
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string Name = "VolumeGuard";

    public static bool IsEnabled()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Key, false);
            return !string.IsNullOrEmpty(k?.GetValue(Name) as string);
        }
        catch { return false; }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Key, true);
            if (k == null) return;
            if (enabled)
            {
                var exe = Environment.ProcessPath ?? "";
                if (exe.Contains(' ')) exe = $"\"{exe}\"";
                k.SetValue(Name, exe);
            }
            else
            {
                k.DeleteValue(Name, false);
            }
        }
        catch { }
    }
}
