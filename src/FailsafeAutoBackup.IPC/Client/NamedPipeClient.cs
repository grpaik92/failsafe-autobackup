using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using FailsafeAutoBackup.Shared.IPC;

namespace FailsafeAutoBackup.IPC.Client;

public class NamedPipeClient : IDisposable
{
    private const string PipeName = "FailsafeAutoBackup_Pipe";
    private NamedPipeClientStream? _client;
    private bool _disposed;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsConnected => _client?.IsConnected ?? false;

    public async Task<bool> ConnectAsync(int timeoutMs = 5000, CancellationToken cancellationToken = default)
    {
        try
        {
            _client = new NamedPipeClientStream(
                ".",
                PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            using var cts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            
            await _client.ConnectAsync(linkedCts.Token);
            return true;
        }
        catch (Exception)
        {
            _client?.Dispose();
            _client = null;
            return false;
        }
    }

    public async Task<IPCMessage?> SendMessageAsync(IPCMessage message, CancellationToken cancellationToken = default)
    {
        if (_client == null || !_client.IsConnected)
        {
            return null;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Send message
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _client.WriteAsync(bytes, cancellationToken);
            await _client.FlushAsync(cancellationToken);

            // Read response
            return await ReadMessageAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Log error
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IPCMessage?> ReadMessageAsync(CancellationToken cancellationToken)
    {
        if (_client == null || !_client.IsConnected) return null;

        try
        {
            var buffer = new byte[4096];
            var bytesRead = await _client.ReadAsync(buffer, cancellationToken);
            
            if (bytesRead > 0)
            {
                var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                return JsonSerializer.Deserialize<IPCMessage>(json);
            }
        }
        catch (Exception)
        {
            // Log error
        }

        return null;
    }

    public void Disconnect()
    {
        _client?.Dispose();
        _client = null;
    }

    public void Dispose()
    {
        if (_disposed) return;

        Disconnect();
        _lock.Dispose();
        _disposed = true;
    }
}
