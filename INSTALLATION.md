# Windows Service Installation and Configuration

## Prerequisites

- Windows 10/11 or Windows Server 2019+
- .NET 8.0 Runtime installed
- Administrator privileges

## Step 1: Build the Service

```powershell
# Navigate to repository root
cd failsafe-autobackup

# Build the service in Release mode
dotnet publish src/FailsafeAutoBackup.Service -c Release -o C:\FailsafeAutoBackup\Service
```

## Step 2: Install Windows Service

Open PowerShell as Administrator and run:

```powershell
# Create the service
sc.exe create "FailsafeAutoBackup Service" `
    binPath= "C:\FailsafeAutoBackup\Service\FailsafeAutoBackup.Service.exe" `
    start= auto `
    DisplayName= "Failsafe AutoBackup Service" `
    description= "Automatic backup service for Microsoft Word and PDF documents"

# Configure service recovery
sc.exe failure "FailsafeAutoBackup Service" reset= 86400 actions= restart/60000/restart/60000/restart/60000

# Start the service
sc.exe start "FailsafeAutoBackup Service"

# Verify service is running
sc.exe query "FailsafeAutoBackup Service"
```

## Step 3: Configure Service Recovery

The service is configured to automatically restart on failure:
- **First failure**: Restart after 1 minute
- **Second failure**: Restart after 1 minute
- **Subsequent failures**: Restart after 1 minute
- **Reset failure count**: After 24 hours (86400 seconds)

To modify recovery settings:

```powershell
sc.exe failure "FailsafeAutoBackup Service" reset= 86400 actions= restart/30000/restart/60000/restart/120000
```

## Step 4: Set Up Watchdog (Optional but Recommended)

### Create Watchdog Script

Save this as `C:\FailsafeAutoBackup\watchdog.ps1`:

```powershell
# Failsafe AutoBackup Service Watchdog
# Monitors service health and restarts if necessary

$ServiceName = "FailsafeAutoBackup Service"
$LogPath = "C:\ProgramData\FailsafeAutoBackup\Logs\watchdog.log"
$HeartbeatFile = "C:\ProgramData\FailsafeAutoBackup\heartbeat.txt"
$MaxAge = 10  # minutes

function Write-Log {
    param($Message)
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$Timestamp - $Message" | Out-File -FilePath $LogPath -Append
}

# Ensure log directory exists
$LogDir = Split-Path $LogPath
if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
}

Write-Log "Watchdog: Checking service health..."

# Check if service is running
$Service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -eq $Service) {
    Write-Log "ERROR: Service not found!"
    exit 1
}

if ($Service.Status -ne 'Running') {
    Write-Log "WARNING: Service is not running. Status: $($Service.Status)"
    Write-Log "Attempting to start service..."
    
    try {
        Start-Service -Name $ServiceName
        Write-Log "Service started successfully"
    }
    catch {
        Write-Log "ERROR: Failed to start service: $_"
        exit 1
    }
}
else {
    # Check heartbeat file
    if (Test-Path $HeartbeatFile) {
        $HeartbeatAge = (Get-Date) - (Get-Item $HeartbeatFile).LastWriteTime
        
        if ($HeartbeatAge.TotalMinutes -gt $MaxAge) {
            Write-Log "WARNING: Heartbeat is stale (${HeartbeatAge.TotalMinutes} minutes old)"
            Write-Log "Restarting service..."
            
            try {
                Restart-Service -Name $ServiceName -Force
                Write-Log "Service restarted successfully"
            }
            catch {
                Write-Log "ERROR: Failed to restart service: $_"
                exit 1
            }
        }
        else {
            Write-Log "Service is healthy (heartbeat age: $([math]::Round($HeartbeatAge.TotalMinutes, 2)) minutes)"
        }
    }
    else {
        Write-Log "WARNING: Heartbeat file not found"
    }
}

Write-Log "Watchdog: Check complete"
```

### Create Scheduled Task for Watchdog

```powershell
# Run as Administrator

$Action = New-ScheduledTaskAction -Execute 'PowerShell.exe' -Argument '-ExecutionPolicy Bypass -File "C:\FailsafeAutoBackup\watchdog.ps1"'
$Trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes 5) -RepetitionDuration ([TimeSpan]::MaxValue)
$Principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
$Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable

Register-ScheduledTask -TaskName "FailsafeAutoBackup Watchdog" -Action $Action -Trigger $Trigger -Principal $Principal -Settings $Settings -Description "Monitors Failsafe AutoBackup Service health and restarts if necessary"

Write-Host "Watchdog scheduled task created successfully"
```

## Step 5: Configure Firewall (if Backend API is used)

```powershell
# Allow Backend API through firewall
New-NetFirewallRule -DisplayName "Failsafe AutoBackup API" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5000,5001
```

## Step 6: Install Tray Application

### Option 1: Manual Installation

1. Build the tray app:
   ```powershell
   dotnet publish src/FailsafeAutoBackup.TrayApp -c Release -o C:\FailsafeAutoBackup\TrayApp
   ```

2. Create shortcut in Startup folder:
   ```powershell
   $WshShell = New-Object -ComObject WScript.Shell
   $Shortcut = $WshShell.CreateShortcut("$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\FailsafeAutoBackup.lnk")
   $Shortcut.TargetPath = "C:\FailsafeAutoBackup\TrayApp\FailsafeAutoBackup.TrayApp.exe"
   $Shortcut.WorkingDirectory = "C:\FailsafeAutoBackup\TrayApp"
   $Shortcut.Description = "Failsafe AutoBackup Tray Application"
   $Shortcut.Save()
   
   Write-Host "Startup shortcut created"
   ```

### Option 2: Registry Key (Alternative)

```powershell
$RegPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$AppPath = "C:\FailsafeAutoBackup\TrayApp\FailsafeAutoBackup.TrayApp.exe"

Set-ItemProperty -Path $RegPath -Name "FailsafeAutoBackup" -Value $AppPath
Write-Host "Registry startup entry created"
```

## Service Management Commands

### Start Service
```powershell
sc.exe start "FailsafeAutoBackup Service"
# or
Start-Service "FailsafeAutoBackup Service"
```

### Stop Service
```powershell
sc.exe stop "FailsafeAutoBackup Service"
# or
Stop-Service "FailsafeAutoBackup Service"
```

### Restart Service
```powershell
Restart-Service "FailsafeAutoBackup Service"
```

### Query Service Status
```powershell
sc.exe query "FailsafeAutoBackup Service"
# or
Get-Service "FailsafeAutoBackup Service"
```

### View Service Configuration
```powershell
sc.exe qc "FailsafeAutoBackup Service"
```

### Delete Service (Uninstall)
```powershell
# Stop service first
sc.exe stop "FailsafeAutoBackup Service"

# Delete service
sc.exe delete "FailsafeAutoBackup Service"
```

## Uninstallation

### Remove Service

```powershell
# Run as Administrator

# Stop service
sc.exe stop "FailsafeAutoBackup Service"

# Delete service
sc.exe delete "FailsafeAutoBackup Service"

# Remove scheduled task
Unregister-ScheduledTask -TaskName "FailsafeAutoBackup Watchdog" -Confirm:$false

# Remove startup entry
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "FailsafeAutoBackup" -ErrorAction SilentlyContinue

# Remove files (optional)
Remove-Item -Path "C:\FailsafeAutoBackup" -Recurse -Force
Remove-Item -Path "C:\ProgramData\FailsafeAutoBackup" -Recurse -Force

Write-Host "Failsafe AutoBackup uninstalled successfully"
```

## Troubleshooting

### Service Won't Start

1. Check Windows Event Viewer:
   ```powershell
   Get-EventLog -LogName Application -Source "FailsafeAutoBackup*" -Newest 20
   ```

2. Check service logs:
   ```powershell
   Get-Content "C:\ProgramData\FailsafeAutoBackup\Logs\service-*.log" -Tail 50
   ```

3. Verify .NET Runtime installed:
   ```powershell
   dotnet --info
   ```

### Service Crashes Repeatedly

1. Check recovery settings:
   ```powershell
   sc.exe qfailure "FailsafeAutoBackup Service"
   ```

2. Review crash logs
3. Adjust recovery intervals
4. Check for conflicts with antivirus software

### Permissions Issues

1. Run service as different account:
   ```powershell
   sc.exe config "FailsafeAutoBackup Service" obj= "NT AUTHORITY\NetworkService"
   ```

2. Grant folder permissions:
   ```powershell
   icacls "C:\FailsafeAutoBackup" /grant "NT AUTHORITY\NetworkService:(OI)(CI)F"
   ```

### High Memory Usage

1. Monitor with Performance Monitor
2. Check for memory leaks in logs
3. Restart service periodically if needed

## Verification

After installation, verify everything works:

```powershell
# 1. Check service status
Get-Service "FailsafeAutoBackup Service"

# 2. Check service is running
if ((Get-Service "FailsafeAutoBackup Service").Status -eq 'Running') {
    Write-Host "✓ Service is running" -ForegroundColor Green
} else {
    Write-Host "✗ Service is not running" -ForegroundColor Red
}

# 3. Check logs
if (Test-Path "C:\ProgramData\FailsafeAutoBackup\Logs") {
    Write-Host "✓ Log directory exists" -ForegroundColor Green
} else {
    Write-Host "✗ Log directory missing" -ForegroundColor Red
}

# 4. Check watchdog task
if (Get-ScheduledTask -TaskName "FailsafeAutoBackup Watchdog" -ErrorAction SilentlyContinue) {
    Write-Host "✓ Watchdog task configured" -ForegroundColor Green
} else {
    Write-Host "! Watchdog task not configured (optional)" -ForegroundColor Yellow
}

Write-Host "`nInstallation verification complete!"
```

## Production Deployment Checklist

- [ ] .NET 8.0 Runtime installed
- [ ] Service installed and configured
- [ ] Service recovery policy configured
- [ ] Watchdog scheduled task created
- [ ] Tray app added to Startup
- [ ] Firewall rules configured (if needed)
- [ ] Backup folder permissions verified
- [ ] Logs directory created and writable
- [ ] Service running successfully
- [ ] Tray app connects to service
- [ ] Test backup cycle completes
- [ ] Monitor for 24 hours

## Support

For issues or questions:
- Check logs in `C:\ProgramData\FailsafeAutoBackup\Logs`
- Review Windows Event Viewer
- Contact: support@failsafeautobackup.com
