-- ============================================================================
-- Failsafe AutoBackup Database Schema
-- Database: PostgreSQL / SQLite compatible
-- Version: 1.0
-- ============================================================================

-- Users Table
-- Stores user account information
CREATE TABLE IF NOT EXISTS Users (
    Id TEXT PRIMARY KEY,
    Email TEXT UNIQUE NOT NULL,
    ClioUserId TEXT UNIQUE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastLoginAt DATETIME,
    IsActive BOOLEAN NOT NULL DEFAULT 1,
    CONSTRAINT unique_email UNIQUE (Email)
);

CREATE INDEX IF NOT EXISTS idx_users_email ON Users(Email);
CREATE INDEX IF NOT EXISTS idx_users_clio_id ON Users(ClioUserId);

-- Subscriptions Table
-- Manages user subscription status and device limits
CREATE TABLE IF NOT EXISTS Subscriptions (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    StripeSubscriptionId TEXT UNIQUE,
    StripeCustomerId TEXT,
    Status TEXT NOT NULL CHECK (Status IN ('active', 'inactive', 'expired', 'cancelled')),
    StartDate DATETIME NOT NULL,
    EndDate DATETIME,
    MaxDevices INTEGER NOT NULL DEFAULT 1,
    PlanType TEXT NOT NULL DEFAULT 'basic',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_subscriptions_user ON Subscriptions(UserId);
CREATE INDEX IF NOT EXISTS idx_subscriptions_stripe_id ON Subscriptions(StripeSubscriptionId);
CREATE INDEX IF NOT EXISTS idx_subscriptions_status ON Subscriptions(Status);

-- Devices Table
-- Tracks registered devices per user
CREATE TABLE IF NOT EXISTS Devices (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    DeviceFingerprint TEXT NOT NULL,
    DeviceName TEXT NOT NULL,
    RegisteredAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastSeenAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT 1,
    OperatingSystem TEXT,
    AppVersion TEXT,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT unique_device_fingerprint UNIQUE (DeviceFingerprint)
);

CREATE INDEX IF NOT EXISTS idx_devices_user ON Devices(UserId);
CREATE INDEX IF NOT EXISTS idx_devices_fingerprint ON Devices(DeviceFingerprint);
CREATE INDEX IF NOT EXISTS idx_devices_active ON Devices(IsActive);

-- Backup Sessions Table (Optional - for analytics)
-- Tracks backup operations for monitoring and analytics
CREATE TABLE IF NOT EXISTS BackupSessions (
    Id TEXT PRIMARY KEY,
    DeviceId TEXT NOT NULL,
    StartTime DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    EndTime DATETIME,
    DocumentsBackedUp INTEGER DEFAULT 0,
    Status TEXT CHECK (Status IN ('running', 'completed', 'failed')),
    ErrorMessage TEXT,
    FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_backup_sessions_device ON BackupSessions(DeviceId);
CREATE INDEX IF NOT EXISTS idx_backup_sessions_start_time ON BackupSessions(StartTime);

-- Audit Log Table (Optional)
-- Stores important events for security and compliance
CREATE TABLE IF NOT EXISTS AuditLog (
    Id TEXT PRIMARY KEY,
    UserId TEXT,
    DeviceId TEXT,
    Action TEXT NOT NULL,
    Details TEXT,
    IpAddress TEXT,
    Timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL,
    FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_audit_log_user ON AuditLog(UserId);
CREATE INDEX IF NOT EXISTS idx_audit_log_device ON AuditLog(DeviceId);
CREATE INDEX IF NOT EXISTS idx_audit_log_timestamp ON AuditLog(Timestamp);

-- ============================================================================
-- Views (Optional)
-- ============================================================================

-- Active Subscriptions View
CREATE VIEW IF NOT EXISTS ActiveSubscriptions AS
SELECT 
    u.Id as UserId,
    u.Email,
    s.Id as SubscriptionId,
    s.Status,
    s.MaxDevices,
    s.PlanType,
    COUNT(d.Id) as RegisteredDevices
FROM Users u
INNER JOIN Subscriptions s ON u.Id = s.UserId
LEFT JOIN Devices d ON u.Id = d.UserId AND d.IsActive = 1
WHERE s.Status = 'active' AND (s.EndDate IS NULL OR s.EndDate > CURRENT_TIMESTAMP)
GROUP BY u.Id, u.Email, s.Id, s.Status, s.MaxDevices, s.PlanType;
