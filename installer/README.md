# Installer

This folder contains installer configurations and scripts for packaging Failsafe AutoBackup.

## WiX Toolset Installer

The primary installer is built using [WiX Toolset v4](https://wixtoolset.org/).

### Prerequisites

1. Install WiX Toolset v4 or later (via .NET tool):
   ```bash
   dotnet tool install --global wix --version 4.0.5
   ```

2. Build the solution in Release mode
3. Publish self-contained executables

### Building the Installer

```powershell
# Step 1: Build and publish all components (self-contained)
dotnet publish src/FailsafeAutoBackup.Service/FailsafeAutoBackup.Service.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o publish/service

dotnet publish src/FailsafeAutoBackup.TrayApp/FailsafeAutoBackup.TrayApp.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o publish/trayapp

# Step 2: Build the MSI installer
cd installer/wix
wix build Product.wxs -ext WixToolset.UI.wixext -arch x64 -out ../../FailsafeAutoBackup.msi

# Or build using the project file
wix build FailsafeAutoBackup.Installer.wixproj -arch x64
```

### Installer Features

The WiX installer includes:

1. **Windows Service Installation**
   - Installs FailsafeAutoBackup.Service as self-contained executable
   - Registers as Windows Service: `FailsafeAutoBackupService`
   - Configures automatic startup
   - Sets recovery options (restart on failure, 3 attempts, 1-minute delay)

2. **Tray Application**
   - Installs WPF Tray Application as self-contained executable
   - Creates Start Menu shortcuts
   - Optional desktop shortcut (user selectable during install)
   - Configures auto-start with Windows login via registry

3. **Installation Directory Structure**
   ```
   C:\Program Files\FailsafeAutoBackup\
   ├── Service\
   │   └── FailsafeAutoBackup.Service.exe    (35 MB self-contained)
   └── TrayApp\
       └── FailsafeAutoBackup.TrayApp.exe     (69 MB self-contained)
   
   C:\ProgramData\FailsafeAutoBackup\
   └── Logs\                                  (created on first run)
   ```

4. **Start Menu Integration**
   - Shortcut: `Start Menu\Programs\Failsafe AutoBackup\Failsafe AutoBackup.lnk`
   - Launches the Tray Application

5. **Auto-Start Configuration**
   - Registry key: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\FailsafeAutoBackup`
   - Tray App launches automatically on Windows login

6. **Uninstallation**
   - Stops and removes Windows Service
   - Removes all installed files
   - Removes registry entries
   - Removes shortcuts
   - Preserves user backup data (optional)

### Self-Contained Deployment

The application is published as self-contained, meaning:
- ✅ No .NET Runtime installation required
- ✅ All dependencies bundled in the executable
- ✅ Single-file executables for easy deployment
- ✅ Works on any Windows 10/11 x64 machine
- ⚠️ Larger file sizes (Service: ~35MB, TrayApp: ~69MB)
- ⚠️ Must rebuild for .NET version updates

### MSI Properties

Custom properties that can be set during installation:

```powershell
# Install to custom directory
msiexec /i FailsafeAutoBackup.msi INSTALLFOLDER="D:\CustomPath" /qn

# Install with all features
msiexec /i FailsafeAutoBackup.msi ADDLOCAL=ALL /qn

# Install without desktop shortcut
msiexec /i FailsafeAutoBackup.msi ADDLOCAL=MainApplication /qn
```

### File Structure

```
installer/
├── wix/
│   ├── Product.wxs                        # Main WiX source file
│   ├── FailsafeAutoBackup.Installer.wixproj  # WiX project file
│   └── License.rtf                        # License agreement (RTF format)
├── scripts/                               # PowerShell installation scripts
│   ├── Initialize-BackupFolder.ps1
│   ├── Create-DesktopShortcut.ps1
│   └── Configure-UserPath.ps1
└── README.md                              # This file
```

## Development Notes

### Current Implementation

The WiX installer is **production-ready** and includes:

1. ✅ **Component Definitions** - Service and TrayApp executables
2. ✅ **Windows Service Installation** - Automatic registration and startup
3. ✅ **Service Recovery Policy** - Restart on failure with 1-minute delay
4. ✅ **Start Menu Shortcuts** - Main application shortcut
5. ✅ **Desktop Shortcuts** - Optional feature during installation
6. ✅ **Auto-Start Configuration** - Registry key for TrayApp auto-start
7. ✅ **Directory Creation** - Logs directory in ProgramData
8. ✅ **Major Upgrade Support** - UpgradeCode for versioning
9. ✅ **License Agreement** - RTF license file included
10. ✅ **Uninstall Support** - Complete cleanup of all components

### Component GUIDs

The following GUIDs are used in the installer:
- Package UpgradeCode: `C33254F0-25AF-4342-95AD-D74F2D726684`
- Service Executable: `A1B2C3D4-E5F6-4A5B-9C8D-7E6F5A4B3C2D`
- TrayApp Executable: `B2C3D4E5-F6A7-5B6C-AD9E-8F7A6B5C4D3E`
- Logs Directory: `C3D4E5F6-A7B8-6C7D-BE0F-9A8B7C6D5E4F`
- Start Menu Shortcut: `D4E5F6A7-B8C9-7D8E-CF1A-0B9C8D7E6F5A`
- Desktop Shortcut: `E5F6A7B8-C9DA-8E9F-DA2B-1C0D9E8F7A6B`
- Auto-Start Registry: `F6A7B8C9-DAEB-9FAD-EB3C-2D1E0F9A8B7C`

**Important**: These GUIDs must remain constant across versions to ensure proper upgrades.

### Testing the Installer

Before releasing:

1. **Build the installer**:
   ```powershell
   # From repository root
   .\build-installer.ps1
   ```

2. **Test installation on clean VM**:
   - Windows 10 (fresh install)
   - Windows 11 (fresh install)
   - Verify no .NET Runtime is required

3. **Verify components**:
   ```powershell
   # Check service
   Get-Service "FailsafeAutoBackupService"
   
   # Check files
   Test-Path "C:\Program Files\FailsafeAutoBackup\Service\FailsafeAutoBackup.Service.exe"
   Test-Path "C:\Program Files\FailsafeAutoBackup\TrayApp\FailsafeAutoBackup.TrayApp.exe"
   
   # Check shortcuts
   Test-Path "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Failsafe AutoBackup\Failsafe AutoBackup.lnk"
   
   # Check auto-start
   Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "FailsafeAutoBackup"
   ```

4. **Test functionality**:
   - Service starts automatically
   - Tray App launches and connects to service
   - Backups work correctly

5. **Test uninstallation**:
   - All files removed
   - Service deleted
   - Registry entries cleaned
   - No leftover processes

### Troubleshooting Build Issues

**Issue: WiX toolset not found**

```bash
# Install WiX as .NET tool
dotnet tool install --global wix --version 4.0.5

# Verify installation
wix --version
```

**Issue: Source files not found**

```
Error: Cannot find file 'publish/service/FailsafeAutoBackup.Service.exe'
```

Solution: Ensure you've published the applications first:
```powershell
# Publish both applications before building installer
dotnet publish src/FailsafeAutoBackup.Service/... (see commands above)
dotnet publish src/FailsafeAutoBackup.TrayApp/... (see commands above)
```

**Issue: WiX build errors**

```
Error: Component 'ServiceExecutable' has invalid KeyPath
```

Solution: Verify all file paths in Product.wxs are correct relative to the WiX file location.

## CI/CD Integration

The GitHub Actions workflow (`.github/workflows/blank.yml`) automates:

1. Building the solution
2. Running tests
3. Publishing self-contained executables
4. Installing WiX Toolset
5. Building the MSI installer
6. Uploading installer as artifact

See the workflow file for details.

## Alternative Installers

While WiX is the primary installer, consider these alternatives:

1. **MSIX Package** - For Microsoft Store distribution
2. **Inno Setup** - Simpler script-based installer
3. **ClickOnce** - For auto-updating desktop applications (not recommended for services)
4. **Chocolatey Package** - For package manager installation
5. **Winget** - For Windows Package Manager

## Resources

- [WiX Toolset v4 Documentation](https://wixtoolset.org/docs/intro/)
- [Windows Installer Best Practices](https://docs.microsoft.com/en-us/windows/win32/msi/windows-installer-best-practices)
- [.NET Self-Contained Deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-self-contained)
- [Windows Services](https://docs.microsoft.com/en-us/dotnet/core/extensions/windows-service)
