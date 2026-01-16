using System.Diagnostics;
using System.Runtime.InteropServices;
using FailsafeAutoBackup.Shared.Models;
using FailsafeAutoBackup.Service.Interfaces;

namespace FailsafeAutoBackup.Service.Services;

public class DocumentDetector : IDocumentDetector
{
    private readonly ILogger<DocumentDetector> _logger;

    public DocumentDetector(ILogger<DocumentDetector> logger)
    {
        _logger = logger;
    }

    public async Task<List<BackupDocument>> DetectActiveDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var documents = new List<BackupDocument>();

        try
        {
            // Detect Word documents
            var wordDocs = await DetectWordDocumentsAsync(cancellationToken);
            documents.AddRange(wordDocs);

            // Detect PDF documents
            var pdfDocs = await DetectPdfDocumentsAsync(cancellationToken);
            documents.AddRange(pdfDocs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting active documents");
        }

        return documents;
    }

    private async Task<List<BackupDocument>> DetectWordDocumentsAsync(CancellationToken cancellationToken)
    {
        var documents = new List<BackupDocument>();

        try
        {
            var wordProcesses = Process.GetProcessesByName("WINWORD");
            
            foreach (var process in wordProcesses)
            {
                try
                {
                    // This is a placeholder - actual COM automation would be used in production
                    // See WordComService for actual implementation
                    var doc = new BackupDocument
                    {
                        ApplicationName = "Microsoft Word",
                        Type = DocumentType.Word,
                        FilePath = $"Word_{process.Id}",
                        IsUnsaved = false,
                        LastBackupTime = DateTime.UtcNow
                    };
                    documents.Add(doc);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error accessing Word process {ProcessId}", process.Id);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting Word documents");
        }

        return await Task.FromResult(documents);
    }

    private async Task<List<BackupDocument>> DetectPdfDocumentsAsync(CancellationToken cancellationToken)
    {
        var documents = new List<BackupDocument>();

        try
        {
            // Detect Adobe Acrobat
            var acrobatProcesses = Process.GetProcessesByName("Acrobat");
            
            foreach (var process in acrobatProcesses)
            {
                try
                {
                    var doc = new BackupDocument
                    {
                        ApplicationName = "Adobe Acrobat",
                        Type = DocumentType.Pdf,
                        FilePath = $"PDF_{process.Id}",
                        IsUnsaved = false,
                        LastBackupTime = DateTime.UtcNow
                    };
                    documents.Add(doc);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error accessing Acrobat process {ProcessId}", process.Id);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting PDF documents");
        }

        return await Task.FromResult(documents);
    }
}
