using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// ============================================================================
// Infrastructure Resources
// ============================================================================

// PostgreSQL - Three logical databases for different bounded contexts
// - UserDb: Platform bounded context (tenants, warehouses, users, memberships)
// - BusinessDb: Inbound bounded context (notices, receipts, qc tasks/decisions)
// - AiDb: AiGateway bounded context (AI workflows, agent states, conversations)
var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres:16")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithDataVolume();

var userDb = postgres.AddDatabase("UserDb");
var businessDb = postgres.AddDatabase("BusinessDb");
var aiDb = postgres.AddDatabase("AiDb");

// Redis - Distributed cache and session storage
// Used for: caching, distributed locks, session state
var redis = builder.AddRedis("redis")
    .WithImage("redis:7")
    .WithDataVolume();

// RabbitMQ - Message broker for CAP event bus
// Management UI: http://localhost:15672 (guest/guest)
// Used for: cross-service event publishing, eventual consistency
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithImage("rabbitmq:3-management")
    .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
    .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
    .WithDataVolume()
    .WithManagementPlugin();

// MinIO - S3-compatible object storage
// Console UI: http://localhost:9001 (minioadmin/minioadmin)
// API: http://localhost:9000
// Used for: evidence file storage, document attachments
var minio = builder.AddContainer("minio", "minio/minio")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithBindMount("minio-data", "/data");

// Nacos - Configuration center and service discovery
// Console UI: http://localhost:8848/nacos (nacos/nacos)
// Used for: dynamic configuration, service registry
var nacos = builder.AddContainer("nacos", "nacos/nacos-server")
    .WithEnvironment("MODE", "standalone")
    .WithEnvironment("NACOS_AUTH_ENABLE", "true")
    .WithEnvironment("NACOS_AUTH_IDENTITY_KEY", "nacos")
    .WithEnvironment("NACOS_AUTH_IDENTITY_VALUE", "nacos")
    .WithEnvironment("NACOS_AUTH_TOKEN", "SecretKey012345678901234567890123456789012345678901234567890123456789")
    .WithEndpoint(port: 8848, targetPort: 8848, name: "http")
    .WithEndpoint(port: 9848, targetPort: 9848, name: "grpc")
    .WithBindMount("nacos-data", "/home/nacos/data");

// ============================================================================
// Service Applications
// ============================================================================

// Platform - User/Tenant/Warehouse management bounded context
// Database: UserDb (tenants, warehouses, users, memberships)
var platform = builder.AddProject<Projects.WmsAi_Platform_Host>("platform")
    .WithReference(userDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(userDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// Inbound - Inbound logistics bounded context
// Database: BusinessDb (inbound notices, receipts, qc tasks/decisions)
var inbound = builder.AddProject<Projects.WmsAi_Inbound_Host>("inbound")
    .WithReference(businessDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(businessDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// AiGateway - AI workflow orchestration bounded context
// Database: AiDb (agent workflows, conversation states)
var aiGateway = builder.AddProject<Projects.WmsAi_AiGateway_Host>("ai-gateway")
    .WithReference(aiDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(aiDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// Operations - Background jobs and scheduled tasks
// Handles: Hangfire jobs, data synchronization, cleanup tasks
var operations = builder.AddProject<Projects.WmsAi_Operations_Host>("operations")
    .WithReference(userDb)
    .WithReference(businessDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(userDb)
    .WaitFor(businessDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// Gateway - YARP reverse proxy with authentication
// Handles: routing, authentication, rate limiting
var gateway = builder.AddProject<Projects.WmsAi_Gateway_Host>("gateway")
    .WithReference(redis)
    .WithReference(platform)
    .WithReference(inbound)
    .WithReference(aiGateway)
    .WithReference(operations);

builder.Build().Run();
