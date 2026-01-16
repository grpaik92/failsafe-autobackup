namespace FailsafeAutoBackup.Shared.Models;

public class UserSession
{
    public string UserId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public bool IsLicenseValid { get; set; }
}
