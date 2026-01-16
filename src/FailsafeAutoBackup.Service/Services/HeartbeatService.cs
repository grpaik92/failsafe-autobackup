using FailsafeAutoBackup.Service.Interfaces;

namespace FailsafeAutoBackup.Service.Services;

public class HeartbeatService : IHeartbeatService
{
    private DateTime _lastHeartbeat = DateTime.UtcNow;
    private readonly TimeSpan _healthTimeout = TimeSpan.FromMinutes(10);

    public DateTime LastHeartbeat => _lastHeartbeat;

    public bool IsHealthy => DateTime.UtcNow - _lastHeartbeat < _healthTimeout;

    public void UpdateHeartbeat()
    {
        _lastHeartbeat = DateTime.UtcNow;
    }
}
