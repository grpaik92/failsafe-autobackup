<#
.SYNOPSIS
    Initializes the backup folder structure for Failsafe AutoBackup

.DESCRIPTION
    Creates the default backup folder in the user's Documents directory,
    sets up the necessary subfolder structure, and configures permissions.

.PARAMETER BackupPath
    Optional custom backup path. If not specified, uses Documents\FailsafeAutoBackup

.EXAMPLE
    .\Initialize-BackupFolder.ps1
    Creates backup folder in default location (Documents\FailsafeAutoBackup)

.EXAMPLE
    .\Initialize-BackupFolder.ps1 -BackupPath "C:\CustomBackups"
    Creates backup folder in specified location
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$BackupPath
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Determine backup path
if ([string]::IsNullOrEmpty($BackupPath)) {
    $documentsPath = [Environment]::GetFolderPath("MyDocuments")
    $BackupPath = Join-Path $documentsPath "FailsafeAutoBackup"
}

Write-Host "Initializing Failsafe AutoBackup folder structure..." -ForegroundColor Cyan
Write-Host "Target path: $BackupPath" -ForegroundColor Gray

# Create main backup folder
try {
    if (-not (Test-Path $BackupPath)) {
        New-Item -Path $BackupPath -ItemType Directory -Force | Out-Null
        Write-Host "[✓] Created main backup folder" -ForegroundColor Green
    } else {
        Write-Host "[i] Main backup folder already exists" -ForegroundColor Yellow
    }
} catch {
    Write-Host "[✗] Failed to create main backup folder: $_" -ForegroundColor Red
    exit 1
}

# Create subfolder structure
$subfolders = @(
    "Word",
    "PDF",
    "Logs",
    "Temp"
)

foreach ($folder in $subfolders) {
    $folderPath = Join-Path $BackupPath $folder
    try {
        if (-not (Test-Path $folderPath)) {
            New-Item -Path $folderPath -ItemType Directory -Force | Out-Null
            Write-Host "[✓] Created subfolder: $folder" -ForegroundColor Green
        }
    } catch {
        Write-Host "[✗] Failed to create subfolder $folder: $_" -ForegroundColor Red
    }
}

# Create configuration file with default settings
$configPath = Join-Path $BackupPath "config.json"
if (-not (Test-Path $configPath)) {
    $config = @{
        BackupPath = $BackupPath
        BackupIntervalMinutes = 2
        MaxVersions = 10
        EnableWord = $true
        EnablePDF = $true
        CreatedAt = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    } | ConvertTo-Json -Depth 10

    try {
        $config | Out-File -FilePath $configPath -Encoding UTF8
        Write-Host "[✓] Created configuration file" -ForegroundColor Green
    } catch {
        Write-Host "[✗] Failed to create configuration file: $_" -ForegroundColor Red
    }
}

# Create a README file in the backup folder
$readmePath = Join-Path $BackupPath "README.txt"
if (-not (Test-Path $readmePath)) {
    $readmeContent = @"
Failsafe AutoBackup - Backup Folder
=====================================

This folder contains automatic backups of your documents created by Failsafe AutoBackup.

Folder Structure:
-----------------
- Word\     : Microsoft Word document backups
- PDF\      : PDF document backups
- Logs\     : Application logs
- Temp\     : Temporary files (can be safely deleted)

Backup Naming Convention:
-------------------------
Backups are named with timestamps: DocumentName_YYYYMMDD_HHMMSS.ext

Configuration:
--------------
Edit config.json to customize backup settings.

Support:
--------
For support, visit: https://github.com/grpaik92/failsafe-autobackup
"@

    try {
        $readmeContent | Out-File -FilePath $readmePath -Encoding UTF8
        Write-Host "[✓] Created README file" -ForegroundColor Green
    } catch {
        Write-Host "[✗] Failed to create README file: $_" -ForegroundColor Red
    }
}

# Set folder attributes (hidden for Temp folder)
try {
    $tempFolder = Join-Path $BackupPath "Temp"
    if (Test-Path $tempFolder) {
        (Get-Item $tempFolder).Attributes = "Directory, Hidden"
    }
} catch {
    # Ignore errors setting attributes
}

Write-Host ""
Write-Host "Initialization complete!" -ForegroundColor Green
Write-Host "Backup folder: $BackupPath" -ForegroundColor Cyan
Write-Host ""

# Return the backup path for use in other scripts
return $BackupPath
