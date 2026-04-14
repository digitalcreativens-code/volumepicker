using System.Text.Json;

namespace VolumeGuard;

internal sealed class ConfigService
{
    public static readonly ConfigService Instance = new();

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VolumeGuard", "config.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppConfig Data { get; private set; } = AppConfig.Default();

    public void Load()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            if (!File.Exists(FilePath))
            {
                Data = AppConfig.Default();
                Save();
                return;
            }
            Data = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(FilePath), JsonOpts)
                   ?? AppConfig.Default();
        }
        catch
        {
            Data = AppConfig.Default();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(Data, JsonOpts));
            File.Copy(tmp, FilePath, true);
            File.Delete(tmp);
        }
        catch { }
    }
}

internal sealed class AppConfig
{
    public string PasswordHash { get; set; } = "";
    public int PollMs { get; set; } = 500;
    public bool StartupEnabled { get; set; } = true;
    public bool ProtectionOn { get; set; } = true;
    public List<Slot> Slots { get; set; } = new();

    /// <summary>
    /// 22:00–06:30  20%
    /// 06:30–07:30  40%
    /// 08:30–22:00  85%
    /// Gap 07:30–08:30 holds previous (40%)
    /// </summary>
    public static AppConfig Default() => new()
    {
        PollMs = 500,
        StartupEnabled = true,
        ProtectionOn = true,
        Slots = new List<Slot>
        {
            new() { From = "22:00", To = "06:30", MaxPct = 20 },
            new() { From = "06:30", To = "07:30", MaxPct = 40 },
            new() { From = "08:30", To = "22:00", MaxPct = 85 }
        }
    };
}

internal sealed class Slot
{
    public string From { get; set; } = "00:00";
    public string To { get; set; } = "00:00";
    public int MaxPct { get; set; } = 100;
}
