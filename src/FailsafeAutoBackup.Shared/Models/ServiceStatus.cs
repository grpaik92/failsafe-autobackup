namespace FailsafeAutoBackup.Shared.Models;

public class ServiceStatus
{
    public bool IsRunning { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public int ActiveDocuments { get; set; }
    public int TotalBackups { get; set; }
    public DateTime ServiceStartTime { get; set; }
    public string Version { get; set; } = "1.0.0";
    public List<string> RecentErrors { get; set; } = new();
}
