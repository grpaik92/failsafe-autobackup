using System.Runtime.InteropServices;
using System.Threading.Channels;
using FailsafeAutoBackup.Shared.Models;

namespace FailsafeAutoBackup.Service.Services;

/// <summary>
/// Handles Word COM automation on an STA thread with timeout protection
/// </summary>
public class WordComService : IDisposable
{
    private readonly ILogger<WordComService> _logger;
    private readonly Channel<ComRequest> _requestChannel;
    private readonly Task _staThread;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public WordComService(ILogger<WordComService> logger)
    {
        _logger = logger;
        _requestChannel = Channel.CreateUnbounded<ComRequest>();
        _staThread = Task.Factory.StartNew(
            () => ProcessComRequestsAsync(_cts.Token),
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public async Task<List<BackupDocument>> GetActiveWordDocumentsAsync(int timeoutSeconds = 30)
    {
        var request = new ComRequest
        {
            ResponseChannel = Channel.CreateUnbounded<List<BackupDocument>>()
        };

        await _requestChannel.Writer.WriteAsync(request);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            var result = await request.ResponseChannel.Reader.ReadAsync(cts.Token);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Word COM operation timed out after {Timeout} seconds", timeoutSeconds);
            return new List<BackupDocument>();
        }
    }

    [STAThread]
    private async Task ProcessComRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var request = await _requestChannel.Reader.ReadAsync(cancellationToken);
                var documents = await GetDocumentsFromWordAsync();
                await request.ResponseChannel.Writer.WriteAsync(documents, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing COM request");
            }
        }
    }

    private async Task<List<BackupDocument>> GetDocumentsFromWordAsync()
    {
        var documents = new List<BackupDocument>();

        try
        {
            // This is a placeholder for actual Word COM automation
            // In production, this would use Microsoft.Office.Interop.Word
            
            // Type wordType = Type.GetTypeFromProgID("Word.Application");
            // if (wordType == null) return documents;
            
            // dynamic? wordApp = Marshal.GetActiveObject("Word.Application");
            // if (wordApp != null)
            // {
            //     foreach (dynamic doc in wordApp.Documents)
            //     {
            //         var backupDoc = new BackupDocument
            //         {
            //             FilePath = doc.FullName,
            //             ApplicationName = "Microsoft Word",
            //             Type = DocumentType.Word,
            //             IsUnsaved = !doc.Saved,
            //             LastBackupTime = DateTime.UtcNow
            //         };
            //         documents.Add(backupDoc);
            //     }
            //     Marshal.ReleaseComObject(wordApp);
            // }
        }
        catch (COMException ex)
        {
            _logger.LogWarning(ex, "Word COM exception - Word may not be running");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing Word documents");
        }

        return await Task.FromResult(documents);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cts.Cancel();
        _requestChannel.Writer.Complete();
        
        try
        {
            _staThread.Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error waiting for STA thread to complete");
        }

        _cts.Dispose();
        _disposed = true;
    }

    private class ComRequest
    {
        public Channel<List<BackupDocument>> ResponseChannel { get; set; } = null!;
    }
}
