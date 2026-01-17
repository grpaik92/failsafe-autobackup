# Testing Plan

## Overview

This document outlines the comprehensive testing strategy for Failsafe AutoBackup, covering functional tests, reliability tests, and integration tests.

## Unit Tests

### Service Layer Tests

**Location**: `tests/FailsafeAutoBackup.Tests/ServiceTests/`

#### BackupService Tests
- ✅ `BackupDocumentAsync_CreatesBackupFile`
- ✅ `BackupDocumentAsync_CreatesTypeFolder`
- ✅ `BackupDocumentAsync_LimitsBackupVersions`
- ✅ `GetBackupHistoryAsync_ReturnsHistory`
- ✅ `CleanOldBackupsAsync_DeletesOldFiles`

#### DocumentDetector Tests
- ✅ `DetectActiveDocumentsAsync_FindsWordProcesses`
- ✅ `DetectActiveDocumentsAsync_FindsPdfProcesses`
- ✅ `DetectActiveDocumentsAsync_HandlesNoProcesses`
- ✅ `DetectActiveDocumentsAsync_HandlesExceptions`

#### HeartbeatService Tests
- ✅ `UpdateHeartbeat_UpdatesTimestamp`
- ✅ `IsHealthy_ReturnsTrueWhenRecent`
- ✅ `IsHealthy_ReturnsFalseWhenExpired`

### IPC Layer Tests

**Location**: `tests/FailsafeAutoBackup.Tests/IPCTests/`

#### Named Pipes Tests
- ✅ `NamedPipeClient_CanConnect`
- ✅ `NamedPipeClient_SendsMessage`
- ✅ `NamedPipeClient_ReceivesResponse`
- ✅ `NamedPipeServer_AcceptsMultipleClients`
- ✅ `NamedPipeServer_HandlesDisconnect`
- ✅ `IPCMessage_Serializes`
- ✅ `IPCMessage_Deserializes`

### Backend API Tests

**Location**: `tests/FailsafeAutoBackup.Tests/ApiTests/`

#### LicensingService Tests
- ✅ `ValidateLicenseAsync_ValidLicense_ReturnsTrue`
- ✅ `ValidateLicenseAsync_InvalidUser_ReturnsFalse`
- ✅ `ValidateLicenseAsync_ExpiredSubscription_ReturnsFalse`
- ✅ `ValidateLicenseAsync_DeviceLimitReached_ReturnsFalse`
- ✅ `RegisterDeviceAsync_Success`
- ✅ `RegisterDeviceAsync_DeviceLimitExceeded_ReturnsFalse`
- ✅ `DeactivateDeviceAsync_Success`

#### LicenseController Tests
- ✅ `ValidateLicense_ValidRequest_ReturnsOk`
- ✅ `ValidateLicense_MissingData_ReturnsBadRequest`
- ✅ `RegisterDevice_Success_ReturnsOk`
- ✅ `RegisterDevice_Failure_ReturnsBadRequest`

## Integration Tests

### End-to-End Backup Flow
1. Start Windows Service
2. Open Microsoft Word document
3. Verify document detected
4. Wait for backup cycle (2 minutes)
5. Verify backup file created
6. Verify backup file content
7. Close Word document
8. Verify no active documents

### IPC Communication Flow
1. Start Windows Service (IPC Server)
2. Start Tray App (IPC Client)
3. Send GetStatus message
4. Verify StatusResponse received
5. Send UpdateConfiguration message
6. Verify ConfigurationUpdated response

### License Validation Flow
1. Register user in backend
2. Create subscription
3. Register device
4. Validate license (should succeed)
5. Register second device
6. Validate license (should fail - device limit)
7. Deactivate first device
8. Validate license for second device (should succeed)

## Reliability Tests

### Service Resilience

#### Retry Logic Test
1. Simulate transient failure in backup operation
2. Verify retry with exponential backoff
3. Verify success on retry
4. Verify log entries for retries

#### Circuit Breaker Test
1. Simulate 5 consecutive failures
2. Verify circuit opens
3. Wait 1 minute
4. Verify circuit half-opens
5. Simulate success
6. Verify circuit closes

#### Fault Isolation Test
1. Simulate COM operation hang (> 30 seconds)
2. Verify timeout triggers
3. Verify operation aborted safely
4. Verify service continues running
5. Verify no resource leaks

### Service Recovery

#### Automatic Restart Test
1. Kill service process unexpectedly
2. Verify Windows restarts service (within 1 minute)
3. Verify service reconnects IPC
4. Verify backup operations resume

#### Watchdog Test
1. Stop heartbeat updates (simulate hang)
2. Wait 5 minutes
3. Verify watchdog detects unhealthy state
4. Verify watchdog restarts service

## Manual Testing Checklist

### Windows Service

- [ ] Service installs successfully
- [ ] Service starts automatically on boot
- [ ] Service runs continuously without crashes
- [ ] Service detects Word documents
- [ ] Service detects PDF documents
- [ ] Service creates backups every 2 minutes
- [ ] Service handles Word not running
- [ ] Service handles Acrobat not running
- [ ] Service survives Word crash
- [ ] Service survives Acrobat crash
- [ ] Service restarts on failure
- [ ] Service logs errors correctly

### Tray Application

- [ ] Tray app starts with Windows
- [ ] Tray icon appears in system tray
- [ ] Dashboard shows correct status
- [ ] Dashboard updates in real-time
- [ ] Settings can be changed
- [ ] Settings are persisted
- [ ] Restart Service button works
- [ ] Open Logs Folder button works
- [ ] Open Backup Folder button works
- [ ] Minimize to tray works
- [ ] Exit closes app and service

### IPC Communication

- [ ] Tray app connects to service
- [ ] Connection survives service restart
- [ ] Tray app auto-reconnects on disconnect
- [ ] Messages sent successfully
- [ ] Responses received successfully
- [ ] Multiple concurrent connections work
- [ ] Security permissions enforced

### Backend API

- [ ] API starts successfully
- [ ] Swagger UI accessible
- [ ] License validation endpoint works
- [ ] Device registration endpoint works
- [ ] Device deactivation endpoint works
- [ ] Database migrations run successfully
- [ ] CORS configured correctly
- [ ] Authentication works (when implemented)

## Performance Testing

### Backup Performance
- **Target**: < 1 second per document backup
- **Test**: Backup 100 documents, measure average time
- **Threshold**: Average < 1 second, 95th percentile < 2 seconds

### IPC Performance
- **Target**: < 100ms round-trip time
- **Test**: Send 1000 messages, measure round-trip time
- **Threshold**: Average < 100ms, 95th percentile < 200ms

### Memory Usage
- **Target**: < 100MB for service, < 50MB for tray app
- **Test**: Run for 24 hours, monitor memory usage
- **Threshold**: No memory leaks, stable memory usage

## Security Testing

### Named Pipes Security
- [ ] Verify only current user can connect
- [ ] Verify administrators can connect
- [ ] Verify other users cannot connect
- [ ] Verify unauthorized messages rejected

### Token Storage
- [ ] Verify tokens encrypted with DPAPI
- [ ] Verify tokens not in plaintext
- [ ] Verify tokens not logged
- [ ] Verify tokens cleared on logout

### API Security
- [ ] Verify HTTPS enforced
- [ ] Verify SQL injection prevention
- [ ] Verify XSS prevention
- [ ] Verify CSRF protection (when implemented)

## Continuous Testing

### CI/CD Pipeline
- All unit tests run on every commit
- All integration tests run on every commit
- Code coverage reports generated
- Test failures block merge

### Nightly Tests
- Full end-to-end test suite
- Performance benchmarks
- Memory leak detection
- Stress tests (24 hour runs)

## Installer Testing

### Build Verification
- [ ] WiX Toolset v4.0.5 installs successfully
- [ ] WixToolset.UI extension installs successfully
- [ ] MSI builds without errors
- [ ] MSI file size is reasonable (< 200MB)
- [ ] No WIX0144 errors during build

### Installation Testing (Clean Windows Environment)

#### Basic Installation
- [ ] MSI opens and displays welcome screen
- [ ] License agreement displays correctly
- [ ] Installation directory selection works
- [ ] Feature selection shows all components
- [ ] Progress bar displays during installation
- [ ] Installation completes successfully

#### Files and Directories
```powershell
# Verify installation paths (assumes default installation directory)
# For custom installations, check HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall for actual path
Test-Path "C:\Program Files\FailsafeAutoBackup\Service\FailsafeAutoBackup.Service.exe"
Test-Path "C:\Program Files\FailsafeAutoBackup\TrayApp\FailsafeAutoBackup.TrayApp.exe"
Test-Path "C:\ProgramData\FailsafeAutoBackup\Logs"
```

#### Windows Service
```powershell
# Check service installation
Get-Service "FailsafeAutoBackupService"

# Verify service properties
$service = Get-Service "FailsafeAutoBackupService"
$service.Status          # Should be: Running
$service.StartType       # Should be: Automatic
```

#### Shortcuts
```powershell
# Check Start Menu shortcut
Test-Path "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Failsafe AutoBackup\Failsafe AutoBackup.lnk"

# Check Desktop shortcut (if selected)
Test-Path "$env:USERPROFILE\Desktop\Failsafe AutoBackup.lnk"
```

#### Registry Keys
```powershell
# Check auto-start registry
Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "FailsafeAutoBackup"
```

### Functional Testing Post-Installation

#### Tray Application
- [ ] Tray App launches successfully
- [ ] Tray icon appears in system tray
- [ ] Context menu opens
- [ ] Configuration window opens
- [ ] Can connect to Windows Service via IPC

#### Windows Service
- [ ] Service starts automatically on boot
- [ ] Service runs without errors
- [ ] Service logs appear in Logs folder
- [ ] Service can be stopped/started via Services.msc
- [ ] Service restarts on failure (verify recovery policy)

#### Integration
- [ ] Tray App successfully communicates with Service
- [ ] Backup functionality works end-to-end
- [ ] Document detection works
- [ ] Backup files created in correct location

### Upgrade Testing
```powershell
# Install version 1.0.0
msiexec /i FailsafeAutoBackup-v1.0.0.msi /qn

# Upgrade to version 1.1.0
msiexec /i FailsafeAutoBackup-v1.1.0.msi /qn

# Verify:
# - Service updates without issues
# - Configuration preserved
# - No duplicate services
# - Old files removed
```

### Uninstallation Testing
```powershell
# Uninstall via MSI
msiexec /x FailsafeAutoBackup.msi /qn

# Or via Control Panel
```

#### Verify Cleanup
- [ ] Service stopped and removed
- [ ] Executables removed
- [ ] Shortcuts removed
- [ ] Registry keys removed (auto-start, etc.)
- [ ] Logs folder optionally preserved
- [ ] No leftover processes
- [ ] Can reinstall without issues

### Silent Installation Testing
```powershell
# Silent install
msiexec /i FailsafeAutoBackup.msi /qn /l*v install.log

# Verify installation succeeded
if (Get-Service "FailsafeAutoBackupService" -ErrorAction SilentlyContinue) {
    Write-Host "✓ Silent installation successful"
} else {
    Write-Host "✗ Silent installation failed"
}
```

### Test Matrix

| Test Scenario | Windows 10 | Windows 11 | Server 2019 | Server 2022 |
|--------------|-----------|-----------|-------------|-------------|
| Fresh Install | ✓ | ✓ | ✓ | ✓ |
| Upgrade | ✓ | ✓ | ✓ | ✓ |
| Uninstall | ✓ | ✓ | ✓ | ✓ |
| Silent Install | ✓ | ✓ | ✓ | ✓ |
| Service Auto-Start | ✓ | ✓ | ✓ | ✓ |
| Tray App Launch | ✓ | ✓ | N/A | N/A |

### Known Issues and Workarounds

#### Issue: Service fails to start immediately after installation
**Workaround**: Wait 5-10 seconds and try again. Service may need time to initialize.

#### Issue: Tray App doesn't auto-start on first login
**Workaround**: Log out and log back in. Auto-start registry key takes effect on next login.

### Installer CI/CD Checklist

- [ ] GitHub Actions workflow runs successfully
- [ ] WiX Toolset installs in CI environment
- [ ] WixToolset.UI extension installs in CI
- [ ] MSI artifact uploads successfully
- [ ] MSI artifact is downloadable
- [ ] No WIX0144 errors in CI logs
- [ ] Build completes in < 10 minutes

## Test Automation

### Tools
- **Unit Tests**: xUnit
- **Mocking**: Moq
- **Integration Tests**: WebApplicationFactory (for API)
- **UI Tests**: Appium/WinAppDriver (future)
- **Load Tests**: k6 (future)

### Coverage Goals
- **Unit Tests**: > 80% code coverage
- **Integration Tests**: All critical paths covered
- **Manual Tests**: All user scenarios covered

## Bug Reporting

### Issue Template
```
**Title**: [Component] Brief description

**Severity**: Critical / High / Medium / Low

**Steps to Reproduce**:
1. Step 1
2. Step 2
3. ...

**Expected Behavior**: What should happen

**Actual Behavior**: What actually happens

**Environment**:
- Windows Version:
- .NET Version:
- Application Version:

**Logs**: Attach relevant log files

**Screenshots**: If applicable
```

## Test Environments

### Development
- Local machine
- Windows 10/11
- Visual Studio 2022
- SQL Server LocalDB / SQLite

### Staging
- Azure VM / AWS EC2
- Windows Server 2022
- IIS
- PostgreSQL / SQL Server

### Production
- Customer machines
- Windows 10/11
- Real Word/Acrobat installations
- Production database

## Success Criteria

✅ All unit tests pass  
✅ All integration tests pass  
✅ Code coverage > 80%  
✅ No critical or high severity bugs  
✅ Performance targets met  
✅ Security tests pass  
✅ Manual testing checklist complete  
✅ 24-hour stability test successful  
