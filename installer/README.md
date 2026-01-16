# Installer

This folder contains installer configurations and scripts for packaging Failsafe AutoBackup.

## WiX Toolset Installer

The primary installer is built using [WiX Toolset v4](https://wixtoolset.org/).

### Prerequisites

1. Install WiX Toolset v4 or later
2. Build the solution in Release mode
3. Publish all projects

### Building the Installer

```powershell
# Build all components
dotnet publish src/FailsafeAutoBackup.Service/FailsafeAutoBackup.Service.csproj -c Release -o publish/service
dotnet publish src/FailsafeAutoBackup.TrayApp/FailsafeAutoBackup.TrayApp.csproj -c Release -o publish/trayapp
dotnet publish src/FailsafeAutoBackup.BackendApi/FailsafeAutoBackup.BackendApi.csproj -c Release -o publish/api

# Build the installer (placeholder - needs component groups defined)
cd installer/wix
wix build Product.wxs -o ../../FailsafeAutoBackup.msi
```

### Installer Features

The WiX installer will include:

1. **Windows Service Installation**
   - Installs FailsafeAutoBackup.Service
   - Registers as Windows Service
   - Configures automatic startup
   - Sets recovery options

2. **Tray Application**
   - Installs WPF Tray Application
   - Creates Start Menu shortcuts
   - Optional desktop shortcut
   - Configures auto-start with Windows login

3. **Backend API (Optional)**
   - Installs ASP.NET Core Web API
   - Optional feature during installation

4. **Configuration**
   - Creates default configuration files
   - Initializes backup folders
   - Sets up logging directories

5. **Uninstallation**
   - Stops and removes Windows Service
   - Removes all installed files
   - Optionally preserves backup data

### Custom Actions

The installer includes custom actions for:

- Installing and starting Windows Service
- Creating backup folder structure
- Generating desktop shortcuts
- Configuring user-specific paths
- Initializing database (if Backend API is installed)

### File Structure

```
installer/
├── wix/
│   ├── Product.wxs          # Main WiX source file
│   ├── Components.wxs        # Component definitions (future)
│   ├── UI.wxs               # Custom UI definitions (future)
│   └── CustomActions.wxs    # Custom action definitions (future)
├── scripts/
│   ├── install-service.ps1  # Service installation script
│   └── uninstall-service.ps1 # Service removal script
└── README.md
```

## Development Notes

### Current Status

This is a **placeholder framework**. The following items need to be completed:

1. **Component Groups** - Define file components for each application
2. **Service Installation** - Add custom actions for Windows Service
3. **Registry Keys** - Add registry entries for configuration
4. **License Agreement** - Add license RTF file
5. **Custom UI** - Create branded installer UI
6. **Digital Signing** - Add code signing certificate

### Future Enhancements

- Support for side-by-side installations (multiple versions)
- Per-user vs. per-machine installation options
- Silent installation support with command-line parameters
- Upgrade path from previous versions
- Custom installation paths
- Feature selection during installation

## Alternative Installers

In addition to WiX, the following installer options could be considered:

1. **MSIX Package** - For Microsoft Store distribution
2. **Inno Setup** - Simpler alternative to WiX
3. **ClickOnce** - For auto-updating desktop applications
4. **Chocolatey Package** - For package manager installation

## Testing

Before releasing the installer:

1. Test on clean Windows installations
2. Verify service installation and startup
3. Test upgrade scenarios
4. Verify uninstallation cleanup
5. Check for registry pollution
6. Validate security permissions

## Resources

- [WiX Toolset Documentation](https://wixtoolset.org/docs/)
- [Windows Installer Best Practices](https://docs.microsoft.com/en-us/windows/win32/msi/windows-installer-best-practices)
- [.NET Application Deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/)
