using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using FailsafeAutoBackup.Shared.IPC;

namespace FailsafeAutoBackup.IPC.Server;

public class NamedPipeServer : IDisposable
{
    private const string PipeName = "FailsafeAutoBackup_Pipe";
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly List<Task> _clientTasks = new();
    private bool _disposed;

    public event EventHandler<IPCMessage>? MessageReceived;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
        
        while (!linkedCts.Token.IsCancellationRequested)
        {
            try
            {
                var server = CreateNamedPipeServer();
                await server.WaitForConnectionAsync(linkedCts.Token);

                var clientTask = Task.Run(() => HandleClientAsync(server, linkedCts.Token), linkedCts.Token);
                _clientTasks.Add(clientTask);

                // Clean up completed tasks
                _clientTasks.RemoveAll(t => t.IsCompleted);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Log error and continue
                await Task.Delay(1000, linkedCts.Token);
            }
        }
    }

    private NamedPipeServerStream CreateNamedPipeServer()
    {
        var pipeSecurity = new PipeSecurity();
        var identity = WindowsIdentity.GetCurrent();
        var userSid = identity.User;

        if (userSid != null)
        {
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                userSid,
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow));
        }

        // Allow administrators
        var adminsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        pipeSecurity.AddAccessRule(new PipeAccessRule(
            adminsSid,
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            PipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous,
            4096,
            4096,
            pipeSecurity);
    }

    private async Task HandleClientAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        try
        {
            using (server)
            {
                while (server.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    var message = await ReadMessageAsync(server, cancellationToken);
                    if (message != null)
                    {
                        MessageReceived?.Invoke(this, message);
                    }
                }
            }
        }
        catch (Exception)
        {
            // Log error
        }
    }

    public async Task SendMessageAsync(IPCMessage message, NamedPipeServerStream? server = null)
    {
        if (server == null || !server.IsConnected) return;

        try
        {
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await server.WriteAsync(bytes);
            await server.FlushAsync();
        }
        catch (Exception)
        {
            // Log error
        }
    }

    private async Task<IPCMessage?> ReadMessageAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        try
        {
            var buffer = new byte[4096];
            var bytesRead = await server.ReadAsync(buffer, cancellationToken);
            
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

    public void Dispose()
    {
        if (_disposed) return;

        _cancellationTokenSource.Cancel();
        Task.WaitAll(_clientTasks.ToArray(), TimeSpan.FromSeconds(5));
        _cancellationTokenSource.Dispose();
        _disposed = true;
    }
}
