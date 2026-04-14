namespace VolumeGuard.Helpers;

public static class TimeOnlyHelper
{
    public static bool TryParse(string hhmm, out TimeOnly time)
    {
        time = default;
        var parts = hhmm.Split(':');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m)) return false;
        if (h is < 0 or > 23 || m is < 0 or > 59) return false;
        time = new TimeOnly(h, m);
        return true;
    }
}
