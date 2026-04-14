using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using VolumeGuard.Models;

namespace VolumeGuard.Services;

public sealed class ConfigService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ConfigPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VolumeGuard", "config.json");

    public AppConfig Current { get; private set; } = CreateDefault();

    public void Load()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(ConfigPath))
            {
                Current = CreateDefault();
                Save();
                return;
            }

            var json = File.ReadAllText(ConfigPath);
            var loaded = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
            Current = loaded ?? CreateDefault();
            Normalize(Current);
        }
        catch
        {
            Current = CreateDefault();
        }
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var temp = ConfigPath + ".tmp";
        var json = JsonSerializer.Serialize(Current, _jsonOptions);
        File.WriteAllText(temp, json);
        File.Copy(temp, ConfigPath, overwrite: true);
        File.Delete(temp);
    }

    private static void Normalize(AppConfig c)
    {
        if (c.PollingIntervalMs < 100) c.PollingIntervalMs = 100;
        if (c.PollingIntervalMs > 10_000) c.PollingIntervalMs = 10_000;
        if (c.ScheduleSlots.Count == 0)
            c.ScheduleSlots = CreateDefault().ScheduleSlots;
    }

    /// <summary>
    /// Default schedule per spec:
    /// - 22:00–06:30 max 20% (overnight wrap)
    /// - 06:30–07:30 max 40%
    /// - 08:30–22:00 max 85%
    /// Gap 07:30–08:30: no explicit slot — HoldPreviousLimit keeps 40% from the morning slot until 08:30.
    /// </summary>
    public static AppConfig CreateDefault()
    {
        return new AppConfig
        {
            PollingIntervalMs = 500,
            StartupEnabled = true,
            ProtectionEnabled = true,
            GapBehavior = GapBehavior.HoldPreviousLimit,
            ScheduleSlots =
            {
                new ScheduleSlot { Start = "22:00", End = "06:30", MaxVolumePercent = 20 },
                new ScheduleSlot { Start = "06:30", End = "07:30", MaxVolumePercent = 40 },
                new ScheduleSlot { Start = "08:30", End = "22:00", MaxVolumePercent = 85 }
            }
        };
    }
}
