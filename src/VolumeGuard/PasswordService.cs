namespace VolumeGuard;

internal sealed class PasswordService
{
    public static readonly PasswordService Instance = new();

    public string? Hash { get; private set; }
    public bool HasPassword => !string.IsNullOrEmpty(Hash);

    public void LoadHash(string? h) => Hash = string.IsNullOrWhiteSpace(h) ? null : h;

    public void SetPassword(string plain)
    {
        Hash = BCrypt.Net.BCrypt.HashPassword(plain, workFactor: 12);
        ConfigService.Instance.Data.PasswordHash = Hash;
        ConfigService.Instance.Save();
    }

    public bool Verify(string plain)
    {
        if (string.IsNullOrEmpty(Hash)) return false;
        try { return BCrypt.Net.BCrypt.Verify(plain, Hash); }
        catch { return false; }
    }
}
