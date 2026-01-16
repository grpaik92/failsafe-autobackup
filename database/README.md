# Database Initialization

This folder contains database schemas and migration scripts for the Failsafe AutoBackup application.

## Structure

- **schemas/** - Database schema definitions
  - `init_schema.sql` - Initial database schema for PostgreSQL/SQLite
  
- **migrations/** - Database migration scripts (version-controlled schema changes)
  - Migrations will be added here as the schema evolves

## Database Support

The application supports both:
- **SQLite** - For development and single-instance deployments
- **PostgreSQL** - For production and multi-tenant deployments

## Schema Overview

### Core Tables

1. **Users** - User account information
2. **Subscriptions** - User subscription management
3. **Devices** - Registered device tracking
4. **BackupSessions** (Optional) - Backup operation tracking
5. **AuditLog** (Optional) - Security and compliance logging

## Initialization

### SQLite (Development)

```bash
sqlite3 failsafeautobackup.db < schemas/init_schema.sql
```

### PostgreSQL (Production)

```bash
createdb failsafeautobackup
psql -d failsafeautobackup -f schemas/init_schema.sql
```

## Security Considerations

1. Never commit connection strings with credentials to source control
2. Use encrypted connections (SSL/TLS) for PostgreSQL
3. Database user should have minimal required permissions
4. Regular automated backups of the database
