# Failsafe AutoBackup - Windows Native Application

A production-ready Windows-only application utilizing a Windows-native .NET stack designed for maximum reliability and resilience ("service never dies"). Automatically backs up Microsoft Word and PDF documents every 2 minutes.

## 🏗️ Architecture

### Components

1. **Windows Service (.NET Worker Service)** - Robust background engine running auto-backups every 2 minutes
2. **WPF Tray Application** - System tray interface for status, settings, and controls
3. **IPC Layer (Named Pipes)** - Secure communication between Tray App and Service
4. **Backend API (ASP.NET Core)** - Handles licensing, subscriptions, and device management
5. **SQLite Database** - Local storage for backend (PostgreSQL ready for production)

### Key Features

✅ **Service Never Dies** - Automatic restart policies, circuit breakers, retry logic, exponential backoff  
✅ **Resilient Architecture** - Fault isolation, STA thread handling for COM automation  
✅ **Auto-Backup** - Detects and backs up active documents every 2 minutes (saved and unsaved)  
✅ **Multi-Application Support** - Microsoft Word and Adobe Acrobat/PDF editors  
✅ **Licensing & Payments** - Stripe integration for subscription management  
✅ **Device Management** - Single-device per-user constraint with device fingerprinting  
✅ **Secure Communication** - Named Pipes with authentication, DPAPI token storage  
✅ **Desktop Integration** - Desktop shortcuts for quick access to backups  

## 📋 Prerequisites

- Windows 10/11
- .NET 8.0 SDK or later
- Visual Studio 2022 or JetBrains Rider (optional, for development)
- Administrator privileges (for service installation)

## 🚀 Getting Started

### Development Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/grpaik92/failsafe-autobackup.git
   cd failsafe-autobackup
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the solution:**
   ```bash
   dotnet build
   ```

4. **Run tests:**
   ```bash
   dotnet test
   ```

### Running the Backend API

```bash
cd src/FailsafeAutoBackup.BackendApi
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger UI at `https://localhost:5001/swagger`

### Installing the Windows Service

1. **Build the service:**
   ```bash
   dotnet publish src/FailsafeAutoBackup.Service -c Release -o publish/service
   ```

2. **Install as Windows Service (Run as Administrator):**
   ```powershell
   sc.exe create "FailsafeAutoBackup Service" binPath= "C:\path\to\publish\service\FailsafeAutoBackup.Service.exe"
   sc.exe start "FailsafeAutoBackup Service"
   ```

3. **Configure service recovery (Run as Administrator):**
   ```powershell
   sc.exe failure "FailsafeAutoBackup Service" reset= 86400 actions= restart/60000/restart/60000/restart/60000
   ```

### Running the Tray Application

```bash
cd src/FailsafeAutoBackup.TrayApp
dotnet run
```

Or publish and run the executable:
```bash
dotnet publish src/FailsafeAutoBackup.TrayApp -c Release -o publish/trayapp
publish/trayapp/FailsafeAutoBackup.TrayApp.exe
```

## 🔧 Configuration

### Service Configuration

Edit `src/FailsafeAutoBackup.Service/appsettings.json`:

```json
{
  "ServiceConfiguration": {
    "BackupIntervalMinutes": 2,
    "BackupFolderPath": "",
    "EnableWordBackup": true,
    "EnablePdfBackup": true,
    "MaxBackupVersions": 10,
    "ComTimeoutSeconds": 30,
    "CreateDesktopShortcut": true
  }
}
```

### Backend API Configuration

Edit `src/FailsafeAutoBackup.BackendApi/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=failsafeautobackup.db"
  },
  "Stripe": {
    "SecretKey": "your-stripe-secret-key",
    "PublishableKey": "your-stripe-publishable-key"
  }
}
```

## 📦 Project Structure

```
failsafe-autobackup/
├── src/
│   ├── FailsafeAutoBackup.Service/       # Windows Service
│   │   ├── Services/                     # Document detection, backup logic
│   │   ├── Resilience/                   # Retry, circuit breaker patterns
│   │   └── Program.cs                    # Service entry point
│   ├── FailsafeAutoBackup.TrayApp/       # WPF Tray Application
│   │   ├── MainWindow.xaml               # Main dashboard UI
│   │   └── MainWindow.xaml.cs            # UI logic
│   ├── FailsafeAutoBackup.IPC/           # Named Pipes communication
│   │   ├── Server/                       # IPC server for service
│   │   └── Client/                       # IPC client for tray app
│   ├── FailsafeAutoBackup.BackendApi/    # ASP.NET Core Web API
│   │   ├── Controllers/                  # API endpoints
│   │   ├── Services/                     # Business logic
│   │   └── Models/                       # Data models
│   └── FailsafeAutoBackup.Shared/        # Shared models and contracts
│       ├── Models/                       # Domain models
│       ├── IPC/                          # IPC message contracts
│       └── Configuration/                # Configuration models
├── tests/
│   └── FailsafeAutoBackup.Tests/         # Unit tests
├── .github/
│   └── workflows/
│       └── build.yml                     # CI/CD pipeline
└── README.md
```

## 🛡️ Security Features

- **DPAPI Encryption** - Secure token storage using Windows Data Protection API
- **Windows Credential Manager** - Integration for credential management
- **Named Pipes Security** - Authenticated pipes with access control
- **TLS 1.2+** - Secure backend API communication
- **Least Privilege** - Service runs with minimal required permissions
- **Ransomware Protection** - Handles Controlled Folder Access scenarios

## 🔄 Service Resilience

### Automatic Restart Policy
- Service automatically restarts on failure
- Configurable restart intervals (default: 1 minute)
- Daily reset of failure count

### Retry Logic
- Exponential backoff for transient failures
- Circuit breaker pattern to prevent cascading failures
- Fault isolation for COM automation

### Watchdog
- Task Scheduler monitors service health every 5 minutes
- Lightweight heartbeat check
- Automatic service restart if unhealthy

## 📱 Tray Application Features

- **System Tray Integration** - Minimizes to tray, auto-starts with Windows
- **Status Dashboard** - Real-time service status and statistics
- **Settings Management** - Configure backup intervals, folders, and options
- **Service Control** - Restart service, view logs
- **Quick Access** - Open logs folder, open backup folder

## 🔐 Licensing & Payments

- **Stripe Integration** - Subscription management and payment processing
- **Device Licensing** - Single-device per-user constraint
- **Device Fingerprinting** - Windows-based device identification
- **Session Management** - Backend validation and session handling

## 🚢 Deployment

### Building Installer

The project includes WiX Toolset v4 configuration for building MSI installers.

```powershell
# Use the automated build script (recommended)
.\build-installer.ps1

# Or build manually:

# 1. Install WiX Toolset v4
dotnet tool install --global wix --version 4.0.5

# 2. Add WiX UI Extension
wix extension add WixToolset.UI.wixext --global

# 3. Publish self-contained executables
dotnet publish src/FailsafeAutoBackup.Service/FailsafeAutoBackup.Service.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -o publish/service

dotnet publish src/FailsafeAutoBackup.TrayApp/FailsafeAutoBackup.TrayApp.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -o publish/trayapp

# 4. Build the MSI installer
cd installer/wix
wix build Product.wxs -arch x64 -out ../../FailsafeAutoBackup.msi
```

For more details, see [installer/README.md](installer/README.md)

### GitHub Actions CI/CD

The repository includes a GitHub Actions workflow that:
- Builds the solution on push/PR
- Runs tests
- Publishes self-contained executables
- Installs WiX Toolset and extensions
- Builds Windows MSI installer
- Uploads all artifacts

See `.github/workflows/blank.yml` for details.

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

### Test Coverage

- Unit tests for business logic
- Integration tests for IPC communication
- Service resilience tests
- Licensing validation tests

## 📝 Logging

Logs are written to:
- **Service**: `C:\ProgramData\FailsafeAutoBackup\Logs\service-YYYYMMDD.log`
- **Tray App**: Console and Windows Event Log

Log rotation: Daily, retained for 30 days

## 🐛 Troubleshooting

### Service Won't Start
1. Check Windows Event Viewer for errors
2. Verify service is installed: `sc.exe query "FailsafeAutoBackup Service"`
3. Check log files in `C:\ProgramData\FailsafeAutoBackup\Logs`

### Tray App Can't Connect to Service
1. Verify service is running: `sc.exe query "FailsafeAutoBackup Service"`
2. Check Named Pipe permissions
3. Restart service: Use tray app or `sc.exe restart "FailsafeAutoBackup Service"`

### Backups Not Working
1. Check backup folder permissions
2. Verify Word/Acrobat is running
3. Check service logs for COM automation errors

## 📄 License

Copyright © 2025 Failsafe AutoBackup. All rights reserved.

## 🤝 Contributing

This is a private repository. For questions or issues, contact the repository owner.

## 📞 Support

For support inquiries, please contact support@failsafeautobackup.com
