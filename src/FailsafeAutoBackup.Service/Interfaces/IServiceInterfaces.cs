using FailsafeAutoBackup.Shared.Models;

namespace FailsafeAutoBackup.Service.Interfaces;

public interface IDocumentDetector
{
    Task<List<BackupDocument>> DetectActiveDocumentsAsync(CancellationToken cancellationToken = default);
}

public interface IBackupService
{
    Task BackupDocumentAsync(BackupDocument document, CancellationToken cancellationToken = default);
    Task<List<BackupDocument>> GetBackupHistoryAsync(CancellationToken cancellationToken = default);
}

public interface IHeartbeatService
{
    DateTime LastHeartbeat { get; }
    bool IsHealthy { get; }
    void UpdateHeartbeat();
}
