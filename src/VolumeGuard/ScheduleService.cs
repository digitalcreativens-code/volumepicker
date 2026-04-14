namespace VolumeGuard;

internal static class ScheduleService
{
    public static (int maxPct, string label) GetLimit(DateTime now)
    {
        var slots = ConfigService.Instance.Data.Slots;
        if (slots.Count == 0) return (100, "Nema rasporeda — 100%");

        var m = now.Hour * 60 + now.Minute;

        foreach (var s in slots)
        {
            if (!TryParse(s.From, out var f) || !TryParse(s.To, out var t)) continue;
            var pct = Math.Clamp(s.MaxPct, 0, 100);
            if (f < t)
            {
                if (m >= f && m < t) return (pct, $"{s.From}-{s.To} ({pct}%)");
            }
            else
            {
                // overnight e.g. 22:00-06:30
                if (m >= f || m < t) return (pct, $"{s.From}-{s.To} ({pct}%)");
            }
        }

        // Gap (07:30-08:30): hold previous — find last ended slot
        var best = slots
            .Select(s => (s, ok: TryParse(s.From, out var f2) && TryParse(s.To, out var t2), f2: ParseMin(s.From), t2: ParseMin(s.To)))
            .Where(x => x.ok && x.f2 < x.t2 && x.t2 <= m)
            .OrderByDescending(x => x.t2)
            .Select(x => x.s)
            .FirstOrDefault();

        if (best != null)
        {
            var p = Math.Clamp(best.MaxPct, 0, 100);
            return (p, $"Rupa — drži prethodni ({best.From}-{best.To}, {p}%)");
        }

        // Fallback for overnight tail
        var over = slots.FirstOrDefault(s =>
            TryParse(s.From, out var f3) && TryParse(s.To, out var t3) && f3 > t3 && m < t3);
        if (over != null)
        {
            var p = Math.Clamp(over.MaxPct, 0, 100);
            return (p, $"Rupa — drži noćni ({over.From}-{over.To}, {p}%)");
        }

        return (40, "Rupa — podrazumevano 40%");
    }

    private static bool TryParse(string hhmm, out int mins)
    {
        mins = 0;
        var p = hhmm.Split(':');
        if (p.Length != 2) return false;
        if (!int.TryParse(p[0], out var h) || !int.TryParse(p[1], out var mi)) return false;
        mins = h * 60 + mi;
        return true;
    }

    private static int ParseMin(string hhmm) { TryParse(hhmm, out var m); return m; }
}
