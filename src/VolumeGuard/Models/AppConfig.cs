namespace VolumeGuard.Models;

/// <summary>
/// Persisted application settings (JSON on disk).
/// </summary>
public sealed class AppConfig
{
    /// <summary>BCrypt password hash; empty until first run.</summary>
    public string PasswordHash { get; set; } = "";

    public List<ScheduleSlot> ScheduleSlots { get; set; } = new();

    /// <summary>Polling interval for volume enforcement (milliseconds).</summary>
    public int PollingIntervalMs { get; set; } = 500;

    /// <summary>Register in HKCU Run for current user.</summary>
    public bool StartupEnabled { get; set; } = true;

    /// <summary>When true, enforcement loop clamps volume; UI can reflect this.</summary>
    public bool ProtectionEnabled { get; set; } = true;

    /// <summary>
    /// Gap policy: when current time is not inside any slot, use the last slot's limit
    /// that was active before the gap (see ScheduleService).
    /// </summary>
    public GapBehavior GapBehavior { get; set; } = GapBehavior.HoldPreviousLimit;
}

public enum GapBehavior
{
    /// <summary>Keep the limit from the chronologically previous slot until the next slot starts.</summary>
    HoldPreviousLimit,

    /// <summary>Use a fixed fallback percent (see ScheduleService).</summary>
    FixedFallback
}

public sealed class ScheduleSlot
{
    public string Start { get; set; } = "00:00";
    public string End { get; set; } = "24:00";
    public int MaxVolumePercent { get; set; } = 100;
}
