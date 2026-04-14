using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// ============================================================================
// 基础设施资源
// ============================================================================

// PostgreSQL：为不同限界上下文拆分逻辑数据库
// - UserDb：平台域（租户、仓库、用户、成员关系）
// - BusinessDb：入库域（到货通知、收货、质检任务与质检结论）
// - AiDb：AI 网关域（AI 工作流、智能体状态、会话数据）
// - HangfireDb：后台任务元数据，避免污染 UserDb 的业务建表流程
var postgresPassword = builder.AddParameter("postgres-password", "postgres", secret: true);
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithImage("postgres:16")
    .WithDataVolume()
    .WithPgAdmin(); // ← 添加 pgAdmin 管理界面

var userDb = postgres.AddDatabase("UserDb");
var businessDb = postgres.AddDatabase("BusinessDb");
var aiDb = postgres.AddDatabase("AiDb");
var hangfireDb = postgres.AddDatabase("HangfireDb");

// Redis：分布式缓存与会话存储
// 用途：缓存、分布式锁、会话状态
var redis = builder.AddRedis("redis")
    .WithImage("redis:7")
    .WithDataVolume()
    .WithRedisCommander(); // ← 添加 Redis Commander 管理界面

// RabbitMQ：CAP 事件总线使用的消息中间件
// 管理后台：http://localhost:15672 （wmsai / wmsai）
// 用途：跨服务事件发布、最终一致性
// 注意：RabbitMQ 4.x 默认不允许 guest 从远程连接，必须显式使用非 guest 账号。
var rabbitMqUser = builder.AddParameter("rabbitmq-user", "wmsai");
var rabbitMqPassword = builder.AddParameter("rabbitmq-password", "wmsai", secret: true);
var rabbitmq = builder.AddRabbitMQ("rabbitmq", rabbitMqUser, rabbitMqPassword)
    .WithDataVolume()
    .WithManagementPlugin();

// MinIO：兼容 S3 的对象存储
// 控制台：http://localhost:9001 （minioadmin/minioadmin）
// API：http://localhost:9000
// 用途：质检证据文件、附件存储
var minio = builder.AddContainer("minio", "minio/minio")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithBindMount("minio-data", "/data");

// Nacos：配置中心与服务注册中心
// 控制台：http://localhost:8848/nacos （nacos/nacos）
// 用途：动态配置、服务注册发现
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
// 服务应用
// ============================================================================

// Platform：用户 / 租户 / 仓库管理限界上下文
// 数据库：UserDb（租户、仓库、用户、成员关系）
var platform = builder.AddProject<Projects.WmsAi_Platform_Host>("platform")
    .WithHttpEndpoint(targetPort: 5001, port: 5001, isProxied: false)
    .WithReference(userDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(userDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// Inbound：入库业务限界上下文
// 数据库：BusinessDb（到货通知、收货、质检任务 / 结论）
var inbound = builder.AddProject<Projects.WmsAi_Inbound_Host>("inbound")
    .WithHttpEndpoint(targetPort: 5002, port: 5002, isProxied: false)
    .WithReference(businessDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(businessDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// AiGateway：AI 工作流编排限界上下文
// 数据库：AiDb（智能体工作流、会话状态）
var aiGateway = builder.AddProject<Projects.WmsAi_AiGateway_Host>("ai-gateway")
    .WithHttpEndpoint(targetPort: 5003, port: 5003, isProxied: false)
    .WithReference(aiDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(aiDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// Operations：后台任务与定时任务
// 负责：Hangfire 作业、数据同步、清理任务
var operations = builder.AddProject<Projects.WmsAi_Operations_Host>("operations")
    .WithHttpEndpoint(targetPort: 5004, port: 5004, isProxied: false)
    .WithReference(userDb)
    .WithReference(businessDb)
    .WithReference(aiDb)
    .WithReference(hangfireDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(userDb)
    .WaitFor(businessDb)
    .WaitFor(aiDb)
    .WaitFor(hangfireDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WaitFor(platform);

// Gateway：带鉴权的 YARP 反向代理
// 负责：路由、鉴权、限流
var gateway = builder.AddProject<Projects.WmsAi_Gateway_Host>("gateway")
    .WithHttpEndpoint(targetPort: 5000, port: 5000, isProxied: false)
    .WithReference(redis)
    .WithReference(platform)
    .WithReference(inbound)
    .WithReference(aiGateway)
    .WithReference(operations)
    .WaitFor(platform)
    .WaitFor(inbound)
    .WaitFor(aiGateway)
    .WaitFor(operations);

builder.Build().Run();
