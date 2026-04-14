namespace VolumeGuard.Services;

public sealed class PasswordService
{
    private const int WorkFactor = 12;

    public string? Hash { get; private set; }

    public bool HasPassword => !string.IsNullOrEmpty(Hash);

    public void LoadFromConfig(string? passwordHash)
    {
        Hash = string.IsNullOrWhiteSpace(passwordHash) ? null : passwordHash;
    }

    public void SetPassword(string plain)
    {
        Hash = BCrypt.Net.BCrypt.HashPassword(plain, workFactor: WorkFactor);
    }

    public bool Verify(string plain)
    {
        if (string.IsNullOrEmpty(Hash)) return false;
        try
        {
            return BCrypt.Net.BCrypt.Verify(plain, Hash);
        }
        catch
        {
            return false;
        }
    }
}
