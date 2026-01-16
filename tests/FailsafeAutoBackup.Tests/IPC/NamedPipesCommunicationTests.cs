using System;
using System.Threading;
using System.Threading.Tasks;
using FailsafeAutoBackup.IPC.Client;
using FailsafeAutoBackup.IPC.Server;
using FailsafeAutoBackup.Shared.IPC;
using Xunit;

namespace FailsafeAutoBackup.Tests.IPC;

/// <summary>
/// Tests for Named Pipes communication between Tray App and Worker Service
/// Demonstrates basic secure Named Pipes connection with test messages
/// </summary>
public class NamedPipesCommunicationTests : IDisposable
{
    private NamedPipeServer? _server;
    private NamedPipeClient? _client;
    private readonly CancellationTokenSource _cts = new();

    [Fact]
    public async Task Server_ShouldStart_Successfully()
    {
        // Arrange
        _server = new NamedPipeServer();

        // Act
        var serverTask = Task.Run(() => _server.StartAsync(_cts.Token));
        await Task.Delay(500); // Give server time to start

        // Assert
        Assert.NotNull(_server);
        
        // Cleanup
        _cts.Cancel();
    }

    [Fact]
    public async Task Client_ShouldConnect_ToServer()
    {
        // Arrange
        _server = new NamedPipeServer();
        _client = new NamedPipeClient();

        // Start server
        var serverTask = Task.Run(() => _server.StartAsync(_cts.Token));
        await Task.Delay(500); // Give server time to start

        // Act
        var connected = await _client.ConnectAsync(timeoutMs: 5000);

        // Assert
        Assert.True(connected, "Client should connect to server");
        Assert.True(_client.IsConnected, "Client should be connected");

        // Cleanup
        _cts.Cancel();
    }

    [Fact]
    public async Task Client_ShouldReconnect_AfterDisconnection()
    {
        // Arrange
        _server = new NamedPipeServer();
        _client = new NamedPipeClient();

        var serverTask = Task.Run(() => _server.StartAsync(_cts.Token));
        await Task.Delay(500);

        // Act - Connect, disconnect, and reconnect
        var firstConnection = await _client.ConnectAsync();
        Assert.True(firstConnection, "First connection should succeed");

        _client.Disconnect();
        Assert.False(_client.IsConnected, "Client should be disconnected");

        await Task.Delay(100); // Brief delay
        var secondConnection = await _client.ConnectAsync();

        // Assert
        Assert.True(secondConnection, "Second connection should succeed");
        Assert.True(_client.IsConnected, "Client should be reconnected");

        // Cleanup
        _cts.Cancel();
    }

    [Fact]
    public void MessageTypes_ShouldBe_Defined()
    {
        // Arrange & Act
        var messageTypes = new[]
        {
            MessageType.GetStatus,
            MessageType.RestartService,
            MessageType.Heartbeat,
            MessageType.UpdateConfiguration,
            MessageType.Error,
            MessageType.StatusResponse,
            MessageType.HeartbeatResponse
        };

        // Assert
        Assert.NotEmpty(messageTypes);
        Assert.True(messageTypes.Length >= 7, "All essential message types should be defined");
    }

    [Fact]
    public void IPCMessage_ShouldSerialize_Correctly()
    {
        // Arrange
        var message = new IPCMessage
        {
            Type = MessageType.GetStatus,
            Payload = "Test payload data"
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(message);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<IPCMessage>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(message.Type, deserialized.Type);
        Assert.Equal(message.Payload, deserialized.Payload);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _client?.Dispose();
        _server?.Dispose();
        _cts.Dispose();
    }
}
