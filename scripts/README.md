# PowerShell Scripts

This folder contains PowerShell scripts for installation, configuration, and management of Failsafe AutoBackup.

## Available Scripts

### 1. Initialize-BackupFolder.ps1

Initializes the backup folder structure with default configuration.

**Usage:**
```powershell
# Use default location (Documents\FailsafeAutoBackup)
.\Initialize-BackupFolder.ps1

# Use custom location
.\Initialize-BackupFolder.ps1 -BackupPath "D:\MyBackups"
```

**What it does:**
- Creates main backup folder
- Creates subfolders (Word, PDF, Logs, Temp)
- Generates default configuration file (config.json)
- Creates README in backup folder
- Sets Temp folder as hidden

### 2. Create-DesktopShortcut.ps1

Creates a desktop shortcut for the Tray Application.

**Usage:**
```powershell
# Basic usage
.\Create-DesktopShortcut.ps1 -TrayAppPath "C:\Program Files\FailsafeAutoBackup\FailsafeAutoBackup.TrayApp.exe"

# With custom icon
.\Create-DesktopShortcut.ps1 -TrayAppPath "C:\Program Files\FailsafeAutoBackup\FailsafeAutoBackup.TrayApp.exe" -IconPath "C:\Icons\app.ico"

# Custom shortcut name
.\Create-DesktopShortcut.ps1 -TrayAppPath "C:\Program Files\FailsafeAutoBackup\FailsafeAutoBackup.TrayApp.exe" -ShortcutName "My Backup App"
```

**What it does:**
- Creates desktop shortcut with proper icon
- Sets working directory
- Configures shortcut description

### 3. Configure-UserPath.ps1

Configures user-specific paths for backups, logs, and configuration.

**Usage:**
```powershell
# Use default paths
.\Configure-UserPath.ps1

# Custom paths
.\Configure-UserPath.ps1 -BackupPath "D:\Backups" -LogPath "D:\Logs"
```

**What it does:**
- Creates directory structure
- Generates user configuration file (user_config.json)
- Sets environment variable FAILSAFE_BACKUP_PATH
- Stores configuration in AppData

## Configuration Files

### config.json (Backup Folder)

```json
{
  "BackupPath": "C:\\Users\\User\\Documents\\FailsafeAutoBackup",
  "BackupIntervalMinutes": 2,
  "MaxVersions": 10,
  "EnableWord": true,
  "EnablePDF": true,
  "CreatedAt": "2025-01-16T12:00:00Z"
}
```

### user_config.json (AppData)

```json
{
  "Version": "1.0",
  "User": {
    "Username": "JohnDoe",
    "MachineName": "DESKTOP-ABC123",
    "ConfiguredAt": "2025-01-16T12:00:00Z"
  },
  "Paths": {
    "BackupPath": "C:\\Users\\JohnDoe\\Documents\\FailsafeAutoBackup",
    "LogPath": "C:\\Users\\JohnDoe\\AppData\\Local\\FailsafeAutoBackup\\Logs",
    "ConfigPath": "C:\\Users\\JohnDoe\\AppData\\Roaming\\FailsafeAutoBackup"
  },
  "Settings": {
    "BackupIntervalMinutes": 2,
    "MaxVersions": 10,
    "EnableWord": true,
    "EnablePDF": true,
    "AutoStartWithWindows": true
  }
}
```

## Installation Workflow

Typical installation and setup workflow:

```powershell
# 1. Initialize backup folder
.\Initialize-BackupFolder.ps1

# 2. Configure user paths
.\Configure-UserPath.ps1

# 3. Create desktop shortcut
.\Create-DesktopShortcut.ps1 -TrayAppPath "C:\Program Files\FailsafeAutoBackup\FailsafeAutoBackup.TrayApp.exe"

# 4. Install and start Windows Service (requires admin)
sc.exe create "FailsafeAutoBackup" binPath="C:\Program Files\FailsafeAutoBackup\Service\FailsafeAutoBackup.Service.exe"
sc.exe start "FailsafeAutoBackup"
```

## Requirements

- Windows 10 or later
- PowerShell 5.1 or later
- .NET 8 Runtime

## Security

All scripts:
- Use secure Windows ACLs for file permissions
- Store configuration in user-specific directories
- Never store credentials in plain text
- Follow Windows best practices for file system operations

## Troubleshooting

### Script Execution Policy

If you get an error about execution policy:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Permission Errors

Run PowerShell as Administrator for system-level operations.

### Path Not Found

Ensure all paths are absolute and properly formatted with double backslashes.

## Future Enhancements

- [ ] Add script for automatic Windows Service installation
- [ ] Add script for configuring auto-start with Windows
- [ ] Add script for backup cleanup and maintenance
- [ ] Add script for exporting/importing configuration
