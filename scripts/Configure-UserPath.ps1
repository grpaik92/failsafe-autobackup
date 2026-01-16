<#
.SYNOPSIS
    Configures user-specific paths for Failsafe AutoBackup

.DESCRIPTION
    Creates and configures user-specific paths for backup storage, logs, and configuration.
    Stores the configuration in the user's AppData directory.

.PARAMETER BackupPath
    Custom backup path (optional)

.PARAMETER LogPath
    Custom log path (optional)

.EXAMPLE
    .\Configure-UserPath.ps1
    Uses default paths in user's Documents and AppData

.EXAMPLE
    .\Configure-UserPath.ps1 -BackupPath "D:\Backups" -LogPath "D:\Logs"
    Uses custom paths for backups and logs
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$BackupPath,
    
    [Parameter(Mandatory=$false)]
    [string]$LogPath
)

$ErrorActionPreference = "Stop"

Write-Host "Configuring user-specific paths for Failsafe AutoBackup..." -ForegroundColor Cyan

# Get standard Windows paths
$appDataPath = [Environment]::GetFolderPath("ApplicationData")
$localAppDataPath = [Environment]::GetFolderPath("LocalApplicationData")
$documentsPath = [Environment]::GetFolderPath("MyDocuments")

# Set default paths if not provided
if ([string]::IsNullOrEmpty($BackupPath)) {
    $BackupPath = Join-Path $documentsPath "FailsafeAutoBackup"
}

if ([string]::IsNullOrEmpty($LogPath)) {
    $LogPath = Join-Path $localAppDataPath "FailsafeAutoBackup\Logs"
}

# Configuration path
$configDir = Join-Path $appDataPath "FailsafeAutoBackup"
$configFile = Join-Path $configDir "user_config.json"

Write-Host ""
Write-Host "Paths:" -ForegroundColor Yellow
Write-Host "  Backup Path:  $BackupPath" -ForegroundColor Gray
Write-Host "  Log Path:     $LogPath" -ForegroundColor Gray
Write-Host "  Config Path:  $configFile" -ForegroundColor Gray
Write-Host ""

# Create directories
$pathsToCreate = @($BackupPath, $LogPath, $configDir)

foreach ($path in $pathsToCreate) {
    try {
        if (-not (Test-Path $path)) {
            New-Item -Path $path -ItemType Directory -Force | Out-Null
            Write-Host "[✓] Created directory: $path" -ForegroundColor Green
        } else {
            Write-Host "[i] Directory already exists: $path" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "[✗] Failed to create directory $path: $_" -ForegroundColor Red
        exit 1
    }
}

# Create user configuration
$userConfig = @{
    Version = "1.0"
    User = @{
        Username = $env:USERNAME
        MachineName = $env:COMPUTERNAME
        ConfiguredAt = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    }
    Paths = @{
        BackupPath = $BackupPath
        LogPath = $LogPath
        ConfigPath = $configDir
    }
    Settings = @{
        BackupIntervalMinutes = 2
        MaxVersions = 10
        EnableWord = $true
        EnablePDF = $true
        AutoStartWithWindows = $true
    }
}

# Save configuration
try {
    $userConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
    Write-Host "[✓] Configuration saved: $configFile" -ForegroundColor Green
} catch {
    Write-Host "[✗] Failed to save configuration: $_" -ForegroundColor Red
    exit 1
}

# Set environment variable for current session (optional)
[Environment]::SetEnvironmentVariable("FAILSAFE_BACKUP_PATH", $BackupPath, "User")
Write-Host "[✓] Environment variable set: FAILSAFE_BACKUP_PATH" -ForegroundColor Green

Write-Host ""
Write-Host "Configuration complete!" -ForegroundColor Green
Write-Host "Configuration file: $configFile" -ForegroundColor Cyan

# Return configuration object
return $userConfig
