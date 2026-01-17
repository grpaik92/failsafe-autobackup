# Failsafe AutoBackup - Installation Guide

> **Version**: 1.0.0  
> **Last Updated**: January 2025  
> **Platform**: Windows 10/11 (64-bit)

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Pre-Flight Check](#pre-flight-check)
3. [Installation Methods](#installation-methods)
   - [MSI Installer (Recommended)](#msi-installer-recommended)
   - [Manual Installation](#manual-installation-advanced)
4. [Post-Installation Verification](#post-installation-verification)
5. [Configuration](#configuration)
6. [Uninstallation](#uninstallation)
7. [Troubleshooting](#troubleshooting)
8. [Testing Plan](#testing-plan)
9. [Production Deployment](#production-deployment)

---

## Prerequisites

### System Requirements

| Component | Requirement |
|-----------|------------|
| **Operating System** | Windows 10 (1809+) or Windows 11 |
| **Architecture** | 64-bit (x64) |
| **.NET Runtime** | Not required (self-contained) |
| **Disk Space** | 150 MB (installation) + backup storage |
| **RAM** | 100 MB minimum |
| **Permissions** | Administrator (for installation) |

### Software Dependencies

- **None** - Application is self-contained and includes all dependencies
- Microsoft Word and/or Adobe Acrobat/Reader (for backup functionality)

### Network Requirements

- Internet connection for:
  - License validation
  - Subscription management
  - Optional: Cloud backup sync (future feature)

---

## Pre-Flight Check

Run this PowerShell script **before installation** to verify system readiness:

```powershell
# Pre-Flight-Check.ps1
# Validates system requirements before Failsafe AutoBackup installation

#Requires -RunAsAdministrator

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Failsafe AutoBackup - Pre-Flight Check" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

$issues = @()
$warnings = @()

# Check Windows Version
Write-Host "[1/8] Checking Windows version..." -ForegroundColor Yellow
$osInfo = Get-CimInstance Win32_OperatingSystem
$osVersion = [Version]$osInfo.Version
$buildNumber = $osInfo.BuildNumber

if ($osVersion.Major -ge 10) {
    if ($buildNumber -ge 17763) { # Windows 10 1809+
        Write-Host "  ✓ Windows version: $($osInfo.Caption) (Build $buildNumber)" -ForegroundColor Green
    } else {
        $issues += "Windows 10 build $buildNumber is too old. Minimum: 17763 (1809)"
    }
} else {
    $issues += "Windows version $($osVersion) is not supported. Minimum: Windows 10"
}

# Check Architecture
Write-Host "[2/8] Checking system architecture..." -ForegroundColor Yellow
if ($env:PROCESSOR_ARCHITECTURE -eq "AMD64") {
    Write-Host "  ✓ 64-bit architecture detected" -ForegroundColor Green
} else {
    $issues += "32-bit system detected. Only 64-bit is supported."
}

# Check Administrator Privileges
Write-Host "[3/8] Checking administrator privileges..." -ForegroundColor Yellow
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if ($isAdmin) {
    Write-Host "  ✓ Running with administrator privileges" -ForegroundColor Green
} else {
    $issues += "Not running as administrator. Installation requires admin rights."
}

# Check Disk Space
Write-Host "[4/8] Checking disk space..." -ForegroundColor Yellow
$drive = Get-PSDrive C
$freeSpaceGB = [math]::Round($drive.Free / 1GB, 2)
if ($freeSpaceGB -ge 1) {
    Write-Host "  ✓ Free space on C: drive: $freeSpaceGB GB" -ForegroundColor Green
} else {
    $issues += "Insufficient disk space on C: drive. Available: $freeSpaceGB GB, Required: 1 GB minimum"
}

# Check if service already exists
Write-Host "[5/8] Checking for existing installation..." -ForegroundColor Yellow
$existingService = Get-Service -Name "FailsafeAutoBackupService" -ErrorAction SilentlyContinue
if ($existingService) {
    $warnings += "Service 'FailsafeAutoBackupService' already exists. Uninstall existing version first."
    Write-Host "  ⚠ Existing service found: $($existingService.Status)" -ForegroundColor Yellow
} else {
    Write-Host "  ✓ No existing service found" -ForegroundColor Green
}

# Check installation directory
Write-Host "[6/8] Checking installation directory..." -ForegroundColor Yellow
$installDir = "C:\Program Files\FailsafeAutoBackup"
if (Test-Path $installDir) {
    $warnings += "Installation directory already exists: $installDir"
    Write-Host "  ⚠ Directory exists: $installDir" -ForegroundColor Yellow
} else {
    Write-Host "  ✓ Installation directory available" -ForegroundColor Green
}

# Check Microsoft Word
Write-Host "[7/8] Checking for Microsoft Word..." -ForegroundColor Yellow
$wordPath = "HKLM:\SOFTWARE\Microsoft\Office\ClickToRun\REGISTRY\MACHINE\Software\Microsoft\Office"
$wordInstalled = Test-Path $wordPath
if ($wordInstalled) {
    Write-Host "  ✓ Microsoft Word detected" -ForegroundColor Green
} else {
    $warnings += "Microsoft Word not detected. Install Word for backup functionality."
}

# Check Adobe Acrobat/Reader
Write-Host "[8/8] Checking for Adobe Acrobat/Reader..." -ForegroundColor Yellow
$adobePaths = @(
    "HKLM:\SOFTWARE\Adobe\Acrobat Reader",
    "HKLM:\SOFTWARE\Adobe\Adobe Acrobat"
)
$adobeInstalled = $false
foreach ($path in $adobePaths) {
    if (Test-Path $path) {
        $adobeInstalled = $true
        break
    }
}
if ($adobeInstalled) {
    Write-Host "  ✓ Adobe Acrobat/Reader detected" -ForegroundColor Green
} else {
    $warnings += "Adobe Acrobat/Reader not detected. Install for PDF backup functionality."
}

# Summary
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Pre-Flight Check Summary" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

if ($issues.Count -eq 0) {
    Write-Host "✓ All checks passed!" -ForegroundColor Green
    if ($warnings.Count -gt 0) {
        Write-Host ""
        Write-Host "Warnings:" -ForegroundColor Yellow
        foreach ($warning in $warnings) {
            Write-Host "  ⚠ $warning" -ForegroundColor Yellow
        }
    }
    Write-Host ""
    Write-Host "System is ready for installation." -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ Issues found:" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host "  ✗ $issue" -ForegroundColor Red
    }
    if ($warnings.Count -gt 0) {
        Write-Host ""
        Write-Host "Warnings:" -ForegroundColor Yellow
        foreach ($warning in $warnings) {
            Write-Host "  ⚠ $warning" -ForegroundColor Yellow
        }
    }
    Write-Host ""
    Write-Host "Please resolve issues before installation." -ForegroundColor Red
    exit 1
}
```

**Usage**:
```powershell
# Save script as Pre-Flight-Check.ps1
# Run in PowerShell as Administrator
.\Pre-Flight-Check.ps1
```

---

## Installation Methods

### MSI Installer (Recommended)

The MSI installer automates the entire installation process including service registration, shortcut creation, and configuration.

#### GUI Installation

1. **Download the Installer**
   ```
   FailsafeAutoBackup-1.0.0-x64.msi
   ```

2. **Run the Installer**
   - Double-click the MSI file
   - Click "Next" on the welcome screen
   - Accept the license agreement
   - Choose installation directory (default: `C:\Program Files\FailsafeAutoBackup`)
   - Select components:
     - ✓ Core Application (required)
     - ✓ Desktop Shortcut (optional)
   - Click "Install"
   - Wait for installation to complete
   - Click "Finish"

3. **What Gets Installed**
   - **Service**: `C:\Program Files\FailsafeAutoBackup\Service\`
   - **Tray App**: `C:\Program Files\FailsafeAutoBackup\TrayApp\`
   - **Logs**: `C:\ProgramData\FailsafeAutoBackup\Logs\`
   - **Start Menu**: Shortcut in Start Menu
   - **Desktop**: Shortcut (if selected)
   - **Auto-Start**: Registry entry for auto-start

4. **What Gets Configured**
   - Windows Service registered as `FailsafeAutoBackupService`
   - Service set to auto-start on boot
   - Service recovery policy: Restart on failure (3 attempts, 1-minute delay)
   - Tray app set to start with Windows login

#### Silent Installation

For automated deployment or enterprise environments:

```powershell
# Silent install with default options
msiexec /i FailsafeAutoBackup-1.0.0-x64.msi /quiet /norestart

# Silent install with custom directory
msiexec /i FailsafeAutoBackup-1.0.0-x64.msi /quiet /norestart INSTALLFOLDER="D:\FailsafeAutoBackup"

# Silent install without desktop shortcut
msiexec /i FailsafeAutoBackup-1.0.0-x64.msi /quiet /norestart ADDLOCAL=MainApplication

# Silent install with logging
msiexec /i FailsafeAutoBackup-1.0.0-x64.msi /quiet /norestart /l*v install.log
```

**MSI Properties**:
- `INSTALLFOLDER` - Installation directory (default: `C:\Program Files\FailsafeAutoBackup`)
- `ADDLOCAL` - Features to install (MainApplication, DesktopShortcutFeature)

**Exit Codes**:
- `0` - Success
- `1603` - Fatal error during installation
- `1618` - Another installation is in progress
- `3010` - Reboot required

---

### Manual Installation (Advanced)

For advanced users or custom deployments:

#### Step 1: Download and Extract

```powershell
# Create installation directory
New-Item -Path "C:\Program Files\FailsafeAutoBackup" -ItemType Directory -Force

# Extract release package
Expand-Archive -Path "FailsafeAutoBackup-1.0.0-x64.zip" -DestinationPath "C:\Program Files\FailsafeAutoBackup"
```

#### Step 2: Install Windows Service

```powershell
# Navigate to service directory
cd "C:\Program Files\FailsafeAutoBackup\Service"

# Create Windows Service
sc.exe create "FailsafeAutoBackupService" `
    binPath= "C:\Program Files\FailsafeAutoBackup\Service\FailsafeAutoBackup.Service.exe" `
    DisplayName= "Failsafe AutoBackup Service" `
    start= auto `
    obj= LocalSystem

# Configure service description
sc.exe description "FailsafeAutoBackupService" "Automatic backup service for Microsoft Word and PDF documents"

# Configure service recovery policy
sc.exe failure "FailsafeAutoBackupService" `
    reset= 86400 `
    actions= restart/60000/restart/60000/restart/60000

# Start the service
sc.exe start "FailsafeAutoBackupService"
```

#### Step 3: Create Directories

```powershell
# Create logs directory
New-Item -Path "C:\ProgramData\FailsafeAutoBackup\Logs" -ItemType Directory -Force

# Create backups directory (optional, can be configured later)
New-Item -Path "$env:USERPROFILE\FailsafeAutoBackup\Backups" -ItemType Directory -Force
```

#### Step 4: Create Shortcuts

```powershell
# Start Menu shortcut
$WshShell = New-Object -ComObject WScript.Shell
$StartMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Failsafe AutoBackup.lnk"
$Shortcut = $WshShell.CreateShortcut($StartMenuPath)
$Shortcut.TargetPath = "C:\Program Files\FailsafeAutoBackup\TrayApp\FailsafeAutoBackup.TrayApp.exe"
$Shortcut.WorkingDirectory = "C:\Program Files\FailsafeAutoBackup\TrayApp"
$Shortcut.Description = "Automatic document backup for Word and PDF"
$Shortcut.Save()

# Desktop shortcut (optional)
$DesktopPath = "$env:USERPROFILE\Desktop\Failsafe AutoBackup.lnk"
$Shortcut = $WshShell.CreateShortcut($DesktopPath)
$Shortcut.TargetPath = "C:\Program Files\FailsafeAutoBackup\TrayApp\FailsafeAutoBackup.TrayApp.exe"
$Shortcut.WorkingDirectory = "C:\Program Files\FailsafeAutoBackup\TrayApp"
$Shortcut.Description = "Automatic document backup for Word and PDF"
$Shortcut.Save()
```

#### Step 5: Configure Auto-Start

```powershell
# Add registry entry for auto-start
Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" `
    -Name "FailsafeAutoBackup" `
    -Value "C:\Program Files\FailsafeAutoBackup\TrayApp\FailsafeAutoBackup.TrayApp.exe"
```

#### Step 6: Launch Tray App

```powershell
# Start tray application
Start-Process "C:\Program Files\FailsafeAutoBackup\TrayApp\FailsafeAutoBackup.TrayApp.exe"
```

---

## Post-Installation Verification

Run this PowerShell script to verify successful installation:

```powershell
# Post-Install-Verification.ps1
# Verifies Failsafe AutoBackup installation

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Failsafe AutoBackup - Installation Verification" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

$allPassed = $true

# Check service installation
Write-Host "[1/8] Verifying Windows Service..." -ForegroundColor Yellow
$service = Get-Service -Name "FailsafeAutoBackupService" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "  ✓ Service found: $($service.DisplayName)" -ForegroundColor Green
    Write-Host "    Status: $($service.Status)" -ForegroundColor Gray
    Write-Host "    Start Type: $($service.StartType)" -ForegroundColor Gray
    
    if ($service.Status -ne "Running") {
        Write-Host "  ⚠ Service is not running" -ForegroundColor Yellow
        $allPassed = $false
    }
} else {
    Write-Host "  ✗ Service not found" -ForegroundColor Red
    $allPassed = $false
}

# Check service recovery policy
Write-Host "[2/8] Verifying service recovery policy..." -ForegroundColor Yellow
$recoveryInfo = sc.exe qfailure "FailsafeAutoBackupService" 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ Recovery policy configured" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Could not verify recovery policy" -ForegroundColor Yellow
}

# Check installation files
Write-Host "[3/8] Verifying installation files..." -ForegroundColor Yellow
$requiredFiles = @(
    "C:\Program Files\FailsafeAutoBackup\Service\FailsafeAutoBackup.Service.exe",
    "C:\Program Files\FailsafeAutoBackup\TrayApp\FailsafeAutoBackup.TrayApp.exe"
)
$filesMissing = $false
foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Write-Host "  ✓ Found: $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Missing: $file" -ForegroundColor Red
        $filesMissing = $true
        $allPassed = $false
    }
}

# Check logs directory
Write-Host "[4/8] Verifying logs directory..." -ForegroundColor Yellow
$logsDir = "C:\ProgramData\FailsafeAutoBackup\Logs"
if (Test-Path $logsDir) {
    Write-Host "  ✓ Logs directory exists: $logsDir" -ForegroundColor Green
} else {
    Write-Host "  ✗ Logs directory not found" -ForegroundColor Red
    $allPassed = $false
}

# Check Start Menu shortcut
Write-Host "[5/8] Verifying Start Menu shortcut..." -ForegroundColor Yellow
$startMenuShortcut = Get-ChildItem "$env:APPDATA\Microsoft\Windows\Start Menu\Programs" -Recurse -Filter "*Failsafe*.lnk" -ErrorAction SilentlyContinue
if ($startMenuShortcut) {
    Write-Host "  ✓ Start Menu shortcut found" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Start Menu shortcut not found" -ForegroundColor Yellow
}

# Check auto-start registry
Write-Host "[6/8] Verifying auto-start configuration..." -ForegroundColor Yellow
$autoStart = Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "FailsafeAutoBackup" -ErrorAction SilentlyContinue
if ($autoStart) {
    Write-Host "  ✓ Auto-start registry entry found" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Auto-start registry entry not found" -ForegroundColor Yellow
}

# Check if tray app is running
Write-Host "[7/8] Verifying tray application..." -ForegroundColor Yellow
$trayProcess = Get-Process -Name "FailsafeAutoBackup.TrayApp" -ErrorAction SilentlyContinue
if ($trayProcess) {
    Write-Host "  ✓ Tray application is running (PID: $($trayProcess.Id))" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Tray application is not running" -ForegroundColor Yellow
    Write-Host "    You can start it from the Start Menu" -ForegroundColor Gray
}

# Check service logs
Write-Host "[8/8] Checking service logs..." -ForegroundColor Yellow
$todayLog = Get-ChildItem $logsDir -Filter "service-*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($todayLog) {
    Write-Host "  ✓ Log file found: $($todayLog.Name)" -ForegroundColor Green
    Write-Host "    Size: $([math]::Round($todayLog.Length / 1KB, 2)) KB" -ForegroundColor Gray
    Write-Host "    Last modified: $($todayLog.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "  ⚠ No log files found (service may not have started yet)" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Verification Summary" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

if ($allPassed) {
    Write-Host "✓ Installation verified successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Launch the Tray App from Start Menu" -ForegroundColor White
    Write-Host "2. Configure backup settings" -ForegroundColor White
    Write-Host "3. Open Word/PDF document to test backup" -ForegroundColor White
    exit 0
} else {
    Write-Host "✗ Verification failed. Please check the errors above." -ForegroundColor Red
    exit 1
}
```

**Usage**:
```powershell
.\Post-Install-Verification.ps1
```

---

## Configuration

### Initial Setup

1. **Launch Tray Application**
   - Click Start Menu → "Failsafe AutoBackup"
   - Or double-click desktop shortcut
   - App minimizes to system tray (check notification area)

2. **Configure Backup Settings**
   - Right-click tray icon → "Settings"
   - Configure options:

     | Setting | Description | Default |
     |---------|-------------|---------|
     | **Backup Interval** | Minutes between backups | 2 minutes |
     | **Backup Folder** | Destination for backups | `%USERPROFILE%\FailsafeAutoBackup\Backups` |
     | **Enable Word Backup** | Backup Word documents | ✓ Enabled |
     | **Enable PDF Backup** | Backup PDF documents | ✓ Enabled |
     | **Max Backup Versions** | Number of versions to keep | 10 |
     | **Create Desktop Shortcut** | Shortcut to backup folder | ✓ Enabled |

3. **Save Configuration**
   - Click "Save" to apply settings
   - Service automatically restarts with new configuration

### Advanced Configuration

Edit configuration file manually (requires service restart):

**Location**: `C:\Program Files\FailsafeAutoBackup\Service\appsettings.json`

```json
{
  "ServiceConfiguration": {
    "BackupIntervalMinutes": 2,
    "BackupFolderPath": "",
    "EnableWordBackup": true,
    "EnablePdfBackup": true,
    "MaxBackupVersions": 10,
    "ComTimeoutSeconds": 30,
    "CreateDesktopShortcut": true,
    "LogLevel": "Information"
  }
}
```

**Restart service after changes**:
```powershell
Restart-Service -Name "FailsafeAutoBackupService"
```

### Backup Folder Structure

```
%USERPROFILE%\FailsafeAutoBackup\Backups\
├── Document1_20250115_143022.docx
├── Document1_20250115_143222.docx
├── Document1_20250115_143422.docx
├── Report_20250115_150000.pdf
└── Report_20250115_150200.pdf
```

**Naming Convention**: `{OriginalName}_{YYYYMMDD}_{HHMMSS}.{ext}`

---

## Uninstallation

### MSI Uninstall (Recommended)

#### GUI Method

1. Open **Settings** → **Apps** → **Installed apps**
2. Search for "Failsafe AutoBackup"
3. Click **...** (three dots) → **Uninstall**
4. Confirm uninstallation
5. Wait for completion

#### Command Line Method

```powershell
# Find product code
$app = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "*Failsafe*" }
$app.IdentityingNumber

# Uninstall silently
msiexec /x {PRODUCT-CODE-GUID} /quiet /norestart

# OR uninstall with UI
msiexec /x {PRODUCT-CODE-GUID} /passive
```

### Manual Uninstall

```powershell
# Uninstall-FailsafeAutoBackup.ps1
# Complete manual uninstallation script

#Requires -RunAsAdministrator

Write-Host "Uninstalling Failsafe AutoBackup..." -ForegroundColor Yellow

# Stop and remove service
Write-Host "Stopping service..." -ForegroundColor Yellow
Stop-Service -Name "FailsafeAutoBackupService" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Write-Host "Removing service..." -ForegroundColor Yellow
sc.exe delete "FailsafeAutoBackupService"

# Stop tray app
Write-Host "Stopping tray application..." -ForegroundColor Yellow
Get-Process -Name "FailsafeAutoBackup.TrayApp" -ErrorAction SilentlyContinue | Stop-Process -Force

# Remove installation directory
Write-Host "Removing installation files..." -ForegroundColor Yellow
Remove-Item -Path "C:\Program Files\FailsafeAutoBackup" -Recurse -Force -ErrorAction SilentlyContinue

# Remove logs (optional - comment out to keep logs)
Write-Host "Removing logs..." -ForegroundColor Yellow
Remove-Item -Path "C:\ProgramData\FailsafeAutoBackup" -Recurse -Force -ErrorAction SilentlyContinue

# Remove shortcuts
Write-Host "Removing shortcuts..." -ForegroundColor Yellow
Remove-Item -Path "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Failsafe AutoBackup.lnk" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$env:USERPROFILE\Desktop\Failsafe AutoBackup.lnk" -Force -ErrorAction SilentlyContinue

# Remove auto-start registry entry
Write-Host "Removing auto-start entry..." -ForegroundColor Yellow
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "FailsafeAutoBackup" -ErrorAction SilentlyContinue

# Remove registry keys
Write-Host "Removing registry entries..." -ForegroundColor Yellow
Remove-Item -Path "HKCU:\Software\FailsafeAutoBackup" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "✓ Uninstallation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Note: Backup files were NOT deleted." -ForegroundColor Yellow
Write-Host "Location: $env:USERPROFILE\FailsafeAutoBackup\Backups" -ForegroundColor Gray
```

**Note**: Backup files are preserved by default. Delete manually if no longer needed.

---

## Building the Installer

This section is for developers who need to build the MSI installer from source.

### Prerequisites for Building

- Windows 10/11 (64-bit)
- .NET 8.0 SDK
- WiX Toolset v4.0.5 or later
- PowerShell 7.0+

### Build Process

#### Method 1: Using GitHub Actions (Recommended)

The GitHub Actions workflow automatically builds the installer on every push to the `main` branch:

1. Push changes to the repository
2. GitHub Actions will:
   - Build the .NET projects
   - Publish the Service and Tray App as self-contained executables
   - Install WiX Toolset v4.0.5
   - Install WiX UI Extension
   - Build the MSI installer
   - Upload artifacts (including the MSI)

3. Download the `msi-installer` artifact from the workflow run

#### Method 2: Local Build

To build the installer locally on your Windows machine:

```powershell
# Step 1: Install .NET SDK 8.0
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0

# Step 2: Install WiX Toolset v4
dotnet tool install --global wix --version 4.0.5

# Step 3: Clone the repository
git clone https://github.com/grpaik92/failsafe-autobackup.git
cd failsafe-autobackup

# Step 4: Build the solution
dotnet build --configuration Release

# Step 5: Publish the Service (self-contained)
dotnet publish src/FailsafeAutoBackup.Service/FailsafeAutoBackup.Service.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o publish/service

# Step 6: Publish the Tray App (self-contained)
dotnet publish src/FailsafeAutoBackup.TrayApp/FailsafeAutoBackup.TrayApp.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o publish/trayapp

# Step 7: Install WiX UI Extension
wix extension add WixToolset.UI.wixext

# Step 8: Build the MSI installer
cd installer/wix
wix build Product.wxs -ext WixToolset.UI.wixext/4.0.5 -arch x64 -out ../../FailsafeAutoBackup.msi

# Step 9: Verify the MSI was created
cd ../..
if (Test-Path "FailsafeAutoBackup.msi") {
    Write-Host "✓ MSI installer built successfully!" -ForegroundColor Green
    $msi = Get-Item "FailsafeAutoBackup.msi"
    Write-Host "  File: $($msi.FullName)" -ForegroundColor Gray
    Write-Host "  Size: $([math]::Round($msi.Length / 1MB, 2)) MB" -ForegroundColor Gray
} else {
    Write-Host "✗ MSI installer build failed" -ForegroundColor Red
}
```

### Common Build Issues

#### Issue: WiX Extension Not Found

**Error Message**:
```
error WIX0144: The extension 'WixToolset.UI.wixext' could not be found.
```

**Solution**:
```powershell
# The WiX UI extension version must match the WiX version (4.0.5)
# Use the version-specific extension reference in the build command:
wix build Product.wxs -ext WixToolset.UI.wixext/4.0.5 -arch x64 -out ../../FailsafeAutoBackup.msi

# Alternatively, install the extension explicitly (may not work in all cases):
wix extension add WixToolset.UI.wixext

# Verify the extension is installed
wix extension list
```

#### Issue: Published Files Not Found

**Error Message**:
```
error LGHT0103: The system cannot find the file '..\..\publish\service\FailsafeAutoBackup.Service.exe'
```

**Solution**:
```powershell
# Ensure you've published the applications first
# Run steps 5 and 6 from the build process above

# Verify the files exist
Test-Path "publish/service/FailsafeAutoBackup.Service.exe"
Test-Path "publish/trayapp/FailsafeAutoBackup.TrayApp.exe"
```

#### Issue: License.rtf Not Found

**Error Message**:
```
error LGHT0103: The system cannot find the file 'License.rtf'
```

**Solution**:
```powershell
# Ensure you're in the installer/wix directory when building
cd installer/wix
wix build Product.wxs -ext WixToolset.UI.wixext/4.0.5 -arch x64 -out ../../FailsafeAutoBackup.msi
```

### Build Script

For convenience, you can create a `build-installer.ps1` script:

```powershell
# build-installer.ps1
# Complete build script for the MSI installer

param(
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "FailsafeAutoBackup.msi"
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Building Failsafe AutoBackup MSI Installer" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean previous builds
Write-Host "[1/9] Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "publish") {
    Remove-Item -Path "publish" -Recurse -Force
}
if (Test-Path $OutputPath) {
    Remove-Item -Path $OutputPath -Force
}

# Step 2: Restore dependencies
Write-Host "[2/9] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore

# Step 3: Build solution
Write-Host "[3/9] Building solution..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore

# Step 4: Run tests
Write-Host "[4/9] Running tests..." -ForegroundColor Yellow
dotnet test --configuration $Configuration --no-build --verbosity minimal

# Step 5: Publish Service
Write-Host "[5/9] Publishing Windows Service..." -ForegroundColor Yellow
dotnet publish src/FailsafeAutoBackup.Service/FailsafeAutoBackup.Service.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o publish/service

# Step 6: Publish Tray App
Write-Host "[6/9] Publishing Tray Application..." -ForegroundColor Yellow
dotnet publish src/FailsafeAutoBackup.TrayApp/FailsafeAutoBackup.TrayApp.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o publish/trayapp

# Step 7: Install WiX (if not already installed)
Write-Host "[7/9] Checking WiX installation..." -ForegroundColor Yellow
$wixInstalled = $null -ne (Get-Command "wix" -ErrorAction SilentlyContinue)
if (-not $wixInstalled) {
    Write-Host "  Installing WiX Toolset..." -ForegroundColor Gray
    dotnet tool install --global wix --version 4.0.5
}

# Step 8: Install WiX UI Extension
Write-Host "[8/9] Installing WiX UI Extension..." -ForegroundColor Yellow
wix extension add WixToolset.UI.wixext

# Step 9: Build MSI
Write-Host "[9/9] Building MSI installer..." -ForegroundColor Yellow
Set-Location "installer/wix"
wix build Product.wxs -ext WixToolset.UI.wixext/4.0.5 -arch x64 -out "../../$OutputPath"
Set-Location "../.."

# Verify
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Build Complete" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

if (Test-Path $OutputPath) {
    $msi = Get-Item $OutputPath
    Write-Host "✓ MSI installer built successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Installer Details:" -ForegroundColor Cyan
    Write-Host "  File: $($msi.FullName)" -ForegroundColor White
    Write-Host "  Size: $([math]::Round($msi.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "  Created: $($msi.CreationTime)" -ForegroundColor White
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Test the installer on a clean VM" -ForegroundColor White
    Write-Host "  2. Verify all components install correctly" -ForegroundColor White
    Write-Host "  3. Run the post-installation verification script" -ForegroundColor White
    exit 0
} else {
    Write-Host "✗ MSI installer build failed!" -ForegroundColor Red
    Write-Host "Check the output above for errors" -ForegroundColor Yellow
    exit 1
}
```

**Usage**:
```powershell
# Build with default settings
.\build-installer.ps1

# Build Debug version
.\build-installer.ps1 -Configuration Debug

# Specify custom output path
.\build-installer.ps1 -OutputPath "releases\FailsafeAutoBackup-v1.0.0.msi"
```

---

## Troubleshooting

### Common Issues

#### Issue 1: Service Won't Start

**Symptoms**:
- Service status shows "Stopped"
- Error in Windows Event Viewer
- Tray app shows "Service Disconnected"

**Solutions**:

```powershell
# Check service status
sc.exe query "FailsafeAutoBackupService"

# Check Windows Event Viewer
Get-EventLog -LogName Application -Source "FailsafeAutoBackup*" -Newest 10

# Check service logs
Get-Content "C:\ProgramData\FailsafeAutoBackup\Logs\service-$(Get-Date -Format 'yyyyMMdd').log" -Tail 50

# Try manual start
sc.exe start "FailsafeAutoBackupService"

# If still fails, reinstall
# 1. Uninstall completely
# 2. Reboot
# 3. Reinstall
```

#### Issue 2: Tray App Can't Connect to Service

**Symptoms**:
- "Cannot connect to service" message
- Tray icon shows red/disconnected status

**Solutions**:

```powershell
# Verify service is running
Get-Service -Name "FailsafeAutoBackupService"

# Restart service
Restart-Service -Name "FailsafeAutoBackupService"

# Check Named Pipe
[System.IO.Directory]::GetFiles("\\.\pipe\") | Select-String "FailsafeAutoBackup"

# Restart tray app
Get-Process -Name "FailsafeAutoBackup.TrayApp" | Stop-Process -Force
Start-Process "C:\Program Files\FailsafeAutoBackup\TrayApp\FailsafeAutoBackup.TrayApp.exe"
```

#### Issue 3: Backups Not Working

**Symptoms**:
- No new backup files created
- Service running but no activity

**Solutions**:

```powershell
# Check if documents are open
Get-Process -Name "WINWORD" -ErrorAction SilentlyContinue
Get-Process -Name "Acrobat" -ErrorAction SilentlyContinue

# Check backup folder permissions
$backupFolder = "$env:USERPROFILE\FailsafeAutoBackup\Backups"
$acl = Get-Acl $backupFolder
$acl.Access | Format-Table

# Check service logs for COM errors
Select-String -Path "C:\ProgramData\FailsafeAutoBackup\Logs\service-*.log" -Pattern "COM|Error|Exception" | Select-Object -Last 20

# Test backup manually (open Word document and wait 2 minutes)
```

#### Issue 4: High CPU/Memory Usage

**Symptoms**:
- Service using excessive resources
- System slowdown

**Solutions**:

```powershell
# Check resource usage
Get-Process -Name "FailsafeAutoBackup.Service" | Select-Object CPU,WS

# Increase backup interval
# Edit appsettings.json: BackupIntervalMinutes = 5 or higher
# Restart service

# Check for large number of open documents
# Close unnecessary documents
```

#### Issue 5: Permission Denied Errors

**Symptoms**:
- "Access denied" in logs
- Cannot backup to folder

**Solutions**:

```powershell
# Grant full control to backup folder
$backupFolder = "$env:USERPROFILE\FailsafeAutoBackup\Backups"
$acl = Get-Acl $backupFolder
$permission = "$env:USERNAME","FullControl","ContainerInherit,ObjectInherit","None","Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $backupFolder $acl

# Check Controlled Folder Access (Windows Security)
# Settings → Privacy & Security → Windows Security → Virus & threat protection
# → Ransomware protection → Controlled folder access
# Add FailsafeAutoBackup.Service.exe to allowed apps
```

#### Issue 6: MSI Installation Fails

**Symptoms**:
- Error code during installation
- Installation rolls back

**Solutions**:

```powershell
# Check MSI log
msiexec /i FailsafeAutoBackup-1.0.0-x64.msi /l*v install.log
notepad install.log

# Common error codes:
# 1603: Restart PC and try again
# 1618: Close Windows Installer and try again
# 2502/2503: Run as Administrator

# If all else fails, use manual installation
```

#### Issue 7: GitHub Actions Build Failure

**Symptoms**:
- Build workflow fails with exit code 1
- Error message: "The extension 'WixToolset.UI.wixext' could not be found"
- MSI artifact not uploaded

**Solutions**:

This issue was resolved in the latest workflow. If you're still experiencing it:

```yaml
# Ensure the workflow includes these steps in order:

- name: Install WiX Toolset
  run: dotnet tool install --global wix --version 4.0.5

- name: Install WiX UI Extension
  run: wix extension add WixToolset.UI.wixext

- name: Build WiX Installer
  run: |
    cd installer/wix
    wix build Product.wxs -ext WixToolset.UI.wixext/4.0.5 -arch x64 -out ../../FailsafeAutoBackup.msi
```

**Root Cause**: WiX Toolset v4 requires UI extensions to be installed separately. The workflow was missing the step to install the `WixToolset.UI.wixext` extension.

**Verification**:
1. Check the workflow run logs for the "Install WiX UI Extension" step
2. Verify the "Build WiX Installer" step completes without errors
3. Confirm the "Upload MSI Installer" step finds and uploads the file

### Diagnostic Script

```powershell
# Diagnose-FailsafeAutoBackup.ps1
# Comprehensive diagnostic script

#Requires -RunAsAdministrator

$ErrorActionPreference = "Continue"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Failsafe AutoBackup - Diagnostics" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Service Status
Write-Host "=== Service Status ===" -ForegroundColor Yellow
Get-Service -Name "FailsafeAutoBackupService" | Format-List *

# Process Information
Write-Host "\n=== Process Information ===" -ForegroundColor Yellow
Get-Process -Name "*FailsafeAutoBackup*" -ErrorAction SilentlyContinue | Format-Table Name,Id,CPU,WS -AutoSize

# Installation Files
Write-Host "\n=== Installation Files ===" -ForegroundColor Yellow
if (Test-Path "C:\Program Files\FailsafeAutoBackup") {
    Get-ChildItem "C:\Program Files\FailsafeAutoBackup" -Recurse -File | 
        Select-Object FullName,Length,LastWriteTime | 
        Format-Table -AutoSize
} else {
    Write-Host "Installation directory not found" -ForegroundColor Red
}

# Configuration
Write-Host "\n=== Configuration ===" -ForegroundColor Yellow
$configPath = "C:\Program Files\FailsafeAutoBackup\Service\appsettings.json"
if (Test-Path $configPath) {
    Get-Content $configPath
} else {
    Write-Host "Configuration file not found" -ForegroundColor Red
}

# Recent Logs
Write-Host "\n=== Recent Log Entries ===" -ForegroundColor Yellow
$logFile = Get-ChildItem "C:\ProgramData\FailsafeAutoBackup\Logs" -Filter "service-*.log" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1
if ($logFile) {
    Write-Host "Latest log: $($logFile.FullName)" -ForegroundColor Gray
    Get-Content $logFile.FullName -Tail 30
} else {
    Write-Host "No log files found" -ForegroundColor Red
}

# Event Viewer Errors
Write-Host "\n=== Recent Event Log Errors ===" -ForegroundColor Yellow
Get-EventLog -LogName Application -EntryType Error -Newest 10 -After (Get-Date).AddDays(-1) -ErrorAction SilentlyContinue |
    Where-Object { $_.Source -like "*Failsafe*" -or $_.Message -like "*Failsafe*" } |
    Format-Table TimeGenerated,Source,Message -Wrap -AutoSize

# Named Pipes
Write-Host "\n=== Named Pipes ===" -ForegroundColor Yellow
[System.IO.Directory]::GetFiles("\\.\pipe\") | Where-Object { $_ -like "*Failsafe*" }

Write-Host "\n=====================================" -ForegroundColor Cyan
Write-Host "Diagnostics Complete" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
```

### Getting Help

If you continue to experience issues:

1. **Run Diagnostic Script**: Execute `Diagnose-FailsafeAutoBackup.ps1`
2. **Collect Logs**: Gather all files from `C:\ProgramData\FailsafeAutoBackup\Logs`
3. **Screenshot Errors**: Capture any error messages
4. **Contact Support**: Email support@failsafeautobackup.com with:
   - Diagnostic script output
   - Log files
   - Screenshots
   - Windows version and build number
   - Steps to reproduce issue

---

## Testing Plan

### Pre-Production Testing

Before deploying to production, complete this comprehensive testing plan:

#### Test 1: Fresh Installation

```powershell
# 1. Clean system (no previous installation)
# 2. Run pre-flight check
.\Pre-Flight-Check.ps1

# 3. Install via MSI
# 4. Run post-install verification
.\Post-Install-Verification.ps1

# 5. Verify service is running
Get-Service -Name "FailsafeAutoBackupService"

# Expected: Status = Running, StartType = Automatic
```

#### Test 2: Basic Backup Functionality

```powershell
# 1. Open Microsoft Word
# 2. Create new document
# 3. Type some text
# 4. Save document as "TestDocument.docx"
# 5. Wait 2 minutes
# 6. Check backup folder

$backupFolder = "$env:USERPROFILE\FailsafeAutoBackup\Backups"
Get-ChildItem $backupFolder -Filter "TestDocument*.docx"

# Expected: At least one backup file with timestamp
```

#### Test 3: Multiple Document Backup

```powershell
# 1. Open 3 Word documents
# 2. Open 2 PDF files in Acrobat
# 3. Wait 2 minutes
# 4. Verify all documents backed up

Get-ChildItem $backupFolder | Sort-Object LastWriteTime -Descending

# Expected: Backups for all 5 documents
```

#### Test 4: Service Recovery

```powershell
# 1. Stop service manually
Stop-Service -Name "FailsafeAutoBackupService" -Force

# 2. Wait 1 minute
Start-Sleep -Seconds 60

# 3. Check if service auto-restarted
Get-Service -Name "FailsafeAutoBackupService"

# Expected: Service should auto-restart (Status = Running)
```

#### Test 5: Tray App Functionality

```
1. Launch tray app from Start Menu
2. Verify tray icon appears in notification area
3. Right-click tray icon → Open Dashboard
4. Verify dashboard shows:
   - Service status: Connected
   - Last backup time
   - Total backups count
5. Change backup interval to 5 minutes
6. Click Save
7. Verify service restarts with new interval
```

#### Test 6: Version Management

```powershell
# 1. Configure MaxBackupVersions = 3
# 2. Open Word document
# 3. Make changes and save 5 times, waiting 2+ minutes between saves
# 4. Check backup folder

Get-ChildItem $backupFolder -Filter "TestDocument*.docx" | Measure-Object

# Expected: Only 3 backup files (oldest deleted automatically)
```

#### Test 7: Uninstall and Reinstall

```powershell
# 1. Uninstall via Control Panel or MSI uninstall command
# 2. Verify service removed
Get-Service -Name "FailsafeAutoBackupService" -ErrorAction SilentlyContinue

# Expected: Service not found

# 3. Verify files removed
Test-Path "C:\Program Files\FailsafeAutoBackup"

# Expected: False (or directory doesn't exist)

# 4. Reinstall
# 5. Run post-install verification
```

#### Test 8: Upgrade Installation

```powershell
# 1. Install version 1.0.0
# 2. Create test backups
# 3. Install version 1.0.1 (or newer)
# 4. Verify service still running
# 5. Verify existing backups preserved
# 6. Verify configuration preserved
```

#### Test 9: Silent Installation

```powershell
# Test silent install
msiexec /i FailsafeAutoBackup-1.0.0-x64.msi /quiet /norestart /l*v silent-install.log

# Wait for completion
Start-Sleep -Seconds 30

# Verify installation
.\Post-Install-Verification.ps1

# Check log for errors
Select-String -Path "silent-install.log" -Pattern "Error|Failed"
```

#### Test 10: System Reboot

```
1. Complete installation
2. Create test backup
3. Restart computer
4. After reboot, verify:
   - Service auto-started
   - Tray app auto-started
   - Backups continue working
```

### Test Results Template

```
Test Date: _______________
Tester: _______________
Windows Version: _______________
Test Environment: [ ] VM [ ] Physical [ ] Production-like

Test 1: Fresh Installation           [ ] Pass [ ] Fail
Test 2: Basic Backup Functionality    [ ] Pass [ ] Fail
Test 3: Multiple Document Backup      [ ] Pass [ ] Fail
Test 4: Service Recovery              [ ] Pass [ ] Fail
Test 5: Tray App Functionality        [ ] Pass [ ] Fail
Test 6: Version Management            [ ] Pass [ ] Fail
Test 7: Uninstall and Reinstall       [ ] Pass [ ] Fail
Test 8: Upgrade Installation          [ ] Pass [ ] Fail
Test 9: Silent Installation           [ ] Pass [ ] Fail
Test 10: System Reboot                [ ] Pass [ ] Fail

Issues Found:
_________________________________________
_________________________________________
_________________________________________

Overall Status: [ ] Ready for Production [ ] Needs Fixes
```

---

## Production Deployment

### Pre-Deployment Checklist

Complete this checklist before deploying to production:

#### 1. Preparation
- [ ] All tests passed (see Testing Plan)
- [ ] MSI installer built and signed (code signing certificate)
- [ ] Release notes prepared
- [ ] User documentation updated
- [ ] Support team trained
- [ ] Rollback plan documented

#### 2. Infrastructure
- [ ] Backend API deployed and tested
- [ ] Database migrations applied (if any)
- [ ] Stripe API keys configured (production)
- [ ] License validation endpoint accessible
- [ ] TLS certificates valid and renewed
- [ ] Monitoring and alerting configured

#### 3. Installer Package
- [ ] Version number correct (e.g., 1.0.0)
- [ ] Digital signature valid
- [ ] Installer tested on clean VM
- [ ] Silent install tested
- [ ] Upgrade install tested
- [ ] All required files included

#### 4. Documentation
- [ ] Installation guide reviewed (this document)
- [ ] User manual available
- [ ] Known issues documented
- [ ] FAQ updated
- [ ] Support contact information current

### Deployment Strategies

#### Strategy 1: Pilot Deployment (Recommended)

```
Phase 1: Internal Testing (1-2 weeks)
- Deploy to 5-10 internal users
- Monitor logs and feedback daily
- Fix critical issues

Phase 2: Beta Release (2-4 weeks)
- Deploy to 50-100 volunteer users
- Gather feedback and metrics
- Address issues and optimize

Phase 3: Staged Rollout (4-8 weeks)
- Week 1: 10% of users
- Week 2: 25% of users
- Week 3: 50% of users
- Week 4+: 100% of users

Phase 4: General Availability
- Full production release
- Ongoing monitoring and support
```

#### Strategy 2: Enterprise Deployment

```powershell
# Enterprise-Deploy.ps1
# Automated deployment script for enterprise environments

param(
    [Parameter(Mandatory=$true)]
    [string]$InstallerPath,
    
    [Parameter(Mandatory=$false)]
    [string]$InstallLocation = "C:\Program Files\FailsafeAutoBackup",
    
    [Parameter(Mandatory=$false)]
    [switch]$Silent,
    
    [Parameter(Mandatory=$false)]
    [switch]$NoReboot
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Enterprise Deployment Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Verify installer exists
if (-not (Test-Path $InstallerPath)) {
    Write-Host "Error: Installer not found at $InstallerPath" -ForegroundColor Red
    exit 1
}

# Pre-flight check
Write-Host "Running pre-flight check..." -ForegroundColor Yellow
# (Include pre-flight check code here)

# Deploy
Write-Host "Starting installation..." -ForegroundColor Yellow
$arguments = "/i \`"$InstallerPath\`" INSTALLFOLDER=\`"$InstallLocation\`""
if ($Silent) {
    $arguments += " /quiet"
}
if ($NoReboot) {
    $arguments += " /norestart"
}
$arguments += " /l*v \`"$env:TEMP\failsafe-install.log\`""

$process = Start-Process -FilePath "msiexec.exe" -ArgumentList $arguments -Wait -PassThru

if ($process.ExitCode -eq 0) {
    Write-Host "✓ Installation successful" -ForegroundColor Green
} else {
    Write-Host "✗ Installation failed with exit code: $($process.ExitCode)" -ForegroundColor Red
    Write-Host "Check log: $env:TEMP\failsafe-install.log" -ForegroundColor Yellow
    exit $process.ExitCode
}

# Post-install verification
Write-Host "Running post-install verification..." -ForegroundColor Yellow
# (Include post-install verification code here)

Write-Host ""
Write-Host "Deployment complete!" -ForegroundColor Green
```

#### Strategy 3: SCCM/Intune Deployment

**SCCM Configuration**:
```xml
<!-- Create Application in SCCM -->
Application Name: Failsafe AutoBackup
Version: 1.0.0
Publisher: Failsafe AutoBackup Team

Installation Program:
msiexec /i "FailsafeAutoBackup-1.0.0-x64.msi" /quiet /norestart

Detection Method:
Registry Key: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{PRODUCT-GUID}
Value: DisplayVersion
Data Type: String
Equals: 1.0.0

Requirements:
- Operating System: Windows 10 (1809+) or Windows 11
- Architecture: x64
- Free Disk Space: 200 MB
```

**Intune Configuration**:
```
App Type: Windows app (Win32)
App Package: FailsafeAutoBackup-1.0.0-x64.intunewin
Install Command: msiexec /i "FailsafeAutoBackup-1.0.0-x64.msi" /quiet /norestart
Uninstall Command: msiexec /x {PRODUCT-GUID} /quiet /norestart

Detection Rule:
Type: MSI
MSI Product Code: {PRODUCT-GUID}

Requirements:
- Operating System: Windows 10 1809, Windows 11
- Architecture: x64
- Minimum OS Version: 10.0.17763

Assignments:
- Required: Pilot Group (Phase 1)
- Available: All Users (Phase 2+)
```

### Post-Deployment Monitoring

#### Key Metrics to Track

1. **Installation Success Rate**
   ```powershell
   # Track successful vs. failed installations
   # Target: >98% success rate
   ```

2. **Service Uptime**
   ```powershell
   # Monitor service availability
   # Target: >99.9% uptime
   ```

3. **Backup Success Rate**
   ```powershell
   # Track successful vs. failed backups
   # Target: >99.5% success rate
   ```

4. **User Adoption**
   ```powershell
   # Track active installations
   # Monitor usage patterns
   ```

5. **Support Tickets**
   ```powershell
   # Track and categorize issues
   # Identify trends and common problems
   ```

#### Monitoring Dashboard

```powershell
# Generate deployment report
$report = @{
    TotalInstallations = 0
    SuccessfulInstallations = 0
    FailedInstallations = 0
    ActiveServices = 0
    AverageBackupsPerDay = 0
    SupportTickets = 0
}

# Populate metrics from monitoring system
# Generate HTML report
# Email to stakeholders
```

### Rollback Procedure

If critical issues are discovered post-deployment:

```powershell
# Rollback-Deployment.ps1
# Emergency rollback to previous version

#Requires -RunAsAdministrator

Write-Host "Initiating rollback..." -ForegroundColor Yellow

# 1. Stop service
Stop-Service -Name "FailsafeAutoBackupService" -Force

# 2. Uninstall current version
msiexec /x {CURRENT-PRODUCT-GUID} /quiet /norestart

# 3. Install previous version
msiexec /i "FailsafeAutoBackup-{PREVIOUS-VERSION}-x64.msi" /quiet /norestart

# 4. Verify service
Start-Sleep -Seconds 30
Get-Service -Name "FailsafeAutoBackupService"

Write-Host "Rollback complete" -ForegroundColor Green
```

### Success Criteria

Deployment is considered successful when:

- [ ] Installation success rate >98%
- [ ] Service uptime >99.9% (first week)
- [ ] Backup success rate >99.5%
- [ ] <2% support ticket rate
- [ ] No critical bugs reported
- [ ] User satisfaction >85% (survey)

---

## Appendix

### Useful Commands

```powershell
# Service Management
Get-Service -Name "FailsafeAutoBackupService"
Start-Service -Name "FailsafeAutoBackupService"
Stop-Service -Name "FailsafeAutoBackupService"
Restart-Service -Name "FailsafeAutoBackupService"

# View Service Logs
Get-Content "C:\ProgramData\FailsafeAutoBackup\Logs\service-$(Get-Date -Format 'yyyyMMdd').log" -Tail 100 -Wait

# Check Service Configuration
Get-Content "C:\Program Files\FailsafeAutoBackup\Service\appsettings.json"

# List Backup Files
Get-ChildItem "$env:USERPROFILE\FailsafeAutoBackup\Backups" | Sort-Object LastWriteTime -Descending

# Check Process Status
Get-Process -Name "*FailsafeAutoBackup*"

# View Event Logs
Get-EventLog -LogName Application -Source "*Failsafe*" -Newest 50
```

### Configuration File Reference

**appsettings.json** (Service Configuration):

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `BackupIntervalMinutes` | int | 2 | Minutes between backup cycles |
| `BackupFolderPath` | string | "" | Custom backup folder (empty = default) |
| `EnableWordBackup` | bool | true | Enable Word document backup |
| `EnablePdfBackup` | bool | true | Enable PDF document backup |
| `MaxBackupVersions` | int | 10 | Maximum versions to retain per document |
| `ComTimeoutSeconds` | int | 30 | COM automation timeout |
| `CreateDesktopShortcut` | bool | true | Create desktop shortcut to backup folder |
| `LogLevel` | string | "Information" | Log level (Verbose, Debug, Information, Warning, Error, Fatal) |

### Directory Structure

```
C:\Program Files\FailsafeAutoBackup\
├── Service\
│   ├── FailsafeAutoBackup.Service.exe
│   ├── appsettings.json
│   └── [other DLLs and dependencies]
└── TrayApp\
    ├── FailsafeAutoBackup.TrayApp.exe
    └── [other DLLs and dependencies]

C:\ProgramData\FailsafeAutoBackup\
└── Logs\
    ├── service-20250115.log
    ├── service-20250116.log
    └── [rolling log files - 30 day retention]

%USERPROFILE%\FailsafeAutoBackup\
└── Backups\
    ├── [backup files]
    └── Desktop Shortcut (optional)
```

### License Information

**Product**: Failsafe AutoBackup  
**Version**: 1.0.0  
**Copyright**: © 2025 Failsafe AutoBackup. All rights reserved.  
**License Type**: Commercial (subscription-based)  
**Support**: support@failsafeautobackup.com

---

## Document Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-01-15 | Initial release |

---

**End of Installation Guide**
