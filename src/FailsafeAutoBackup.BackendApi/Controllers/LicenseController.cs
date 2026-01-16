using FailsafeAutoBackup.BackendApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FailsafeAutoBackup.BackendApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicenseController : ControllerBase
{
    private readonly ILicensingService _licensingService;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(ILicensingService licensingService, ILogger<LicenseController> logger)
    {
        _licensingService = licensingService;
        _logger = logger;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateLicense([FromBody] ValidateLicenseRequest request)
    {
        if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.DeviceFingerprint))
        {
            return BadRequest(new { error = "UserId and DeviceFingerprint are required" });
        }

        var isValid = await _licensingService.ValidateLicenseAsync(request.UserId, request.DeviceFingerprint);
        
        return Ok(new { isValid, message = isValid ? "License is valid" : "License is invalid or device limit reached" });
    }

    [HttpPost("register-device")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        if (string.IsNullOrEmpty(request.UserId) || 
            string.IsNullOrEmpty(request.DeviceFingerprint) || 
            string.IsNullOrEmpty(request.DeviceName))
        {
            return BadRequest(new { error = "UserId, DeviceFingerprint, and DeviceName are required" });
        }

        var registered = await _licensingService.RegisterDeviceAsync(
            request.UserId, 
            request.DeviceFingerprint, 
            request.DeviceName);

        if (registered)
        {
            return Ok(new { success = true, message = "Device registered successfully" });
        }

        return BadRequest(new { success = false, message = "Failed to register device. Check subscription status or device limit." });
    }

    [HttpPost("deactivate-device")]
    public async Task<IActionResult> DeactivateDevice([FromBody] DeactivateDeviceRequest request)
    {
        if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.DeviceFingerprint))
        {
            return BadRequest(new { error = "UserId and DeviceFingerprint are required" });
        }

        var deactivated = await _licensingService.DeactivateDeviceAsync(request.UserId, request.DeviceFingerprint);
        
        return Ok(new { success = deactivated, message = deactivated ? "Device deactivated successfully" : "Device not found" });
    }
}

public record ValidateLicenseRequest(string UserId, string DeviceFingerprint);
public record RegisterDeviceRequest(string UserId, string DeviceFingerprint, string DeviceName);
public record DeactivateDeviceRequest(string UserId, string DeviceFingerprint);
