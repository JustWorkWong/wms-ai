# WMS AI Architecture Documentation

## Overview

WMS AI is built using Domain-Driven Design (DDD), Event-Driven Architecture (EDA), and CQRS patterns. The system is decomposed into bounded contexts that communicate asynchronously via domain events.

## C4 Model Diagrams

### Level 1: System Context

```mermaid
graph TB
    User[Warehouse User]
    Admin[System Admin]
    
    WmsAi[WMS AI System]
    
    ExtWms[External WMS]
    ExtErp[External ERP]
    
    User -->|Manages operations| WmsAi
    Admin -->|Configures system| WmsAi
    WmsAi -->|Syncs data| ExtWms
    WmsAi -->|Reports to| ExtErp
    
    style WmsAi fill:#1168bd,color:#fff
```

### Level 2: Container Diagram

```mermaid
graph TB
    subgraph "Frontend"
        Web[Vue 3 SPA<br/>Port 5173]
    end
    
    subgraph "API Gateway"
        Gateway[YARP Gateway<br/>Port 5000]
    end
    
    subgraph "Backend Services"
        Platform[Platform Service<br/>Tenants, Users]
        Inbound[Inbound Service<br/>Notices, Receipts, QC]
        AiGateway[AI Gateway Service<br/>Workflows, Agents]
        Operations[Operations Service<br/>Background Jobs]
    end
    
    subgraph "Infrastructure"
        Postgres[(PostgreSQL<br/>UserDb, BusinessDb, AiDb)]
        RabbitMQ[RabbitMQ<br/>Event Bus]
        Redis[(Redis<br/>Cache)]
        MinIO[(MinIO<br/>Object Storage)]
        Nacos[Nacos<br/>Config Center]
    end
    
    Web -->|HTTP/REST| Gateway
    Gateway -->|Routes| Platform
    Gateway -->|Routes| Inbound
    Gateway -->|Routes| AiGateway
    Gateway -->|Routes| Operations
    
    Platform -->|EF Core| Postgres
    Inbound -->|EF Core| Postgres
    AiGateway -->|EF Core| Postgres
    Operations -->|Reads| Postgres
    
    Platform -->|CAP| RabbitMQ
    Inbound -->|CAP| RabbitMQ
    AiGateway -->|CAP| RabbitMQ
    
    Platform -->|Cache| Redis
    Inbound -->|Cache| Redis
    AiGateway -->|Cache| Redis
    
    AiGateway -->|Store files| MinIO
    
    Operations -->|Read config| Nacos
    
    style Gateway fill:#1168bd,color:#fff
    style Platform fill:#1168bd,color:#fff
    style Inbound fill:#1168bd,color:#fff
    style AiGateway fill:#1168bd,color:#fff
    style Operations fill:#1168bd,color:#fff
```

### Level 3: Component Diagram - Platform Service

```mermaid
graph TB
    subgraph "Platform.Host"
        API[Minimal APIs<br/>Endpoints]
    end
    
    subgraph "Platform.Application"
        TenantHandler[CreateTenantHandler]
        EventPub[Event Publisher]
    end
    
    subgraph "Platform.Domain"
        TenantAgg[Tenant Aggregate]
        WarehouseAgg[Warehouse Aggregate]
        UserAgg[User Aggregate]
        TenantRepo[ITenantRepository]
        WarehouseRepo[IWarehouseRepository]
        UserRepo[IUserRepository]
    end
    
    subgraph "Platform.Infrastructure"
        UserDbContext[UserDbContext<br/>EF Core]
        TenantRepoImpl[TenantRepository]
        CapPublisher[CAP Publisher]
        DomainEventInterceptor[Domain Event Interceptor]
    end
    
    API -->|Calls| TenantHandler
    TenantHandler -->|Uses| TenantAgg
    TenantHandler -->|Uses| TenantRepo
    TenantHandler -->|Publishes| EventPub
    
    TenantRepo -.->|Implements| TenantRepoImpl
    EventPub -.->|Implements| CapPublisher
    
    TenantRepoImpl -->|Uses| UserDbContext
    CapPublisher -->|Uses| UserDbContext
    UserDbContext -->|Intercepts| DomainEventInterceptor
    
    style TenantAgg fill:#f9f,color:#000
    style WarehouseAgg fill:#f9f,color:#000
    style UserAgg fill:#f9f,color:#000
```

### Level 3: Component Diagram - Inbound Service

```mermaid
graph TB
    subgraph "Inbound.Host"
        API[Minimal APIs<br/>Endpoints]
        EventConsumer[Platform Event Consumer]
    end
    
    subgraph "Inbound.Application"
        NoticeHandler[CreateInboundNoticeHandler]
        ReceiptHandler[RecordReceiptHandler]
        QcHandler[FinalizeQcDecisionHandler]
        EventPub[Event Publisher]
    end
    
    subgraph "Inbound.Domain"
        NoticeAgg[InboundNotice Aggregate]
        ReceiptAgg[Receipt Aggregate]
        QcTaskAgg[QcTask Aggregate]
        QcDecisionAgg[QcDecision Aggregate]
        Repos[Repositories]
    end
    
    subgraph "Inbound.Infrastructure"
        BusinessDbContext[BusinessDbContext<br/>EF Core]
        RepoImpl[Repository Implementations]
        CapPublisher[CAP Publisher]
        CapConsumer[CAP Consumer]
    end
    
    API -->|Calls| NoticeHandler
    API -->|Calls| ReceiptHandler
    API -->|Calls| QcHandler
    EventConsumer -->|Handles| CapConsumer
    
    NoticeHandler -->|Uses| NoticeAgg
    ReceiptHandler -->|Uses| ReceiptAgg
    QcHandler -->|Uses| QcDecisionAgg
    
    NoticeHandler -->|Publishes| EventPub
    ReceiptHandler -->|Publishes| EventPub
    QcHandler -->|Publishes| EventPub
    
    Repos -.->|Implements| RepoImpl
    EventPub -.->|Implements| CapPublisher
    
    RepoImpl -->|Uses| BusinessDbContext
    CapPublisher -->|Uses| BusinessDbContext
    CapConsumer -->|Uses| BusinessDbContext
    
    style NoticeAgg fill:#f9f,color:#000
    style ReceiptAgg fill:#f9f,color:#000
    style QcTaskAgg fill:#f9f,color:#000
    style QcDecisionAgg fill:#f9f,color:#000
```

### Level 3: Component Diagram - AI Gateway Service

```mermaid
graph TB
    subgraph "AiGateway.Host"
        SessionAPI[Sessions API<br/>Controller]
        InspectionAPI[Inspections API<br/>Controller]
        EventConsumer[Inbound Event Consumer]
    end
    
    subgraph "AiGateway.Application"
        WorkflowOrch[Workflow Orchestrator]
        EvidenceAgent[Evidence Gap Agent]
        DecisionAgent[Inspection Decision Agent]
        MafService[MAF Persistence Service]
        AgUiService[AG-UI Event Stream Service]
        BusinessFunctions[Inbound Business Functions]
    end
    
    subgraph "AiGateway.Domain"
        SessionAgg[MafSession Aggregate]
        WorkflowAgg[MafWorkflowRun Aggregate]
        InspectionAgg[AiInspectionRun Aggregate]
        ModelConfig[Model Configuration]
        Repos[Repositories]
    end
    
    subgraph "AiGateway.Infrastructure"
        AiDbContext[AiDbContext<br/>EF Core]
        RepoImpl[Repository Implementations]
        AgentImpl[Agent Implementations]
        BusinessApiClient[Business API Client]
    end
    
    SessionAPI -->|Calls| MafService
    SessionAPI -->|Streams| AgUiService
    InspectionAPI -->|Updates| InspectionAgg
    EventConsumer -->|Triggers| WorkflowOrch
    
    WorkflowOrch -->|Uses| EvidenceAgent
    WorkflowOrch -->|Uses| DecisionAgent
    WorkflowOrch -->|Uses| BusinessFunctions
    WorkflowOrch -->|Persists| MafService
    
    EvidenceAgent -->|Creates| SessionAgg
    DecisionAgent -->|Creates| InspectionAgg
    BusinessFunctions -.->|Implements| BusinessApiClient
    
    MafService -->|Uses| Repos
    Repos -.->|Implements| RepoImpl
    RepoImpl -->|Uses| AiDbContext
    
    style SessionAgg fill:#f9f,color:#000
    style WorkflowAgg fill:#f9f,color:#000
    style InspectionAgg fill:#f9f,color:#000
```

## Bounded Context Relationships

```mermaid
graph LR
    Platform[Platform Context<br/>UserDb]
    Inbound[Inbound Context<br/>BusinessDb]
    AiGateway[AI Gateway Context<br/>AiDb]
    
    Platform -->|TenantCreated<br/>WarehouseCreated| Inbound
    Inbound -->|QcTaskCreated<br/>ReceiptRecorded| AiGateway
    AiGateway -->|AiSuggestionCreated| Inbound
    
    style Platform fill:#1168bd,color:#fff
    style Inbound fill:#1168bd,color:#fff
    style AiGateway fill:#1168bd,color:#fff
```

### Context Integration Patterns

1. **Platform → Inbound**: Reference data synchronization
   - Events: `TenantCreatedV1`, `WarehouseCreatedV1`
   - Pattern: Event-carried state transfer
   - Purpose: Maintain tenant/warehouse reference data

2. **Inbound → AiGateway**: Business event triggers
   - Events: `QcTaskCreatedV1`, `ReceiptRecordedV1`
   - Pattern: Domain event notification
   - Purpose: Trigger AI inspection workflows

3. **AiGateway → Inbound**: AI decision feedback
   - Events: `AiSuggestionCreatedV1`
   - Pattern: Command via event
   - Purpose: Provide AI recommendations to business context

## Event Flow Diagrams

### QC Task Creation and AI Inspection Flow

```mermaid
sequenceDiagram
    participant User
    participant Gateway
    participant Inbound
    participant RabbitMQ
    participant AiGateway
    participant Agent
    
    User->>Gateway: POST /api/inbound/receipts
    Gateway->>Inbound: Forward request
    Inbound->>Inbound: Create Receipt
    Inbound->>Inbound: Create QcTask
    Inbound->>RabbitMQ: Publish QcTaskCreatedV1
    Inbound-->>Gateway: 201 Created
    Gateway-->>User: Receipt created
    
    RabbitMQ->>AiGateway: Deliver QcTaskCreatedV1
    AiGateway->>AiGateway: Start InboundInspectionWorkflow
    AiGateway->>AiGateway: Create MafSession
    AiGateway->>Agent: Call EvidenceGapAgent
    Agent->>AiGateway: Evidence analysis
    AiGateway->>Agent: Call InspectionDecisionAgent
    Agent->>AiGateway: Inspection decision
    AiGateway->>AiGateway: Create AiInspectionRun
    AiGateway->>RabbitMQ: Publish AiSuggestionCreatedV1
    
    RabbitMQ->>Inbound: Deliver AiSuggestionCreatedV1
    Inbound->>Inbound: Update QcTask with suggestion
```

### Manual Review Flow (Human-in-the-Loop)

```mermaid
sequenceDiagram
    participant User
    participant Frontend
    participant Gateway
    participant AiGateway
    participant Inbound
    
    AiGateway->>AiGateway: Low confidence decision
    AiGateway->>AiGateway: Pause MafSession
    AiGateway->>AiGateway: Set InspectionStatus=WaitingManualReview
    
    User->>Frontend: Open QC workbench
    Frontend->>Gateway: GET /api/inbound/qc/tasks
    Gateway->>Inbound: Forward request
    Inbound-->>Frontend: QC tasks with AI suggestions
    
    User->>Frontend: Review and decide
    Frontend->>Gateway: POST /api/ai/inspections/{id}/manual-review
    Gateway->>AiGateway: Forward request
    AiGateway->>AiGateway: CompleteManualReview
    AiGateway->>AiGateway: Resume MafSession
    AiGateway-->>Frontend: 200 OK
    
    AiGateway->>AiGateway: Continue workflow
    AiGateway->>Inbound: Finalize QC decision
```

## Database Schema Overview

### UserDb (Platform Context)

```mermaid
erDiagram
    tenants ||--o{ warehouses : "has"
    tenants ||--o{ memberships : "has"
    warehouses ||--o{ memberships : "has"
    users ||--o{ memberships : "has"
    
    tenants {
        uuid id PK
        string code UK
        string name
        string status
        int version
        timestamp created_at
        timestamp updated_at
    }
    
    warehouses {
        uuid id PK
        uuid tenant_id FK
        string code
        string name
        string status
        int version
        timestamp created_at
        timestamp updated_at
    }
    
    users {
        uuid id PK
        string login_name UK
        string status
        int version
        timestamp created_at
        timestamp updated_at
    }
    
    memberships {
        uuid id PK
        uuid tenant_id FK
        uuid warehouse_id FK
        uuid user_id FK
        string role
        string status
        int version
        timestamp created_at
        timestamp updated_at
    }
```

### BusinessDb (Inbound Context)

```mermaid
erDiagram
    inbound_notices ||--o{ inbound_notice_lines : "has"
    inbound_notices ||--o{ receipts : "has"
    receipts ||--o{ receipt_lines : "has"
    receipts ||--o{ qc_tasks : "triggers"
    qc_tasks ||--o{ qc_decisions : "has"
    
    inbound_notices {
        uuid id PK
        string tenant_id
        string warehouse_id
        string notice_no UK
        string status
        int version
        timestamp created_at
        timestamp updated_at
    }
    
    inbound_notice_lines {
        uuid id PK
        uuid inbound_notice_id FK
        string sku_code
        decimal expected_quantity
    }
    
    receipts {
        uuid id PK
        uuid inbound_notice_id FK
        string tenant_id
        string warehouse_id
        string receipt_no UK
        string status
        int version
        timestamp created_at
        timestamp updated_at
    }
    
    receipt_lines {
        uuid id PK
        uuid receipt_id FK
        string sku_code
        decimal received_quantity
    }
    
    qc_tasks {
        uuid id PK
        uuid receipt_id FK
        string tenant_id
        string warehouse_id
        string task_no UK
        string sku_code
        string status
        jsonb ai_suggestion
        int version
        timestamp created_at
        timestamp updated_at
    }
    
    qc_decisions {
        uuid id PK
        uuid qc_task_id FK
        string tenant_id
        string warehouse_id
        string decision
        string reasoning
        string decided_by
        int version
        timestamp created_at
        timestamp updated_at
    }
```

### AiDb (AI Gateway Context)

```mermaid
erDiagram
    maf_sessions ||--o{ maf_messages : "has"
    maf_sessions ||--o{ maf_checkpoints : "has"
    maf_sessions ||--o{ maf_workflow_runs : "has"
    maf_workflow_runs ||--o{ maf_workflow_step_runs : "has"
    maf_workflow_runs ||--o{ ai_inspection_runs : "has"
    
    maf_sessions {
        uuid id PK
        string tenant_id
        string warehouse_id
        string user_id
        string session_type
        string business_object_type
        string business_object_id
        string status
        jsonb agent_session_json
        uuid last_checkpoint_id
        int version
        timestamp created_at
        timestamp updated_at
    }
    
    maf_messages {
        uuid id PK
        uuid session_id FK
        int sequence
        string role
        string message_type
        text content_text
        jsonb content_json
        boolean is_summary
        timestamp created_at
    }
    
    maf_checkpoints {
        uuid id PK
        uuid session_id FK
        int sequence
        string checkpoint_type
        jsonb state_snapshot
        timestamp created_at
    }
    
    maf_workflow_runs {
        uuid id PK
        uuid session_id FK
        string tenant_id
        string warehouse_id
        string workflow_code
        string status
        jsonb input_context
        jsonb output_result
        timestamp started_at
        timestamp completed_at
    }
    
    maf_workflow_step_runs {
        uuid id PK
        uuid workflow_run_id FK
        string step_code
        int sequence
        string status
        jsonb input_data
        jsonb output_data
        timestamp started_at
        timestamp completed_at
    }
    
    ai_inspection_runs {
        uuid id PK
        uuid workflow_run_id FK
        uuid qc_task_id
        string tenant_id
        string warehouse_id
        string status
        string decision
        string reasoning
        decimal confidence_score
        string reviewer_id
        timestamp created_at
        timestamp completed_at
    }
```

## AI Workflow Architecture

### Multi-Agent Framework (MAF) Architecture

```mermaid
graph TB
    subgraph "Workflow Layer"
        WorkflowOrch[Workflow Orchestrator]
        InboundWorkflow[InboundInspectionWorkflow]
    end
    
    subgraph "Agent Layer"
        EvidenceAgent[Evidence Gap Agent]
        DecisionAgent[Inspection Decision Agent]
    end
    
    subgraph "Function Layer"
        BusinessFunctions[Inbound Business Functions]
        GetQcTask[GetQcTaskDetails]
        GetReceipt[GetReceiptDetails]
        GetNotice[GetNoticeDetails]
    end
    
    subgraph "Persistence Layer"
        MafService[MAF Persistence Service]
        SessionRepo[Session Repository]
        WorkflowRepo[Workflow Repository]
        InspectionRepo[Inspection Repository]
    end
    
    subgraph "External Services"
        InboundService[Inbound Service API]
    end
    
    WorkflowOrch -->|Executes| InboundWorkflow
    InboundWorkflow -->|Step 1| EvidenceAgent
    InboundWorkflow -->|Step 2| DecisionAgent
    
    EvidenceAgent -->|Calls| BusinessFunctions
    DecisionAgent -->|Calls| BusinessFunctions
    
    BusinessFunctions -->|Implements| GetQcTask
    BusinessFunctions -->|Implements| GetReceipt
    BusinessFunctions -->|Implements| GetNotice
    
    GetQcTask -->|HTTP| InboundService
    GetReceipt -->|HTTP| InboundService
    GetNotice -->|HTTP| InboundService
    
    InboundWorkflow -->|Persists| MafService
    MafService -->|Uses| SessionRepo
    MafService -->|Uses| WorkflowRepo
    MafService -->|Uses| InspectionRepo
    
    style WorkflowOrch fill:#1168bd,color:#fff
    style EvidenceAgent fill:#f96,color:#fff
    style DecisionAgent fill:#f96,color:#fff
```

### Workflow Execution Flow

```mermaid
stateDiagram-v2
    [*] --> Pending: Event received
    Pending --> Running: Start workflow
    Running --> CheckEvidence: Step 1
    CheckEvidence --> EvidenceComplete: Evidence OK
    CheckEvidence --> WaitingEvidence: Evidence missing
    WaitingEvidence --> CheckEvidence: Evidence provided
    EvidenceComplete --> MakeDecision: Step 2
    MakeDecision --> HighConfidence: Confidence >= 0.8
    MakeDecision --> LowConfidence: Confidence < 0.8
    HighConfidence --> Completed: Auto-approve
    LowConfidence --> WaitingManualReview: Pause for human
    WaitingManualReview --> Completed: Manual decision
    Completed --> [*]
```

### Session State Management

```mermaid
graph LR
    Active[Active Session]
    Paused[Paused Session]
    Completed[Completed Session]
    Failed[Failed Session]
    
    Active -->|Low confidence| Paused
    Active -->|High confidence| Completed
    Active -->|Error| Failed
    Paused -->|Resume| Active
    Paused -->|Manual decision| Completed
    
    style Active fill:#9f9,color:#000
    style Paused fill:#ff9,color:#000
    style Completed fill:#9cf,color:#000
    style Failed fill:#f99,color:#000
```

## Cross-Cutting Concerns

### Authentication & Authorization

- **Gateway Middleware**: `FakeIdentityMiddleware` (dev), JWT in production
- **Execution Context**: `RequestExecutionContextMiddleware` injects tenant/warehouse/user
- **Context Propagation**: Execution context flows through all service calls

### Optimistic Concurrency

- **Version Field**: All aggregates have `Version` property
- **Interceptor**: `VersionedEntitySaveChangesInterceptor` handles concurrency checks
- **Conflict Resolution**: Throws `DbUpdateConcurrencyException` on version mismatch

### Domain Events

- **Event Dispatcher**: `DomainEventDispatcher` collects events from aggregates
- **Event Interceptor**: `DomainEventInterceptor` dispatches events after SaveChanges
- **Event Publishing**: CAP publishes events transactionally with database changes

### Distributed Transactions

- **Outbox Pattern**: CAP stores events in same database transaction
- **Eventual Consistency**: Events delivered asynchronously via RabbitMQ
- **Idempotency**: Event handlers must be idempotent (CAP provides deduplication)

## Technology Decisions

### Why .NET Aspire?

- Simplified local development with orchestration
- Built-in service discovery and health checks
- Automatic telemetry and logging configuration
- Easy infrastructure provisioning (PostgreSQL, Redis, RabbitMQ)

### Why CAP Framework?

- Transactional outbox pattern out of the box
- Supports multiple message brokers (RabbitMQ, Kafka)
- Built-in retry and dead letter queue
- Dashboard for monitoring event delivery

### Why PostgreSQL?

- JSONB support for flexible schema (agent state, metadata)
- Strong ACID guarantees for transactional consistency
- Excellent performance for OLTP workloads
- Native support in EF Core

### Why YARP?

- High-performance reverse proxy built on Kestrel
- Configuration-based routing (no code changes)
- Middleware pipeline for authentication/authorization
- Load balancing and health checks

### Why Vue 3?

- Composition API for better code organization
- TypeScript support for type safety
- Excellent performance with virtual DOM
- Rich ecosystem (Element Plus, Pinia, Vue Router)

## Scalability Considerations

### Horizontal Scaling

- **Stateless Services**: All services are stateless (session state in Redis/PostgreSQL)
- **Load Balancing**: YARP can distribute load across multiple instances
- **Database Sharding**: Tenant-based sharding possible (separate databases per tenant)

### Performance Optimization

- **Redis Caching**: Frequently accessed data cached in Redis
- **Connection Pooling**: EF Core connection pooling enabled
- **Async/Await**: All I/O operations are asynchronous
- **Minimal APIs**: Lower overhead than MVC controllers

### Monitoring & Observability

- **Aspire Dashboard**: Real-time service health and metrics
- **CAP Dashboard**: Event delivery monitoring
- **Hangfire Dashboard**: Background job monitoring
- **Structured Logging**: Serilog with structured log output

## Security Considerations

### Authentication

- JWT tokens (production)
- Fake identity middleware (development)
- Token validation in gateway

### Authorization

- Role-based access control (RBAC)
- Tenant/warehouse isolation
- Execution context validation

### Data Protection

- Tenant data isolation at database level
- Encrypted connections (TLS)
- Secrets management (Aspire configuration)

### API Security

- CORS configuration for frontend
- Rate limiting (future)
- Input validation at API boundary

## Future Enhancements

1. **Distributed Tracing**: OpenTelemetry integration
2. **API Versioning**: Support multiple API versions
3. **GraphQL Gateway**: Alternative to REST APIs
4. **Real-time Notifications**: SignalR for push notifications
5. **Advanced AI Features**: Model fine-tuning, A/B testing
6. **Multi-Region Deployment**: Geographic distribution
7. **Kubernetes Deployment**: Container orchestration
8. **Service Mesh**: Istio/Linkerd for advanced networking
