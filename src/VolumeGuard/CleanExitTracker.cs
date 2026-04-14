namespace VolumeGuard;

/// <summary>
/// Lets the watchdog distinguish intentional shutdown (password-confirmed) from a crash,
/// so it does not immediately respawn the UI after a deliberate exit.
/// </summary>
public static class CleanExitTracker
{
    private static string FlagPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VolumeGuard", "clean_exit.flag");

    public static void ClearOnStartup()
    {
        try
        {
            if (File.Exists(FlagPath))
                File.Delete(FlagPath);
        }
        catch
        {
            // ignore
        }
    }

    public static void MarkCleanExit()
    {
        try
        {
            var dir = Path.GetDirectoryName(FlagPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(FlagPath, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>Returns true if a clean exit was recorded very recently (and consumes the flag).</summary>
    public static bool TryConsumeRecentCleanExit(TimeSpan window)
    {
        try
        {
            if (!File.Exists(FlagPath))
                return false;

            var text = File.ReadAllText(FlagPath).Trim();
            File.Delete(FlagPath);
            if (!long.TryParse(text, out var epoch))
                return false;

            var t = DateTimeOffset.FromUnixTimeSeconds(epoch);
            return DateTimeOffset.UtcNow - t <= window;
        }
        catch
        {
            return false;
        }
    }
}
