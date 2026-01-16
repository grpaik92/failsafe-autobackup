using FailsafeAutoBackup.BackendApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FailsafeAutoBackup.BackendApi.Services;

public interface ILicensingService
{
    Task<bool> ValidateLicenseAsync(string userId, string deviceFingerprint);
    Task<bool> RegisterDeviceAsync(string userId, string deviceFingerprint, string deviceName);
    Task<bool> DeactivateDeviceAsync(string userId, string deviceFingerprint);
}

public class LicensingService : ILicensingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LicensingService> _logger;

    public LicensingService(AppDbContext context, ILogger<LicensingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ValidateLicenseAsync(string userId, string deviceFingerprint)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Subscriptions)
                .Include(u => u.Devices)
                .FirstOrDefaultAsync(u => u.ClioUserId == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return false;
            }

            // Check if user has active subscription
            var activeSubscription = user.Subscriptions
                .FirstOrDefault(s => s.Status == "active" && 
                                   (s.EndDate == null || s.EndDate > DateTime.UtcNow));

            if (activeSubscription == null)
            {
                _logger.LogWarning("No active subscription for user: {UserId}", userId);
                return false;
            }

            // Check if device is registered
            var device = user.Devices
                .FirstOrDefault(d => d.DeviceFingerprint == deviceFingerprint && d.IsActive);

            if (device != null)
            {
                // Update last seen
                device.LastSeenAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            // Check if user can register more devices
            var activeDeviceCount = user.Devices.Count(d => d.IsActive);
            if (activeDeviceCount >= activeSubscription.MaxDevices)
            {
                _logger.LogWarning("Device limit reached for user: {UserId}", userId);
                return false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> RegisterDeviceAsync(string userId, string deviceFingerprint, string deviceName)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Subscriptions)
                .Include(u => u.Devices)
                .FirstOrDefaultAsync(u => u.ClioUserId == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return false;
            }

            // Check if user has active subscription
            var activeSubscription = user.Subscriptions
                .FirstOrDefault(s => s.Status == "active" && 
                                   (s.EndDate == null || s.EndDate > DateTime.UtcNow));

            if (activeSubscription == null)
            {
                _logger.LogWarning("No active subscription for user: {UserId}", userId);
                return false;
            }

            // Check device limit
            var activeDeviceCount = user.Devices.Count(d => d.IsActive);
            if (activeDeviceCount >= activeSubscription.MaxDevices)
            {
                _logger.LogWarning("Device limit reached for user: {UserId}", userId);
                return false;
            }

            // Check if device already registered
            var existingDevice = user.Devices
                .FirstOrDefault(d => d.DeviceFingerprint == deviceFingerprint);

            if (existingDevice != null)
            {
                if (!existingDevice.IsActive)
                {
                    existingDevice.IsActive = true;
                    existingDevice.LastSeenAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                return true;
            }

            // Register new device
            var device = new Device
            {
                UserId = user.Id,
                DeviceFingerprint = deviceFingerprint,
                DeviceName = deviceName,
                RegisteredAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Device registered for user: {UserId}, Device: {DeviceName}", userId, deviceName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DeactivateDeviceAsync(string userId, string deviceFingerprint)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Devices)
                .FirstOrDefaultAsync(u => u.ClioUserId == userId);

            if (user == null)
            {
                return false;
            }

            var device = user.Devices
                .FirstOrDefault(d => d.DeviceFingerprint == deviceFingerprint);

            if (device != null)
            {
                device.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating device for user: {UserId}", userId);
            return false;
        }
    }
}
