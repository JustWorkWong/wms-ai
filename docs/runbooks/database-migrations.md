# Database Migrations Guide

## Overview

WMS AI uses Entity Framework Core migrations to manage database schema changes across three databases:

- **UserDb**: Platform bounded context (tenants, warehouses, users, memberships)
- **BusinessDb**: Inbound bounded context (notices, receipts, QC tasks/decisions)
- **AiDb**: AiGateway bounded context (MAF sessions, workflows, inspections, model config)

## Migration Strategy

### Development Environment

- Use `EnsureCreatedAsync()` for rapid prototyping
- Create migrations for schema changes
- Apply migrations automatically on startup

### Production Environment

- Always use migrations (never `EnsureCreated()`)
- Apply migrations during deployment window
- Test migrations on staging first
- Have rollback plan ready

## Creating Migrations

### Platform (UserDb)

```bash
cd src/Platform/WmsAi.Platform.Infrastructure

# Create new migration
dotnet ef migrations add AddUserEmailColumn --context UserDbContext

# View migration SQL
dotnet ef migrations script --context UserDbContext

# Apply migration
dotnet ef database update --context UserDbContext

# Rollback to specific migration
dotnet ef database update PreviousMigrationName --context UserDbContext

# Remove last migration (if not applied)
dotnet ef migrations remove --context UserDbContext
```

### Inbound (BusinessDb)

```bash
cd src/Inbound/WmsAi.Inbound.Infrastructure

# Create migration
dotnet ef migrations add AddReceiptStatusIndex --context BusinessDbContext

# Apply migration
dotnet ef database update --context BusinessDbContext
```

### AiGateway (AiDb)

```bash
cd src/AiGateway/WmsAi.AiGateway.Infrastructure

# Create migration
dotnet ef migrations add AddModelProviderConfig --context AiDbContext

# Apply migration
dotnet ef database update --context AiDbContext
```

## Migration Naming Conventions

Use descriptive names that indicate the change:

- `AddColumnName` - Adding new column
- `RemoveColumnName` - Removing column
- `RenameOldToNew` - Renaming column/table
- `AddIndexOnColumn` - Adding index
- `CreateTableName` - Creating new table
- `AlterColumnType` - Changing column type

Examples:
```bash
dotnet ef migrations add AddTenantLogoUrl --context UserDbContext
dotnet ef migrations add AddQcTaskPriorityColumn --context BusinessDbContext
dotnet ef migrations add CreateMafToolCallLogsTable --context AiDbContext
```

## Viewing Migration History

### Check Applied Migrations

```bash
# List all migrations
dotnet ef migrations list --context UserDbContext

# Check database migration history
dotnet ef migrations list --context UserDbContext --connection "Host=localhost;Database=UserDb;Username=postgres;Password=postgres"
```

### View Migration SQL

```bash
# Generate SQL for all migrations
dotnet ef migrations script --context UserDbContext

# Generate SQL for specific range
dotnet ef migrations script FromMigration ToMigration --context UserDbContext

# Generate SQL for last migration
dotnet ef migrations script --context UserDbContext --output migration.sql
```

## Applying Migrations in Production

### Option 1: Automatic on Startup (Current Approach)

```csharp
// In Program.cs
await PlatformDatabaseInitializer.InitializeAsync(app.Services);

// In DatabaseInitializer
public static async Task InitializeAsync(IServiceProvider serviceProvider)
{
    await using var scope = serviceProvider.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    
    // Development: Creates schema if not exists
    await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    
    // Production: Should use migrations instead
    // await dbContext.Database.MigrateAsync(cancellationToken);
}
```

### Option 2: Manual Migration Script

```bash
# Generate migration SQL
cd src/Platform/WmsAi.Platform.Infrastructure
dotnet ef migrations script --context UserDbContext --idempotent --output userdb-migration.sql

cd src/Inbound/WmsAi.Inbound.Infrastructure
dotnet ef migrations script --context BusinessDbContext --idempotent --output businessdb-migration.sql

cd src/AiGateway/WmsAi.AiGateway.Infrastructure
dotnet ef migrations script --context AiDbContext --idempotent --output aidb-migration.sql

# Apply manually via psql
psql -h production-host -U postgres -d UserDb -f userdb-migration.sql
psql -h production-host -U postgres -d BusinessDb -f businessdb-migration.sql
psql -h production-host -U postgres -d AiDb -f aidb-migration.sql
```

### Option 3: Migration Tool/Script

```bash
# Create migration runner script
cat > migrate.sh << 'EOF'
#!/bin/bash
set -e

echo "Applying UserDb migrations..."
dotnet ef database update --context UserDbContext --project src/Platform/WmsAi.Platform.Infrastructure

echo "Applying BusinessDb migrations..."
dotnet ef database update --context BusinessDbContext --project src/Inbound/WmsAi.Inbound.Infrastructure

echo "Applying AiDb migrations..."
dotnet ef database update --context AiDbContext --project src/AiGateway/WmsAi.AiGateway.Infrastructure

echo "All migrations applied successfully!"
EOF

chmod +x migrate.sh
./migrate.sh
```

## Rollback Procedures

### Rollback to Previous Migration

```bash
# List migrations to find target
dotnet ef migrations list --context UserDbContext

# Rollback to specific migration
dotnet ef database update TargetMigrationName --context UserDbContext

# Rollback all migrations (empty database)
dotnet ef database update 0 --context UserDbContext
```

### Generate Rollback SQL

```bash
# Generate SQL to rollback from ToMigration to FromMigration
dotnet ef migrations script ToMigration FromMigration --context UserDbContext --output rollback.sql

# Apply rollback
psql -h localhost -U postgres -d UserDb -f rollback.sql
```

### Emergency Rollback

If migration fails in production:

1. **Stop affected services**
   ```bash
   # Stop services to prevent data corruption
   kubectl scale deployment platform --replicas=0
   ```

2. **Restore database backup**
   ```bash
   # Restore from backup taken before migration
   pg_restore -h localhost -U postgres -d UserDb backup.dump
   ```

3. **Redeploy previous version**
   ```bash
   # Deploy previous working version
   kubectl rollout undo deployment/platform
   ```

## Data Seeding

### Seed Data in Migrations

```csharp
// In migration Up() method
migrationBuilder.InsertData(
    table: "tenants",
    columns: new[] { "Id", "Code", "Name", "Status", "CreatedAt", "UpdatedAt", "Version" },
    values: new object[] 
    { 
        Guid.NewGuid(), 
        "DEMO", 
        "Demo Tenant", 
        "Active", 
        DateTime.UtcNow, 
        DateTime.UtcNow, 
        1 
    });
```

### Separate Seed Data Scripts

```bash
# Create seed data directory
mkdir -p src/Platform/WmsAi.Platform.Infrastructure/Persistence/Seeds

# Create seed script
cat > src/Platform/WmsAi.Platform.Infrastructure/Persistence/Seeds/SeedTenants.sql << 'EOF'
INSERT INTO tenants (id, code, name, status, created_at, updated_at, version)
VALUES 
  (gen_random_uuid(), 'ACME', 'Acme Corporation', 'Active', NOW(), NOW(), 1),
  (gen_random_uuid(), 'GLOBEX', 'Globex Corporation', 'Active', NOW(), NOW(), 1)
ON CONFLICT (code) DO NOTHING;
EOF

# Apply seed data
psql -h localhost -U postgres -d UserDb -f src/Platform/WmsAi.Platform.Infrastructure/Persistence/Seeds/SeedTenants.sql
```

### Programmatic Seeding

```csharp
public static class DatabaseSeeder
{
    public static async Task SeedAsync(UserDbContext context)
    {
        if (!await context.Tenants.AnyAsync())
        {
            var tenant = Tenant.Create("DEMO", "Demo Tenant");
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
        }
    }
}

// In Program.cs
await DatabaseSeeder.SeedAsync(dbContext);
```

## Backup and Restore

### Backup Before Migration

```bash
# Backup all databases
pg_dump -h localhost -U postgres -Fc UserDb > userdb-backup-$(date +%Y%m%d-%H%M%S).dump
pg_dump -h localhost -U postgres -Fc BusinessDb > businessdb-backup-$(date +%Y%m%d-%H%M%S).dump
pg_dump -h localhost -U postgres -Fc AiDb > aidb-backup-$(date +%Y%m%d-%H%M%S).dump

# Backup specific tables
pg_dump -h localhost -U postgres -t tenants -t warehouses UserDb > platform-tables-backup.sql
```

### Restore from Backup

```bash
# Restore full database
pg_restore -h localhost -U postgres -d UserDb -c userdb-backup.dump

# Restore specific tables
psql -h localhost -U postgres -d UserDb -f platform-tables-backup.sql
```

### Automated Backup Script

```bash
cat > backup-databases.sh << 'EOF'
#!/bin/bash
BACKUP_DIR="./backups"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)

mkdir -p $BACKUP_DIR

echo "Backing up databases..."
pg_dump -h localhost -U postgres -Fc UserDb > $BACKUP_DIR/userdb-$TIMESTAMP.dump
pg_dump -h localhost -U postgres -Fc BusinessDb > $BACKUP_DIR/businessdb-$TIMESTAMP.dump
pg_dump -h localhost -U postgres -Fc AiDb > $BACKUP_DIR/aidb-$TIMESTAMP.dump

echo "Backup completed: $BACKUP_DIR/*-$TIMESTAMP.dump"

# Keep only last 7 days of backups
find $BACKUP_DIR -name "*.dump" -mtime +7 -delete
EOF

chmod +x backup-databases.sh
```

## Migration Best Practices

### 1. Always Test Migrations

```bash
# Test on local database
dotnet ef database update --context UserDbContext

# Test on staging environment
dotnet ef database update --context UserDbContext --connection "Host=staging;..."

# Verify data integrity
psql -h localhost -U postgres -d UserDb -c "SELECT COUNT(*) FROM tenants;"
```

### 2. Use Idempotent Scripts

```bash
# Generate idempotent SQL (can be run multiple times)
dotnet ef migrations script --context UserDbContext --idempotent
```

### 3. Avoid Breaking Changes

- Don't remove columns immediately (mark as obsolete first)
- Add new columns as nullable initially
- Use multi-phase migrations for renames
- Maintain backward compatibility

### 4. Document Complex Migrations

```csharp
/// <summary>
/// Migration: Split FullName into FirstName and LastName
/// Phase 1: Add new columns
/// Phase 2: Migrate data (separate deployment)
/// Phase 3: Remove old column (future migration)
/// </summary>
public partial class SplitUserFullName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add new columns as nullable
        migrationBuilder.AddColumn<string>(
            name: "FirstName",
            table: "users",
            maxLength: 128,
            nullable: true);
            
        migrationBuilder.AddColumn<string>(
            name: "LastName",
            table: "users",
            maxLength: 128,
            nullable: true);
    }
}
```

### 5. Monitor Migration Performance

```bash
# Time migration execution
time dotnet ef database update --context UserDbContext

# Check migration impact
psql -h localhost -U postgres -d UserDb -c "EXPLAIN ANALYZE SELECT * FROM tenants;"
```

## Troubleshooting Migrations

### Issue: "Migration already applied"

```bash
# Check migration history
dotnet ef migrations list --context UserDbContext

# If migration is in database but not in code, remove from database
DELETE FROM __EFMigrationsHistory WHERE MigrationId = 'MigrationName';
```

### Issue: "Pending model changes"

```bash
# Create migration for pending changes
dotnet ef migrations add PendingChanges --context UserDbContext
```

### Issue: "Migration fails halfway"

```bash
# Check database state
psql -h localhost -U postgres -d UserDb

# Manually fix issues
# Then retry migration
dotnet ef database update --context UserDbContext
```

### Issue: "Cannot connect to database"

```bash
# Verify connection string
dotnet ef database update --context UserDbContext --connection "Host=localhost;Database=UserDb;Username=postgres;Password=postgres" --verbose
```

## Production Migration Checklist

- [ ] Create database backup
- [ ] Test migration on staging
- [ ] Generate migration SQL script
- [ ] Review SQL for performance impact
- [ ] Schedule maintenance window
- [ ] Notify stakeholders
- [ ] Stop affected services
- [ ] Apply migration
- [ ] Verify data integrity
- [ ] Start services
- [ ] Monitor for errors
- [ ] Keep backup for 30 days
