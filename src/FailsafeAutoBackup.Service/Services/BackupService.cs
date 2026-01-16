using FailsafeAutoBackup.Shared.Models;
using FailsafeAutoBackup.Shared.Configuration;
using FailsafeAutoBackup.Service.Interfaces;

namespace FailsafeAutoBackup.Service.Services;

public class BackupService : IBackupService
{
    private readonly ILogger<BackupService> _logger;
    private readonly ServiceConfiguration _configuration;
    private readonly List<BackupDocument> _backupHistory = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public BackupService(ILogger<BackupService> logger, ServiceConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        EnsureBackupDirectoryExists();
    }

    private void EnsureBackupDirectoryExists()
    {
        if (string.IsNullOrWhiteSpace(_configuration.BackupFolderPath))
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _configuration.BackupFolderPath = Path.Combine(userProfile, "FailsafeAutoBackup");
        }

        if (!Directory.Exists(_configuration.BackupFolderPath))
        {
            Directory.CreateDirectory(_configuration.BackupFolderPath);
        }
    }

    public async Task BackupDocumentAsync(BackupDocument document, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var backupFileName = GenerateBackupFileName(document);
            var backupPath = Path.Combine(_configuration.BackupFolderPath, backupFileName);

            // Create document type folder
            var typeFolder = Path.Combine(_configuration.BackupFolderPath, document.Type.ToString());
            if (!Directory.Exists(typeFolder))
            {
                Directory.CreateDirectory(typeFolder);
            }

            backupPath = Path.Combine(typeFolder, backupFileName);

            // Simulate backup (in production, this would copy the actual file)
            await File.WriteAllTextAsync(backupPath, 
                $"Backup of {document.FilePath} at {DateTime.UtcNow}", 
                cancellationToken);

            document.LastBackupTime = DateTime.UtcNow;
            _backupHistory.Add(document);

            // Maintain version limit
            await CleanOldBackupsAsync(document, cancellationToken);

            _logger.LogInformation("Backed up document: {FilePath} to {BackupPath}", 
                document.FilePath, backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error backing up document: {FilePath}", document.FilePath);
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GenerateBackupFileName(BackupDocument document)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = Path.GetFileNameWithoutExtension(document.FilePath);
        var extension = Path.GetExtension(document.FilePath);
        
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = $"{document.ApplicationName}_{document.Type}";
        }

        return $"{fileName}_{timestamp}{extension}";
    }

    private async Task CleanOldBackupsAsync(BackupDocument document, CancellationToken cancellationToken)
    {
        try
        {
            var typeFolder = Path.Combine(_configuration.BackupFolderPath, document.Type.ToString());
            if (!Directory.Exists(typeFolder)) return;

            var files = Directory.GetFiles(typeFolder)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            if (files.Count > _configuration.MaxBackupVersions)
            {
                var filesToDelete = files.Skip(_configuration.MaxBackupVersions);
                foreach (var file in filesToDelete)
                {
                    file.Delete();
                    _logger.LogDebug("Deleted old backup: {FilePath}", file.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning old backups");
        }

        await Task.CompletedTask;
    }

    public async Task<List<BackupDocument>> GetBackupHistoryAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return new List<BackupDocument>(_backupHistory);
        }
        finally
        {
            _lock.Release();
        }
    }
}
