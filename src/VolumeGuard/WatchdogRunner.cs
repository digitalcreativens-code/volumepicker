using System.Diagnostics;

namespace VolumeGuard;

/// <summary>
/// Same EXE, second mode: <c>VolumeGuard.exe --watchdog</c>. Restarts the main UI instance if missing.
/// Main app likewise ensures the watchdog is running — one binary to deploy ("single EXE" when published).
/// </summary>
public static class WatchdogRunner
{

    public static int Run(string[] _)
    {
        using var mutex = new Mutex(true, AppConstants.WatchdogMutexName, out var created);
        if (!created)
            return 0;

        var exe = GetExePath();
        try
        {
            while (true)
            {
                try
                {
                    if (!IsMainRunning())
                    {
                        if (CleanExitTracker.TryConsumeRecentCleanExit(TimeSpan.FromSeconds(30)))
                            continue;

                        StartMain(exe);
                    }
                }
                catch
                {
                    // ignore transient failures
                }

                Thread.Sleep(3000);
            }
        }
        catch
        {
            // ignore
        }

        return 0;
    }

    public static void EnsureWatchdogRunning()
    {
        try
        {
            if (IsWatchdogRunning())
                return;

            var exe = GetExePath();
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "--watchdog",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
        }
        catch
        {
            // non-fatal
        }
    }

    public static bool IsWatchdogRunning()
    {
        try
        {
            Mutex.OpenExisting(AppConstants.WatchdogMutexName).Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsMainRunning()
    {
        try
        {
            Mutex.OpenExisting(AppConstants.MainMutexName).Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void StartMain(string exe)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = true
        });
    }

    private static string GetExePath()
    {
        // Environment.ProcessPath is reliable for both normal and single-file published EXEs.
        // Assembly.Location returns empty string in single-file mode, so we never use it.
        return Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot determine executable path.");
    }
}
