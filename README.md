# WMS AI - Intelligent Warehouse Management System

An AI-powered warehouse management system built with Domain-Driven Design (DDD), Event-Driven Architecture, and CQRS patterns. The system leverages multi-agent AI workflows to automate quality control inspections and optimize warehouse operations.

## Value Proposition

WMS AI transforms traditional warehouse operations by:

- **AI-Driven Quality Control**: Automated inspection workflows using dual-agent collaboration (Evidence Gap Agent + Inspection Decision Agent)
- **Event-Driven Architecture**: Real-time cross-service communication with eventual consistency via CAP + RabbitMQ
- **Multi-Tenant Support**: Isolated data and operations per tenant with warehouse-level granularity
- **Scalable Microservices**: Independent bounded contexts with clean separation of concerns
- **Modern Tech Stack**: .NET 10, PostgreSQL, Vue 3, Aspire orchestration

## Architecture Overview

### Design Principles

- **Domain-Driven Design (DDD)**: Bounded contexts for Platform, Inbound, and AiGateway
- **Event-Driven Architecture**: Asynchronous communication via domain events
- **CQRS**: Separate read/write models where beneficial
- **Clean Architecture**: Domain → Application → Infrastructure → Host layers

### Bounded Contexts

1. **Platform**: Tenant, Warehouse, User, and Membership management
2. **Inbound**: Inbound notices, receipts, QC tasks, and decisions
3. **AiGateway**: AI workflows, MAF sessions, inspection runs, model configuration
4. **Operations**: Background jobs, scheduled tasks (Hangfire)
5. **Gateway**: YARP reverse proxy with authentication and routing

## Technology Stack

### Backend
- **.NET 10**: Latest C# features, minimal APIs, native AOT ready
- **PostgreSQL 16**: Primary database with JSONB support
- **Entity Framework Core**: ORM with migrations and interceptors
- **CAP**: Distributed transaction solution with eventual consistency
- **RabbitMQ**: Message broker for event bus
- **Hangfire**: Background job processing
- **YARP**: Reverse proxy for API gateway

### Frontend
- **Vue 3**: Composition API with TypeScript
- **Element Plus**: UI component library
- **Pinia**: State management
- **Axios**: HTTP client
- **Vite**: Build tool and dev server

### Infrastructure
- **.NET Aspire**: Orchestration and service discovery
- **Redis**: Distributed cache and session storage
- **MinIO**: S3-compatible object storage
- **Nacos**: Configuration center and service registry

## Quick Start

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- Docker Desktop
- PostgreSQL 16 (or use Docker)

### Run with Aspire

```bash
# Clone the repository
git clone <repository-url>
cd wms-ai

# Start infrastructure and services
cd src/AppHost/WmsAi.AppHost
dotnet run

# Access Aspire Dashboard
# http://localhost:15888
```

### Run Frontend

```bash
cd web/wms-ai-web
npm install
npm run dev

# Access at http://localhost:5173
```

### Access Points

- **Aspire Dashboard**: http://localhost:15888
- **Frontend**: http://localhost:5173
- **Gateway**: http://localhost:5000
- **Hangfire Dashboard**: http://operations:8080/hangfire
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **MinIO Console**: http://localhost:9001 (minioadmin/minioadmin)
- **Nacos Console**: http://localhost:8848/nacos (nacos/nacos)

## Project Structure

```
wms-ai/
├── src/
│   ├── AppHost/                    # Aspire orchestration
│   │   └── WmsAi.AppHost/
│   ├── Platform/                   # Platform bounded context
│   │   ├── WmsAi.Platform.Domain/
│   │   ├── WmsAi.Platform.Application/
│   │   ├── WmsAi.Platform.Infrastructure/
│   │   └── WmsAi.Platform.Host/
│   ├── Inbound/                    # Inbound bounded context
│   │   ├── WmsAi.Inbound.Domain/
│   │   ├── WmsAi.Inbound.Application/
│   │   ├── WmsAi.Inbound.Infrastructure/
│   │   └── WmsAi.Inbound.Host/
│   ├── AiGateway/                  # AI Gateway bounded context
│   │   ├── WmsAi.AiGateway.Domain/
│   │   ├── WmsAi.AiGateway.Application/
│   │   ├── WmsAi.AiGateway.Infrastructure/
│   │   └── WmsAi.AiGateway.Host/
│   ├── Operations/                 # Background jobs
│   │   └── WmsAi.Operations.Host/
│   ├── Gateway/                    # YARP gateway
│   │   └── WmsAi.Gateway.Host/
│   ├── BuildingBlocks/             # Shared libraries
│   │   ├── WmsAi.SharedKernel/
│   │   └── WmsAi.Contracts/
│   └── ServiceDefaults/            # Aspire defaults
│       └── WmsAi.ServiceDefaults/
├── web/
│   └── wms-ai-web/                 # Vue 3 frontend
├── tests/
│   ├── WmsAi.Platform.Tests/
│   ├── WmsAi.Inbound.Tests/
│   ├── WmsAi.Integration.Tests/
│   └── WmsAi.ArchitectureTests/
└── docs/                           # Documentation
    ├── architecture/
    ├── api/
    ├── runbooks/
    └── deployment/
```

## Key Features

### Multi-Agent AI Workflow (MAF)
- **Evidence Gap Agent**: Analyzes QC evidence completeness
- **Inspection Decision Agent**: Makes quality control decisions
- **Session Management**: Persistent conversation state with checkpoints
- **Human-in-the-Loop**: Manual review when AI confidence is low

### Event-Driven Integration
- **CAP Framework**: Transactional outbox pattern for reliable event publishing
- **RabbitMQ**: Durable message delivery across services
- **Event Contracts**: Versioned event schemas in WmsAi.Contracts
- **Eventual Consistency**: Cross-database consistency without distributed transactions

### Multi-Tenancy
- **Tenant Isolation**: Data segregation at database level
- **Warehouse Context**: Operations scoped to specific warehouses
- **User Memberships**: Role-based access per tenant/warehouse
- **Execution Context**: Automatic tenant/warehouse/user injection via middleware

## Documentation

- [Architecture Documentation](docs/architecture/README.md) - Detailed architecture, C4 diagrams, event flows
- [API Documentation](docs/api/openapi.yaml) - OpenAPI 3.0 specification
- [Local Development Guide](docs/runbooks/local-development.md) - Setup and development workflow
- [Troubleshooting Guide](docs/runbooks/troubleshooting.md) - Common issues and solutions
- [Database Migrations](docs/runbooks/database-migrations.md) - Migration management
- [Monitoring Guide](docs/runbooks/monitoring.md) - Dashboards and metrics
- [Release Checklist](docs/deployment/release-checklist.md) - Deployment procedures

## Development Workflow

### Running Tests

```bash
# Unit tests
dotnet test tests/WmsAi.Platform.Tests
dotnet test tests/WmsAi.Inbound.Tests

# Integration tests
dotnet test tests/WmsAi.Integration.Tests

# Architecture tests
dotnet test tests/WmsAi.ArchitectureTests
```

### Database Migrations

```bash
# Create migration
cd src/Platform/WmsAi.Platform.Infrastructure
dotnet ef migrations add MigrationName --context UserDbContext

# Apply migrations (automatic on startup)
# Or manually:
dotnet ef database update --context UserDbContext
```

### Code Quality

```bash
# Format code
dotnet format

# Analyze code
dotnet build /p:TreatWarningsAsErrors=true
```

## Contributing

1. Follow DDD principles and clean architecture
2. Write unit tests for domain logic
3. Use domain events for cross-aggregate communication
4. Keep bounded contexts independent
5. Document public APIs and complex workflows

## License

[Your License Here]

## Support

For issues and questions, please refer to the [Troubleshooting Guide](docs/runbooks/troubleshooting.md) or open an issue.
