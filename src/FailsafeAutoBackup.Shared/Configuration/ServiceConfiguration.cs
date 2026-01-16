namespace FailsafeAutoBackup.Shared.Configuration;

public class ServiceConfiguration
{
    public int BackupIntervalMinutes { get; set; } = 2;
    public string BackupFolderPath { get; set; } = string.Empty;
    public bool EnableWordBackup { get; set; } = true;
    public bool EnablePdfBackup { get; set; } = true;
    public int MaxBackupVersions { get; set; } = 10;
    public int ComTimeoutSeconds { get; set; } = 30;
    public bool CreateDesktopShortcut { get; set; } = true;
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}

public enum LogLevel
{
    Debug,
    Information,
    Warning,
    Error
}
