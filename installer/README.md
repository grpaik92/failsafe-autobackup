# Installer

This folder contains installer configurations and scripts for packaging Failsafe AutoBackup.

## WiX Toolset Installer

The primary installer is built using [WiX Toolset v4](https://wixtoolset.org/).

### Prerequisites

1. Install WiX Toolset v4 or later (via .NET tool):
   ```bash
   dotnet tool install --global wix --version 4.0.5
   ```

2. Add WiX UI Extension:
   ```bash
   wix extension add WixToolset.UI.wixext --global
   ```

3. Build the solution in Release mode
4. Publish self-contained executables

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

# Step 2: Add WiX UI Extension (one time setup)
wix extension add WixToolset.UI.wixext --global

# Step 3: Build the MSI installer
cd installer/wix
wix build Product.wxs -arch x64 -out ../../FailsafeAutoBackup.msi

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
- Service Executable: `BF11B56D-0EFA-4112-8F04-A1B76382CA01`
- TrayApp Executable: `829DC472-33C0-48E6-A53E-BAD518AA6AAB`
- Logs Directory: `EC4045AF-6D87-4044-9939-1B8116A37DBE`
- Start Menu Shortcut: `123687ED-81E1-4DD0-AC5F-1DB3533545FF`
- Desktop Shortcut: `1A197F99-A77D-4127-9FF6-EBF51CDB6293`
- Auto-Start Registry: `04BAFCC4-2ACE-409D-A499-FB95D852C4D3`

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

**Issue: WixToolset.UI.wixext not found (WIX0144)**

```bash
# Add the UI extension globally
wix extension add WixToolset.UI.wixext --global

# Verify extensions
wix extension list
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
