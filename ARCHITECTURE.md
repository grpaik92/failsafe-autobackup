# Architecture Documentation

## System Overview

Failsafe AutoBackup is a Windows-native application designed with a "service never dies" philosophy. The system consists of multiple components working together to provide resilient, automatic document backup functionality.

## Component Architecture

### 1. Windows Service (Background Engine)

**Technology**: .NET 8 Worker Service  
**Purpose**: Continuous background operation for automatic backups  
**Location**: `src/FailsafeAutoBackup.Service`

#### Responsibilities
- Document detection (Word, PDF) every 2 minutes
- Automatic backup of saved and unsaved documents
- COM automation for Microsoft Office integration
- Resilience patterns (retry, circuit breaker, exponential backoff)
- IPC server for communication with Tray App
- Heartbeat management for watchdog monitoring

#### Key Classes
- `Worker` - Main background service loop
- `DocumentDetector` - Detects active documents
- `BackupService` - Handles backup operations
- `WordComService` - COM automation on STA thread
- `HeartbeatService` - Health monitoring
- `ResiliencePolicies` - Polly-based resilience patterns

#### Resilience Features
- **Retry Logic**: 3 retries with exponential backoff (2^n seconds)
- **Circuit Breaker**: Opens after 5 consecutive failures, closed after 1 minute
- **Fault Isolation**: COM operations run on separate STA thread with timeout
- **Cancellation Tokens**: Graceful shutdown on service stop
- **Exception Handling**: All operations wrapped in try-catch with logging

### 2. WPF Tray Application (User Interface)

**Technology**: WPF .NET 8  
**Purpose**: User interface and service control  
**Location**: `src/FailsafeAutoBackup.TrayApp`

#### Responsibilities
- System tray integration
- Real-time service status display
- Settings management
- Service control (restart, logs)
- IPC client for communication with Service
- Auto-start with Windows login

#### Key Features
- **Minimize to Tray**: App runs in system tray
- **Status Dashboard**: Real-time service health and statistics
- **Settings UI**: Configure backup intervals, folders, options
- **Quick Actions**: 
  - Restart Service
  - Open Logs Folder
  - Open Backup Folder
- **Auto-Reconnect**: Automatic IPC reconnection on disconnect

### 3. IPC Layer (Communication)

**Technology**: Named Pipes with Security  
**Purpose**: Secure inter-process communication  
**Location**: `src/FailsafeAutoBackup.IPC`

#### Design
- **Protocol**: Named Pipes with Message mode
- **Security**: Windows ACL (current user + administrators)
- **Serialization**: JSON
- **Pattern**: Request-Response

#### Message Types
- `GetStatus` / `StatusResponse`
- `RestartService`
- `GetBackupList` / `BackupListResponse`
- `UpdateConfiguration` / `ConfigurationUpdated`
- `Heartbeat` / `HeartbeatResponse`
- `Error`

#### Implementation
- **Server** (`NamedPipeServer`): Runs in Windows Service
- **Client** (`NamedPipeClient`): Used by Tray App
- **Connection Pool**: Multiple concurrent client connections
- **Timeout Handling**: 5-second connection timeout

### 4. Backend API (Licensing & Subscriptions)

**Technology**: ASP.NET Core 8 Web API  
**Purpose**: Licensing validation, subscription management  
**Location**: `src/FailsafeAutoBackup.BackendApi`

#### Endpoints

**License Validation**
```
POST /api/license/validate
{
  "userId": "string",
  "deviceFingerprint": "string"
}
```

**Device Registration**
```
POST /api/license/register-device
{
  "userId": "string",
  "deviceFingerprint": "string",
  "deviceName": "string"
}
```

**Device Deactivation**
```
POST /api/license/deactivate-device
{
  "userId": "string",
  "deviceFingerprint": "string"
}
```

#### Database Schema

**Users**
- `Id` (PK)
- `Email` (Unique)
- `ClioUserId` (Unique)
- `CreatedAt`
- `LastLoginAt`

**Subscriptions**
- `Id` (PK)
- `UserId` (FK)
- `StripeSubscriptionId` (Unique)
- `StripeCustomerId`
- `Status` (active, inactive, expired, cancelled)
- `StartDate`
- `EndDate`
- `MaxDevices` (default: 1)

**Devices**
- `Id` (PK)
- `UserId` (FK)
- `DeviceFingerprint` (Indexed)
- `DeviceName`
- `RegisteredAt`
- `LastSeenAt`
- `IsActive`

### 5. Shared Library (Common Models)

**Technology**: .NET 8 Class Library  
**Purpose**: Shared models and contracts  
**Location**: `src/FailsafeAutoBackup.Shared`

#### Contents
- Domain models (`BackupDocument`, `ServiceStatus`, `UserSession`, `LicenseInfo`)
- IPC contracts (`IPCMessage`, `MessageType`)
- Configuration models (`ServiceConfiguration`)

## Data Flow

### Backup Flow

```
1. Timer triggers (every 2 minutes)
2. Worker.ExecuteAsync() invoked
3. DocumentDetector.DetectActiveDocumentsAsync()
   - Detect Word processes
   - Detect PDF processes (Acrobat)
   - Return list of active documents
4. For each document:
   - BackupService.BackupDocumentAsync()
   - Generate backup filename with timestamp
   - Copy file to backup folder
   - Manage version limit (keep last N versions)
5. Update heartbeat
6. Log statistics
```

### IPC Communication Flow

```
1. Tray App: Send IPCMessage via NamedPipeClient
2. Named Pipe: Serialize to JSON, send over pipe
3. Service: NamedPipeServer receives message
4. Service: Process message, execute action
5. Service: Send response via same pipe connection
6. Named Pipe: Serialize response to JSON
7. Tray App: Receive and deserialize response
8. Tray App: Update UI
```

### License Validation Flow

```
1. Service/Tray App: Generate device fingerprint
2. Send validation request to Backend API
3. Backend API: Query database
   - Check user exists
   - Check active subscription
   - Check device registration
   - Check device limit
4. Backend API: Return validation result
5. Service: Allow/deny operation based on result
```

## Security Architecture

### Token Storage
- **DPAPI**: Windows Data Protection API for token encryption
- **Credential Manager**: Windows Credential Manager integration
- **In-Memory**: Tokens only in memory during runtime

### Communication Security
- **Named Pipes**: ACL-based access control (current user + admins)
- **TLS 1.2+**: All backend API calls use HTTPS
- **No Plaintext**: Sensitive data encrypted at rest

### Least Privilege
- Service runs as LocalSystem (required for COM automation)
- Tray App runs as current user
- Backup folder: User-accessible directory

## Resilience Patterns

### Circuit Breaker
```
Closed → Exception → Closed (retry)
Closed → 5 Exceptions → Open (1 minute break)
Open → Timeout → Half-Open (test single request)
Half-Open → Success → Closed
Half-Open → Failure → Open
```

### Exponential Backoff
```
Retry 1: 2 seconds delay
Retry 2: 4 seconds delay
Retry 3: 8 seconds delay
```

### Fault Isolation
- COM operations: Separate STA thread
- Timeout: 30 seconds (configurable)
- Abort handling: Safe cleanup on timeout

### Watchdog
- Task Scheduler: Runs every 5 minutes
- Check: Query service heartbeat
- Action: Restart service if unhealthy (> 10 minutes since last heartbeat)

## Deployment Architecture

### Service Installation
```
1. Build Release
2. Publish to folder
3. Install Windows Service (sc.exe)
4. Configure recovery policy
5. Start service
```

### Tray App Deployment
```
1. Build Release
2. Publish to folder
3. Add to Windows Startup folder
4. Create desktop shortcut
```

### Backend API Deployment
```
1. Build Release
2. Publish to folder or container
3. Configure connection strings
4. Deploy to Azure App Service / IIS / Docker
```

## Scalability Considerations

### Service Scalability
- Single instance per machine
- Handles unlimited documents (limited by memory)
- Backup operations are I/O bound (not CPU intensive)

### Backend API Scalability
- Stateless design (can scale horizontally)
- Database connection pooling
- Caching for license validation (Redis recommended for production)
- Rate limiting (recommended for production)

### Storage Scalability
- Local file system for backups
- Configurable retention policy
- Cloud storage integration (future enhancement)

## Monitoring & Observability

### Logging
- **Service**: Serilog to file (rolling daily, 30 day retention)
- **Tray App**: Console output
- **Backend API**: ASP.NET Core logging to file/console

### Metrics
- Backup count (per cycle)
- Active document count
- Service uptime
- IPC message count
- License validation rate

### Health Checks
- Service heartbeat (updated every cycle)
- IPC connectivity (Tray App checks every 5 seconds)
- Backend API health endpoint (recommended)

## Future Enhancements

1. **Cloud Backup**: Sync local backups to cloud storage
2. **Multi-Platform**: Support for macOS and Linux
3. **Additional Applications**: Support for Excel, PowerPoint, etc.
4. **Version Diffing**: Show changes between backup versions
5. **Backup Scheduling**: Customizable backup schedules
6. **Email Notifications**: Alert on backup failures
7. **Web Dashboard**: Web-based status and management
8. **Mobile App**: iOS/Android app for backup management
