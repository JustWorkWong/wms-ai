# Local Development Guide

## Prerequisites

### Required Software

1. **.NET 10 SDK**
   ```bash
   # Download from https://dotnet.microsoft.com/download/dotnet/10.0
   dotnet --version  # Should show 10.x.x
   ```

2. **Node.js 20+**
   ```bash
   # Download from https://nodejs.org/
   node --version  # Should show v20.x.x or higher
   npm --version
   ```

3. **Docker Desktop**
   ```bash
   # Download from https://www.docker.com/products/docker-desktop
   docker --version
   docker compose version
   ```

4. **Git**
   ```bash
   git --version
   ```

### Optional Tools

- **Visual Studio 2025** or **JetBrains Rider** for C# development
- **VS Code** with C# Dev Kit extension
- **PostgreSQL Client** (pgAdmin, DBeaver, or psql CLI)
- **Postman** or **Insomnia** for API testing

## Initial Setup

### 1. Clone Repository

```bash
git clone <repository-url>
cd wms-ai
```

### 2. Verify Project Structure

```bash
ls -la
# Should see: src/, web/, tests/, docs/, wms-ai.sln
```

### 3. Restore Dependencies

```bash
# Restore .NET packages
dotnet restore

# Restore frontend packages
cd web/wms-ai-web
npm install
cd ../..
```

## Running with Aspire

Aspire orchestrates all services and infrastructure automatically.

### Start Aspire AppHost

```bash
cd src/AppHost/WmsAi.AppHost
dotnet run
```

This will:
- Start PostgreSQL container (port 5432)
- Start Redis container (port 6379)
- Start RabbitMQ container (ports 5672, 15672)
- Start MinIO container (ports 9000, 9001)
- Start Nacos container (ports 8848, 9848)
- Launch Platform service
- Launch Inbound service
- Launch AiGateway service
- Launch Operations service
- Launch Gateway service

### Access Aspire Dashboard

Open browser to: **http://localhost:15888**

The dashboard shows:
- Service status and logs
- Resource health checks
- Distributed tracing
- Metrics and performance

### Service Endpoints (via Aspire)

Aspire assigns dynamic ports. Check the dashboard for actual URLs, typically:

- **Gateway**: http://localhost:5000
- **Platform**: http://localhost:5001
- **Inbound**: http://localhost:5002
- **AiGateway**: http://localhost:5003
- **Operations**: http://localhost:5004

## Running Frontend

In a separate terminal:

```bash
cd web/wms-ai-web
npm run dev
```

Access at: **http://localhost:5173**

### Frontend Development

```bash
# Run with hot reload
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run tests
npm run test
```

## Database Setup

### Automatic Initialization

Databases are created automatically on first run via:
- `PlatformDatabaseInitializer.InitializeAsync()` → UserDb
- `BusinessDatabaseInitializer.InitializeAsync()` → BusinessDb
- `AiGatewayDatabaseInitializer.InitializeAsync()` → AiDb

### Manual Database Access

```bash
# Connect to PostgreSQL container
docker exec -it <postgres-container-id> psql -U postgres

# List databases
\l

# Connect to specific database
\c UserDb
\c BusinessDb
\c AiDb

# List tables
\dt

# Query data
SELECT * FROM tenants;
```

### Connection Strings

Default connection strings (configured in Aspire):

```
UserDb: Host=localhost;Database=UserDb;Username=postgres;Password=postgres
BusinessDb: Host=localhost;Database=BusinessDb;Username=postgres;Password=postgres
AiDb: Host=localhost;Database=AiDb;Username=postgres;Password=postgres
```

## Creating Database Migrations

### Platform (UserDb)

```bash
cd src/Platform/WmsAi.Platform.Infrastructure

# Create migration
dotnet ef migrations add MigrationName --context UserDbContext

# Apply migration
dotnet ef database update --context UserDbContext

# Remove last migration (if not applied)
dotnet ef migrations remove --context UserDbContext
```

### Inbound (BusinessDb)

```bash
cd src/Inbound/WmsAi.Inbound.Infrastructure

dotnet ef migrations add MigrationName --context BusinessDbContext
dotnet ef database update --context BusinessDbContext
```

### AiGateway (AiDb)

```bash
cd src/AiGateway/WmsAi.AiGateway.Infrastructure

dotnet ef migrations add MigrationName --context AiDbContext
dotnet ef database update --context AiDbContext
```

## Seed Data

### Manual Seed via API

```bash
# Create tenant
curl -X POST http://localhost:5000/api/platform/tenants \
  -H "Content-Type: application/json" \
  -d '{
    "code": "ACME",
    "name": "Acme Corporation"
  }'

# Create inbound notice
curl -X POST http://localhost:5000/api/inbound/notices \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "ACME",
    "warehouseId": "WH001",
    "noticeNo": "ASN-2024-001",
    "lines": [
      {
        "skuCode": "SKU-12345",
        "expectedQuantity": "100"
      }
    ]
  }'
```

### Automated Seed (Future Enhancement)

Create seed data scripts in:
- `src/Platform/WmsAi.Platform.Infrastructure/Persistence/Seeds/`
- `src/Inbound/WmsAi.Inbound.Infrastructure/Persistence/Seeds/`

## Common Development Tasks

### Run Specific Service

```bash
# Run Platform service only
cd src/Platform/WmsAi.Platform.Host
dotnet run

# Run with specific port
dotnet run --urls "http://localhost:5001"
```

### Run Tests

```bash
# All tests
dotnet test

# Specific test project
dotnet test tests/WmsAi.Platform.Tests
dotnet test tests/WmsAi.Inbound.Tests
dotnet test tests/WmsAi.Integration.Tests

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# Watch mode
dotnet watch test --project tests/WmsAi.Platform.Tests
```

### View Logs

```bash
# Via Aspire Dashboard
# Navigate to http://localhost:15888 → Logs tab

# Or via console output when running services directly
```

### Debug Services

#### Visual Studio / Rider

1. Open `wms-ai.sln`
2. Set `WmsAi.AppHost` as startup project
3. Press F5 to debug
4. Set breakpoints in any service

#### VS Code

1. Open workspace
2. Use `.vscode/launch.json` configuration
3. Select "Debug Aspire AppHost"
4. Press F5

### Hot Reload

.NET 10 supports hot reload:

```bash
dotnet watch run --project src/Platform/WmsAi.Platform.Host
```

Changes to code will automatically reload without restart.

## Infrastructure Management

### RabbitMQ Management

Access: **http://localhost:15672**
- Username: `guest`
- Password: `guest`

Features:
- View queues and exchanges
- Monitor message rates
- Inspect message contents
- Manage connections

### Hangfire Dashboard

Access: **http://localhost:5004/hangfire** (via Operations service)

Features:
- View scheduled jobs
- Monitor job execution
- Retry failed jobs
- View job history

### MinIO Console

Access: **http://localhost:9001**
- Username: `minioadmin`
- Password: `minioadmin`

Features:
- Create buckets
- Upload/download files
- Manage access policies
- View storage metrics

### Nacos Console

Access: **http://localhost:8848/nacos**
- Username: `nacos`
- Password: `nacos`

Features:
- Manage configuration
- Service discovery
- Namespace management

### Redis CLI

```bash
# Connect to Redis container
docker exec -it <redis-container-id> redis-cli

# Common commands
KEYS *
GET key_name
FLUSHALL  # Clear all data (dev only!)
```

## Environment Configuration

### appsettings.json

Each service has its own `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "UserDb": "Host=localhost;Database=UserDb;Username=postgres;Password=postgres",
    "RabbitMQ": "amqp://guest:guest@localhost:5672"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### appsettings.Development.json

Override settings for local development:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### Environment Variables

Set via Aspire or directly:

```bash
export ConnectionStrings__UserDb="Host=localhost;Database=UserDb;..."
export ASPNETCORE_ENVIRONMENT=Development
```

## API Testing

### Using curl

```bash
# Health check
curl http://localhost:5000/health

# Create tenant
curl -X POST http://localhost:5000/api/platform/tenants \
  -H "Content-Type: application/json" \
  -d '{"code":"TEST","name":"Test Tenant"}'

# Get QC tasks
curl "http://localhost:5000/api/inbound/qc/tasks?tenantId=ACME&warehouseId=WH001"
```

### Using Postman

Import OpenAPI spec:
1. Open Postman
2. Import → Link → `http://localhost:5000/swagger/v1/swagger.json`
3. Create environment with base URL: `http://localhost:5000`

## Troubleshooting

See [Troubleshooting Guide](troubleshooting.md) for common issues.

### Quick Checks

```bash
# Verify .NET SDK
dotnet --version

# Verify Docker
docker ps

# Check service health
curl http://localhost:5000/health

# View Aspire logs
# Open http://localhost:15888 → Logs tab
```

### Clean Rebuild

```bash
# Clean solution
dotnet clean

# Remove bin/obj folders
find . -name "bin" -o -name "obj" | xargs rm -rf

# Restore and rebuild
dotnet restore
dotnet build
```

### Reset Databases

```bash
# Stop Aspire
# Ctrl+C in AppHost terminal

# Remove Docker volumes
docker volume ls | grep wms-ai
docker volume rm <volume-name>

# Restart Aspire
cd src/AppHost/WmsAi.AppHost
dotnet run
```

## Next Steps

- [Troubleshooting Guide](troubleshooting.md)
- [Database Migrations](database-migrations.md)
- [Monitoring Guide](monitoring.md)
- [API Documentation](../api/openapi.yaml)
