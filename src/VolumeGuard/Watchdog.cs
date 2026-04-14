using System.Diagnostics;

namespace VolumeGuard;

internal static class Watchdog
{
    private static readonly string CleanFlag = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VolumeGuard", "clean.flag");

    public static void Run()
    {
        using var m = new Mutex(true, Constants.WatchdogMutex, out var ok);
        if (!ok) return;

        while (true)
        {
            try
            {
                if (!IsMutexHeld(Constants.MainMutex))
                {
                    if (WasCleanExit()) continue;
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Environment.ProcessPath!,
                        UseShellExecute = true
                    });
                }
            }
            catch { }
            Thread.Sleep(3000);
        }
    }

    public static void EnsureRunning()
    {
        if (IsMutexHeld(Constants.WatchdogMutex)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Environment.ProcessPath!,
                Arguments = "--watchdog",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch { }
    }

    public static void MarkCleanExit()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CleanFlag)!);
            File.WriteAllText(CleanFlag, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        }
        catch { }
    }

    public static bool IsRunning() => IsMutexHeld(Constants.WatchdogMutex);

    public static void ClearCleanFlag()
    {
        try { if (File.Exists(CleanFlag)) File.Delete(CleanFlag); } catch { }
    }

    private static bool WasCleanExit()
    {
        try
        {
            if (!File.Exists(CleanFlag)) return false;
            var txt = File.ReadAllText(CleanFlag).Trim();
            File.Delete(CleanFlag);
            if (!long.TryParse(txt, out var ep)) return false;
            return DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(ep) < TimeSpan.FromSeconds(30);
        }
        catch { return false; }
    }

    private static bool IsMutexHeld(string name)
    {
        try { Mutex.OpenExisting(name).Dispose(); return true; }
        catch { return false; }
    }
}
