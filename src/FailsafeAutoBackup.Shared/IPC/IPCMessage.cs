namespace FailsafeAutoBackup.Shared.IPC;

public enum MessageType
{
    GetStatus,
    StatusResponse,
    RestartService,
    GetBackupList,
    BackupListResponse,
    UpdateConfiguration,
    ConfigurationUpdated,
    Error,
    Heartbeat,
    HeartbeatResponse
}

public class IPCMessage
{
    public MessageType Type { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
}
