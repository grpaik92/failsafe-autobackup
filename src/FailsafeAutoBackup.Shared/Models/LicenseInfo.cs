namespace FailsafeAutoBackup.Shared.Models;

public class LicenseInfo
{
    public string UserId { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public int MaxDevices { get; set; } = 1;
}

public enum SubscriptionStatus
{
    Active,
    Inactive,
    Expired,
    Cancelled
}
