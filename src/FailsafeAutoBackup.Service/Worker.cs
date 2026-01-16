using FailsafeAutoBackup.Service.Interfaces;
using FailsafeAutoBackup.Service.Resilience;
using FailsafeAutoBackup.Shared.Configuration;
using FailsafeAutoBackup.Shared.Models;
using Polly;

namespace FailsafeAutoBackup.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDocumentDetector _documentDetector;
    private readonly IBackupService _backupService;
    private readonly IHeartbeatService _heartbeatService;
    private readonly ServiceConfiguration _configuration;
    private readonly IAsyncPolicy _resiliencePolicy;

    public Worker(
        ILogger<Worker> logger,
        IDocumentDetector documentDetector,
        IBackupService backupService,
        IHeartbeatService heartbeatService,
        ServiceConfiguration configuration)
    {
        _logger = logger;
        _documentDetector = documentDetector;
        _backupService = backupService;
        _heartbeatService = heartbeatService;
        _configuration = configuration;
        _resiliencePolicy = ResiliencePolicies.CreateCompositePolicy(logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Failsafe AutoBackup Service starting at: {Time}", DateTimeOffset.Now);

        // Initial delay to allow system to stabilize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _resiliencePolicy.ExecuteAsync(async (ct) =>
                {
                    await PerformBackupCycleAsync(ct);
                }, stoppingToken);

                // Update heartbeat after successful cycle
                _heartbeatService.UpdateHeartbeat();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Service stopping - cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in backup cycle - service will retry");
                
                // Exponential backoff on critical errors
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            // Wait for next backup interval
            var delay = TimeSpan.FromMinutes(_configuration.BackupIntervalMinutes);
            _logger.LogDebug("Next backup cycle in {Minutes} minutes", delay.TotalMinutes);
            
            await Task.Delay(delay, stoppingToken);
        }

        _logger.LogInformation("Failsafe AutoBackup Service stopped at: {Time}", DateTimeOffset.Now);
    }

    private async Task PerformBackupCycleAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting backup cycle");

        try
        {
            // Detect active documents
            var documents = await _documentDetector.DetectActiveDocumentsAsync(cancellationToken);
            
            if (documents.Count == 0)
            {
                _logger.LogDebug("No active documents detected");
                return;
            }

            _logger.LogInformation("Detected {Count} active documents", documents.Count);

            // Backup each document
            foreach (var document in documents)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    await _backupService.BackupDocumentAsync(document, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error backing up document: {FilePath}", document.FilePath);
                    // Continue with next document even if one fails
                }
            }

            _logger.LogInformation("Backup cycle completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in backup cycle");
            throw; // Re-throw to trigger resilience policies
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Service stop requested");
        await base.StopAsync(cancellationToken);
    }
}

