<#
.SYNOPSIS
    Creates a desktop shortcut for Failsafe AutoBackup Tray Application

.DESCRIPTION
    Creates a desktop shortcut that launches the Failsafe AutoBackup Tray App.
    The shortcut can be customized with an icon and description.

.PARAMETER TrayAppPath
    Path to the Tray Application executable

.PARAMETER ShortcutName
    Name of the desktop shortcut (default: "Failsafe AutoBackup")

.PARAMETER IconPath
    Optional path to custom icon file (.ico)

.EXAMPLE
    .\Create-DesktopShortcut.ps1 -TrayAppPath "C:\Program Files\FailsafeAutoBackup\FailsafeAutoBackup.TrayApp.exe"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$TrayAppPath,
    
    [Parameter(Mandatory=$false)]
    [string]$ShortcutName = "Failsafe AutoBackup",
    
    [Parameter(Mandatory=$false)]
    [string]$IconPath
)

$ErrorActionPreference = "Stop"

Write-Host "Creating desktop shortcut for Failsafe AutoBackup..." -ForegroundColor Cyan

# Verify the tray app exists
if (-not (Test-Path $TrayAppPath)) {
    Write-Host "[✗] Tray application not found at: $TrayAppPath" -ForegroundColor Red
    exit 1
}

# Get desktop path
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "$ShortcutName.lnk"

try {
    # Create WScript Shell COM object
    $WshShell = New-Object -ComObject WScript.Shell
    
    # Create shortcut
    $Shortcut = $WshShell.CreateShortcut($shortcutPath)
    $Shortcut.TargetPath = $TrayAppPath
    $Shortcut.WorkingDirectory = Split-Path -Parent $TrayAppPath
    $Shortcut.Description = "Launch Failsafe AutoBackup - Automatic document backup service"
    $Shortcut.WindowStyle = 1  # Normal window
    
    # Set icon if provided
    if (-not [string]::IsNullOrEmpty($IconPath) -and (Test-Path $IconPath)) {
        $Shortcut.IconLocation = $IconPath
    } else {
        # Use the executable's icon
        $Shortcut.IconLocation = "$TrayAppPath,0"
    }
    
    # Save the shortcut
    $Shortcut.Save()
    
    Write-Host "[✓] Desktop shortcut created successfully" -ForegroundColor Green
    Write-Host "    Location: $shortcutPath" -ForegroundColor Gray
    
    # Release COM object
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($WshShell) | Out-Null
    
} catch {
    Write-Host "[✗] Failed to create desktop shortcut: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Shortcut creation complete!" -ForegroundColor Green
