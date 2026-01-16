namespace FailsafeAutoBackup.Shared.Models;

public class BackupDocument
{
    public string FilePath { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public DocumentType Type { get; set; }
    public DateTime LastBackupTime { get; set; }
    public bool IsUnsaved { get; set; }
    public long FileSize { get; set; }
}

public enum DocumentType
{
    Word,
    Pdf,
    Unknown
}
