using VolumeGuard.Helpers;
using VolumeGuard.Models;

namespace VolumeGuard.Services;

public sealed class ScheduleService
{
    private readonly ConfigService _config;

    public ScheduleService(ConfigService config)
    {
        _config = config;
    }

    /// <summary>
    /// Computes allowed max volume (0–1) for local clock time.
    /// Overnight ranges (start &gt; end), e.g. 22:00–06:30, are supported.
    /// <para>
    /// Gap 07:30–08:30 (no slot): with <see cref="GapBehavior.HoldPreviousLimit"/>, the limit stays at 40%
    /// because the previous slot (06:30–07:30) is the last one that was active before the gap.
    /// </para>
    /// </summary>
    public (double maxScalar, string description) GetAllowedMaximum(TimeOnly now)
    {
        var cfg = _config.Current;
        var parsed = cfg.ScheduleSlots
            .Select(ParseSlot)
            .Where(x => x != null)
            .Cast<ParsedSlot>()
            .ToList();

        if (parsed.Count == 0)
            return (1.0, "No valid schedule — 100%");

        var m = now.ToTimeSpan().TotalMinutes;

        foreach (var s in parsed)
        {
            if (IsInside(m, s))
                return (s.MaxPercent / 100.0, $"{s.StartLabel}-{s.EndLabel} ({s.MaxPercent}%)");
        }

        if (cfg.GapBehavior == GapBehavior.FixedFallback)
            return (0.4, "Gap — fixed 40%");

        // Hold previous: last same-day segment that ended at or before "now" (end exclusive in IsInside).
        var prev = FindHoldPreviousLimit(m, parsed);
        if (prev.HasValue)
            return (prev.Value.pct / 100.0, $"Gap — hold previous ({prev.Value.label}, {prev.Value.pct}%)");

        return (0.4, "Gap — default 40%");
    }

    private static bool IsInside(double m, ParsedSlot s)
    {
        if (!s.Wraps)
            return m >= s.StartMin && m < s.EndMin;

        // Overnight: [start, 1440) U [0, end)
        return m >= s.StartMin || m < s.EndMin;
    }

    /// <summary>
    /// For gaps like 07:30–08:30: pick the non-overnight slot with the latest EndMin that is still &lt;= m (e.g. 06:30–07:30).
    /// </summary>
    private static (int pct, string label)? FindHoldPreviousLimit(double m, List<ParsedSlot> slots)
    {
        var nonWrapping = slots.Where(s => !s.Wraps).ToList();
        var endedAtOrBefore = nonWrapping.Where(s => s.EndMin <= m).ToList();
        if (endedAtOrBefore.Count > 0)
        {
            var c = endedAtOrBefore.OrderByDescending(s => s.EndMin).First();
            return (c.MaxPercent, $"{c.StartLabel}-{c.EndLabel}");
        }

        var overnight = slots.FirstOrDefault(s => s.Wraps);
        if (overnight != null && m < overnight.EndMin)
            return (overnight.MaxPercent, $"{overnight.StartLabel}-{overnight.EndLabel}");

        return null;
    }

    private static ParsedSlot? ParseSlot(ScheduleSlot slot)
    {
        if (!TimeOnlyHelper.TryParse(slot.Start, out var ts) || !TimeOnlyHelper.TryParse(slot.End, out var te))
            return null;

        var startMin = ts.ToTimeSpan().TotalMinutes;
        var endMin = te.ToTimeSpan().TotalMinutes;
        var wraps = startMin >= endMin;
        var pct = Math.Clamp(slot.MaxVolumePercent, 0, 100);
        return new ParsedSlot(slot.Start, slot.End, startMin, endMin, wraps, pct);
    }

    private sealed record ParsedSlot(string StartLabel, string EndLabel, double StartMin, double EndMin, bool Wraps, int MaxPercent);
}
