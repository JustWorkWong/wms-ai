# WMS AI 生产级一期基础版 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付一个可运行的第一期生产级骨架，打通 `平台账号/租户/仓库 -> ASN/收货 -> 证据上传 -> AiGateway 双 Agent 检验 -> 自动通过/人工复核 -> 正式结论` 的黄金链路。

**Architecture:** 系统按 `Gateway + Platform + Inbound + AiGateway + Operations + Vue` 组织，开发期通过 `Aspire AppHost` 编排，网关使用 `YARP`，跨库一致性使用 `CAP + RabbitMQ`，后台作业使用 `Hangfire`，配置中心使用 `Nacos`，业务与 AI 的正式状态分别落到 `UserDb / BusinessDb / AiDb`。AI 侧采用 `MAF Workflow + EvidenceGapAgent + InspectionDecisionAgent`，前端只通过 `AiGateway` 使用 `AG-UI`。

**Tech Stack:** `.NET 10`, `ASP.NET Core`, `Aspire`, `EF Core`, `PostgreSQL`, `Redis`, `RabbitMQ`, `MinIO`, `YARP`, `Nacos SDK`, `CAP`, `Hangfire`, `OpenTelemetry`, `Vue 3`, `Vite`, `TypeScript`, `Pinia`, `Vue Router`, `Vitest`, `Playwright`, `@ag-ui/client`

---

## 文件结构

### 根目录

- `wms-ai.sln`: 解决方案入口
- `Directory.Build.props`: 统一 SDK、nullable、analyzers、测试约束
- `.editorconfig`: 基础编码规范
- `docs/superpowers/specs/*.md`: 已确认设计文档
- `docs/superpowers/plans/2026-04-13-wms-ai-production-foundation.md`: 当前计划

### 后端

- `src/AppHost/WmsAi.AppHost/`: `Aspire` 本地编排入口
- `src/ServiceDefaults/WmsAi.ServiceDefaults/`: OpenTelemetry、健康检查、服务默认配置
- `src/BuildingBlocks/WmsAi.SharedKernel/`: 共享领域基类、租户仓库上下文、乐观锁基类
- `src/BuildingBlocks/WmsAi.Contracts/`: HTTP DTO、CAP 事件契约、错误码常量
- `src/Gateway/WmsAi.Gateway.Host/`: `YARP` 网关
- `src/Platform/WmsAi.Platform.Host/`: 平台宿主
- `src/Platform/WmsAi.Platform.Application/`: 平台用例
- `src/Platform/WmsAi.Platform.Domain/`: 平台上下文领域对象
- `src/Platform/WmsAi.Platform.Infrastructure/`: `UserDb`、Nacos 配置消费、仓储实现
- `src/Inbound/WmsAi.Inbound.Host/`: 入库宿主
- `src/Inbound/WmsAi.Inbound.Application/`: ASN、收货、质检任务用例
- `src/Inbound/WmsAi.Inbound.Domain/`: 入库质检上下文领域对象
- `src/Inbound/WmsAi.Inbound.Infrastructure/`: `BusinessDb`、对象存储、CAP 消费
- `src/AiGateway/WmsAi.AiGateway.Host/`: AI 协议入口
- `src/AiGateway/WmsAi.AiGateway.Application/`: Session、Workflow、Model Routing、AG-UI
- `src/AiGateway/WmsAi.AiGateway.Domain/`: AI Runtime 上下文领域对象
- `src/AiGateway/WmsAi.AiGateway.Infrastructure/`: `AiDb`、MAF 持久化、Nacos 配置消费
- `src/Operations/WmsAi.Operations.Host/`: `Hangfire` server、迁移、种子数据、补偿任务

### 前端

- `web/wms-ai-web/`: 单一 `Vue` 工程，承载平台管理、主数据、入库作业、AI 工作台

### 测试

- `tests/WmsAi.ArchitectureTests/`: 结构约束与依赖边界
- `tests/WmsAi.Platform.Tests/`: 平台上下文集成测试
- `tests/WmsAi.Inbound.Tests/`: 入库质检上下文集成测试
- `tests/WmsAi.AiGateway.Tests/`: Session、双 Agent、Checkpoint、模型路由测试
- `tests/WmsAi.Integration.Tests/`: CAP、YARP、Bootstrap、种子数据测试
- `tests/WmsAi.Web.Tests/`: `Vitest` 前端单测
- `tests/WmsAi.E2E/`: `Playwright` 黄金链路

## Task 1: 解决方案骨架与 Aspire 编排

**Files:**
- Create: `wms-ai.sln`
- Create: `Directory.Build.props`
- Create: `.editorconfig`
- Create: `src/AppHost/WmsAi.AppHost/WmsAi.AppHost.csproj`
- Create: `src/AppHost/WmsAi.AppHost/Program.cs`
- Create: `src/ServiceDefaults/WmsAi.ServiceDefaults/WmsAi.ServiceDefaults.csproj`
- Create: `src/ServiceDefaults/WmsAi.ServiceDefaults/Extensions.cs`
- Create: `tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj`
- Create: `tests/WmsAi.ArchitectureTests/SolutionBootstrapTests.cs`

- [ ] **Step 1: 写失败的结构测试**

```csharp
using FluentAssertions;

namespace WmsAi.ArchitectureTests;

public class SolutionBootstrapTests
{
    [Theory]
    [InlineData("src/AppHost/WmsAi.AppHost/Program.cs")]
    [InlineData("src/ServiceDefaults/WmsAi.ServiceDefaults/Extensions.cs")]
    [InlineData("src/Gateway/WmsAi.Gateway.Host/WmsAi.Gateway.Host.csproj")]
    [InlineData("web/wms-ai-web/package.json")]
    public void Required_bootstrap_files_should_exist(string relativePath)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        File.Exists(Path.Combine(root, relativePath)).Should().BeTrue();
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj -v minimal`  
Expected: FAIL，提示缺少 `AppHost`、`ServiceDefaults`、`Gateway`、前端目录

- [ ] **Step 3: 创建解决方案与 Aspire 骨架**

```xml
<!-- /Users/tengfengsu/wfcodes/wms-ai/Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/AppHost/WmsAi.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithDataVolume();
var redis = builder.AddRedis("redis").WithDataVolume();
var rabbit = builder.AddRabbitMQ("rabbitmq").WithDataVolume();
var minio = builder.AddContainer("minio", "minio/minio").WithDataVolume();
var nacos = builder.AddContainer("nacos", "nacos/nacos-server").WithDataVolume();

builder.AddProject<Projects.WmsAi_Gateway_Host>("gateway");
builder.AddProject<Projects.WmsAi_Platform_Host>("platform");
builder.AddProject<Projects.WmsAi_Inbound_Host>("inbound");
builder.AddProject<Projects.WmsAi_AiGateway_Host>("ai-gateway");
builder.AddProject<Projects.WmsAi_Operations_Host>("operations");

builder.Build().Run();
```

```bash
dotnet new sln -n wms-ai
dotnet new aspire-apphost -n WmsAi.AppHost -o src/AppHost/WmsAi.AppHost
dotnet new aspire-servicedefaults -n WmsAi.ServiceDefaults -o src/ServiceDefaults/WmsAi.ServiceDefaults
dotnet new xunit -n WmsAi.ArchitectureTests -o tests/WmsAi.ArchitectureTests
npm create vite@latest /Users/tengfengsu/wfcodes/wms-ai/web/wms-ai-web -- --template vue-ts
```

- [ ] **Step 4: 运行结构测试并验证编排入口可构建**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj -v minimal && dotnet build /Users/tengfengsu/wfcodes/wms-ai/wms-ai.sln`  
Expected: PASS，解决方案和 `AppHost` 可编译

- [ ] **Step 5: Commit**

```bash
git -C /Users/tengfengsu/wfcodes/wms-ai add .
git -C /Users/tengfengsu/wfcodes/wms-ai commit -m "chore: bootstrap aspire solution skeleton"
```

## Task 2: 共享内核、DDD 基类与乐观锁

**Files:**
- Create: `src/BuildingBlocks/WmsAi.SharedKernel/WmsAi.SharedKernel.csproj`
- Create: `src/BuildingBlocks/WmsAi.SharedKernel/Domain/AggregateRoot.cs`
- Create: `src/BuildingBlocks/WmsAi.SharedKernel/Domain/TenantScopedAggregateRoot.cs`
- Create: `src/BuildingBlocks/WmsAi.SharedKernel/Domain/WarehouseScopedAggregateRoot.cs`
- Create: `src/BuildingBlocks/WmsAi.SharedKernel/Execution/RequestExecutionContext.cs`
- Create: `src/BuildingBlocks/WmsAi.SharedKernel/Persistence/VersionedEntityTypeConfiguration.cs`
- Create: `src/BuildingBlocks/WmsAi.Contracts/WmsAi.Contracts.csproj`
- Create: `src/BuildingBlocks/WmsAi.Contracts/Errors/ErrorCodes.cs`
- Test: `tests/WmsAi.ArchitectureTests/SharedKernelTests.cs`

- [ ] **Step 1: 写失败的共享内核测试**

```csharp
using FluentAssertions;
using WmsAi.SharedKernel.Execution;

namespace WmsAi.ArchitectureTests;

public class SharedKernelTests
{
    [Fact]
    public void Request_execution_context_should_include_tenant_user_and_membership()
    {
        var context = new RequestExecutionContext("tenant-a", "wh-1", "user-1", "membership-1", "corr-1");

        context.TenantId.Should().Be("tenant-a");
        context.WarehouseId.Should().Be("wh-1");
        context.UserId.Should().Be("user-1");
        context.MembershipId.Should().Be("membership-1");
        context.CorrelationId.Should().Be("corr-1");
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj --filter SharedKernelTests -v minimal`  
Expected: FAIL，提示 `RequestExecutionContext` 和基类不存在

- [ ] **Step 3: 实现共享内核和统一乐观锁约定**

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/BuildingBlocks/WmsAi.SharedKernel/Domain/AggregateRoot.cs
namespace WmsAi.SharedKernel.Domain;

public abstract class AggregateRoot
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public long Version { get; protected set; } = 1;
}
```

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/BuildingBlocks/WmsAi.SharedKernel/Execution/RequestExecutionContext.cs
namespace WmsAi.SharedKernel.Execution;

public sealed record RequestExecutionContext(
    string TenantId,
    string? WarehouseId,
    string UserId,
    string MembershipId,
    string CorrelationId);
```

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/BuildingBlocks/WmsAi.SharedKernel/Persistence/VersionedEntityTypeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WmsAi.SharedKernel.Persistence;

public static class VersionedEntityTypeConfiguration
{
    public static void ApplyVersion<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<long>("Version").IsConcurrencyToken();
    }
}
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj --filter SharedKernelTests -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/tengfengsu/wfcodes/wms-ai add src/BuildingBlocks tests/WmsAi.ArchitectureTests
git -C /Users/tengfengsu/wfcodes/wms-ai commit -m "feat: add shared kernel and optimistic locking primitives"
```

## Task 3: Platform 上下文与 UserDb

**Files:**
- Create: `src/Platform/WmsAi.Platform.Domain/WmsAi.Platform.Domain.csproj`
- Create: `src/Platform/WmsAi.Platform.Domain/Tenants/Tenant.cs`
- Create: `src/Platform/WmsAi.Platform.Domain/Tenants/Warehouse.cs`
- Create: `src/Platform/WmsAi.Platform.Domain/Users/User.cs`
- Create: `src/Platform/WmsAi.Platform.Domain/Users/Membership.cs`
- Create: `src/Platform/WmsAi.Platform.Application/WmsAi.Platform.Application.csproj`
- Create: `src/Platform/WmsAi.Platform.Application/Tenants/CreateTenantCommand.cs`
- Create: `src/Platform/WmsAi.Platform.Infrastructure/WmsAi.Platform.Infrastructure.csproj`
- Create: `src/Platform/WmsAi.Platform.Infrastructure/Persistence/UserDbContext.cs`
- Create: `src/Platform/WmsAi.Platform.Host/WmsAi.Platform.Host.csproj`
- Create: `src/Platform/WmsAi.Platform.Host/Program.cs`
- Test: `tests/WmsAi.Platform.Tests/CreateTenantTests.cs`

- [ ] **Step 1: 写失败的租户创建测试**

```csharp
using FluentAssertions;
using WmsAi.Platform.Application.Tenants;

namespace WmsAi.Platform.Tests;

public class CreateTenantTests
{
    [Fact]
    public async Task Create_tenant_should_create_default_warehouse_and_membership()
    {
        var handler = TestPlatformApplication.CreateTenantHandler();

        var result = await handler.Handle(new CreateTenantCommand(
            "TENANT_DEMO",
            "演示租户",
            "WH_SZ_01",
            "深圳一仓",
            "admin.demo"));

        result.TenantCode.Should().Be("TENANT_DEMO");
        result.DefaultWarehouseCode.Should().Be("WH_SZ_01");
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.Platform.Tests/WmsAi.Platform.Tests.csproj --filter CreateTenantTests -v minimal`  
Expected: FAIL，提示命令、DbContext、聚合不存在

- [ ] **Step 3: 实现 Platform 聚合、UserDb 与最小 API**

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/Platform/WmsAi.Platform.Domain/Tenants/Tenant.cs
public sealed class Tenant : AggregateRoot
{
    private readonly List<Warehouse> _warehouses = [];

    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Status { get; private set; } = "active";

    public IReadOnlyCollection<Warehouse> Warehouses => _warehouses;

    public Tenant(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public void AddWarehouse(string warehouseCode, string warehouseName)
        => _warehouses.Add(new Warehouse(Id, warehouseCode, warehouseName));
}
```

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/Platform/WmsAi.Platform.Infrastructure/Persistence/UserDbContext.cs
public sealed class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Membership> Memberships => Set<Membership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>().ToTable("tenants");
        modelBuilder.Entity<Warehouse>().ToTable("warehouses");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Membership>().ToTable("memberships");
    }
}
```

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/Platform/WmsAi.Platform.Host/Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddPlatformModule(builder.Configuration);

var app = builder.Build();
app.MapPost("/api/platform/tenants", PlatformEndpoints.CreateTenant);
app.MapGet("/health", () => Results.Ok("ok"));
app.Run();
```

- [ ] **Step 4: 运行测试与最小集成构建**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.Platform.Tests/WmsAi.Platform.Tests.csproj -v minimal && dotnet build /Users/tengfengsu/wfcodes/wms-ai/src/Platform/WmsAi.Platform.Host/WmsAi.Platform.Host.csproj`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/tengfengsu/wfcodes/wms-ai add src/Platform tests/WmsAi.Platform.Tests
git -C /Users/tengfengsu/wfcodes/wms-ai commit -m "feat: add platform bounded context and userdb"
```

## Task 4: Inbound 上下文与 BusinessDb

**Files:**
- Create: `src/Inbound/WmsAi.Inbound.Domain/WmsAi.Inbound.Domain.csproj`
- Create: `src/Inbound/WmsAi.Inbound.Domain/Inbound/InboundNotice.cs`
- Create: `src/Inbound/WmsAi.Inbound.Domain/Receipts/Receipt.cs`
- Create: `src/Inbound/WmsAi.Inbound.Domain/Qc/QcTask.cs`
- Create: `src/Inbound/WmsAi.Inbound.Domain/Qc/QcDecision.cs`
- Create: `src/Inbound/WmsAi.Inbound.Application/WmsAi.Inbound.Application.csproj`
- Create: `src/Inbound/WmsAi.Inbound.Application/Inbound/CreateInboundNoticeCommand.cs`
- Create: `src/Inbound/WmsAi.Inbound.Application/Receipts/RecordReceiptCommand.cs`
- Create: `src/Inbound/WmsAi.Inbound.Infrastructure/WmsAi.Inbound.Infrastructure.csproj`
- Create: `src/Inbound/WmsAi.Inbound.Infrastructure/Persistence/BusinessDbContext.cs`
- Create: `src/Inbound/WmsAi.Inbound.Host/WmsAi.Inbound.Host.csproj`
- Create: `src/Inbound/WmsAi.Inbound.Host/Program.cs`
- Test: `tests/WmsAi.Inbound.Tests/RecordReceiptTests.cs`

- [ ] **Step 1: 写失败的收货生成质检任务测试**

```csharp
using FluentAssertions;
using WmsAi.Inbound.Application.Receipts;

namespace WmsAi.Inbound.Tests;

public class RecordReceiptTests
{
    [Fact]
    public async Task Record_receipt_should_create_qc_tasks()
    {
        var handler = TestInboundApplication.CreateRecordReceiptHandler();

        var result = await handler.Handle(new RecordReceiptCommand(
            "tenant-demo",
            "wh-sz-01",
            "asn-001",
            "RCV_DEMO_001",
            [ new ReceiptLineInput("sku-001", 100m) ]));

        result.QcTaskCount.Should().BeGreaterThan(0);
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.Inbound.Tests/WmsAi.Inbound.Tests.csproj --filter RecordReceiptTests -v minimal`  
Expected: FAIL

- [ ] **Step 3: 实现 ASN、收货、质检任务最小闭环**

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/Inbound/WmsAi.Inbound.Domain/Qc/QcTask.cs
public sealed class QcTask : WarehouseScopedAggregateRoot
{
    public string TaskNo { get; private set; }
    public Guid PlanId { get; private set; }
    public Guid SkuId { get; private set; }
    public string Status { get; private set; } = "pending_inspection";

    public QcTask(string tenantId, string warehouseId, Guid planId, Guid skuId, string taskNo)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        PlanId = planId;
        SkuId = skuId;
        TaskNo = taskNo;
    }
}
```

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/Inbound/WmsAi.Inbound.Infrastructure/Persistence/BusinessDbContext.cs
public sealed class BusinessDbContext(DbContextOptions<BusinessDbContext> options) : DbContext(options)
{
    public DbSet<InboundNotice> InboundNotices => Set<InboundNotice>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<QcTask> QcTasks => Set<QcTask>();
    public DbSet<QcDecision> QcDecisions => Set<QcDecision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboundNotice>().ToTable("inbound_notices");
        modelBuilder.Entity<Receipt>().ToTable("receipts");
        modelBuilder.Entity<QcTask>().ToTable("qc_tasks");
        modelBuilder.Entity<QcDecision>().ToTable("qc_decisions");
    }
}
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.Inbound.Tests/WmsAi.Inbound.Tests.csproj -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/tengfengsu/wfcodes/wms-ai add src/Inbound tests/WmsAi.Inbound.Tests
git -C /Users/tengfengsu/wfcodes/wms-ai commit -m "feat: add inbound bounded context and businessdb"
```

## Task 5: CAP 事件契约与跨库一致性

**Files:**
- Create: `src/BuildingBlocks/WmsAi.Contracts/Events/TenantCreatedV1.cs`
- Create: `src/BuildingBlocks/WmsAi.Contracts/Events/ReceiptRecordedV1.cs`
- Create: `src/BuildingBlocks/WmsAi.Contracts/Events/AiSuggestionCreatedV1.cs`
- Create: `src/BuildingBlocks/WmsAi.Contracts/Events/QcDecisionFinalizedV1.cs`
- Modify: `src/Platform/WmsAi.Platform.Application/Tenants/CreateTenantCommand.cs`
- Modify: `src/Inbound/WmsAi.Inbound.Application/Receipts/RecordReceiptCommand.cs`
- Create: `tests/WmsAi.Integration.Tests/WmsAi.Integration.Tests.csproj`
- Create: `tests/WmsAi.Integration.Tests/CapPublishingTests.cs`

- [ ] **Step 1: 写失败的事件发布测试**

```csharp
using FluentAssertions;

namespace WmsAi.Integration.Tests;

public class CapPublishingTests
{
    [Fact]
    public async Task Recording_receipt_should_publish_receipt_recorded_v1()
    {
        var harness = await TestIntegrationHost.StartAsync();

        await harness.RecordReceiptAsync();

        harness.PublishedEvents.Should().ContainSingle(x => x.EventName == "receipt_recorded");
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.Integration.Tests/WmsAi.Integration.Tests.csproj --filter CapPublishingTests -v minimal`  
Expected: FAIL

- [ ] **Step 3: 实现契约与 CAP 发布**

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/BuildingBlocks/WmsAi.Contracts/Events/ReceiptRecordedV1.cs
namespace WmsAi.Contracts.Events;

public sealed record ReceiptRecordedV1(
    string EventId,
    string EventName,
    int EventVersion,
    string TenantId,
    string WarehouseId,
    ReceiptRecordedPayload Payload);

public sealed record ReceiptRecordedPayload(string ReceiptId, string NoticeId);
```

```csharp
// in record receipt application handler
await _capPublisher.PublishAsync("receipt_recorded", new ReceiptRecordedV1(
    Guid.NewGuid().ToString("N"),
    "receipt_recorded",
    1,
    command.TenantId,
    command.WarehouseId,
    new ReceiptRecordedPayload(receipt.Id.ToString(), receipt.NoticeId.ToString())));
```

- [ ] **Step 4: 运行集成测试**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.Integration.Tests/WmsAi.Integration.Tests.csproj -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/tengfengsu/wfcodes/wms-ai add src/BuildingBlocks/WmsAi.Contracts src/Platform src/Inbound tests/WmsAi.Integration.Tests
git -C /Users/tengfengsu/wfcodes/wms-ai commit -m "feat: add cap event contracts and publishing"
```

## Task 6: AiGateway、AiDb 与双 Agent MAF Workflow

**Files:**
- Create: `src/AiGateway/WmsAi.AiGateway.Domain/WmsAi.AiGateway.Domain.csproj`
- Create: `src/AiGateway/WmsAi.AiGateway.Domain/Sessions/MafSession.cs`
- Create: `src/AiGateway/WmsAi.AiGateway.Domain/Workflows/MafWorkflowRun.cs`
- Create: `src/AiGateway/WmsAi.AiGateway.Domain/Inspection/AiInspectionRun.cs`
- Create: `src/AiGateway/WmsAi.AiGateway.Application/WmsAi.AiGateway.Application.csproj`
- Create: `src/AiGateway/WmsAi.AiGateway.Application/Agents/EvidenceGapAgent.cs`
- Create: `src/AiGateway/WmsAi.AiGateway.Application/Agents/InspectionDecisionAgent.cs`
- Create: `src/AiGateway/WmsAi.AiGateway.Application/Workflows/InboundInspectionWorkflow.cs`
- Create: `src/AiGateway/WmsAi.AiGateway.Infrastructure/WmsAi.AiGateway.Infrastructure.csproj`
- Create: `src/AiGateway/WmsAi.AiGateway.Infrastructure/Persistence/AiDbContext.cs`
- Create: `src/AiGateway/WmsAi.AiGateway.Host/WmsAi.AiGateway.Host.csproj`
- Create: `src/AiGateway/WmsAi.AiGateway.Host/Program.cs`
- Test: `tests/WmsAi.AiGateway.Tests/WmsAi.AiGateway.Tests.csproj`
- Test: `tests/WmsAi.AiGateway.Tests/InboundInspectionWorkflowTests.cs`

- [ ] **Step 1: 写失败的双 Agent Workflow 测试**

```csharp
using FluentAssertions;

namespace WmsAi.AiGateway.Tests;

public class InboundInspectionWorkflowTests
{
    [Fact]
    public async Task Workflow_should_run_evidence_gap_then_inspection_decision()
    {
        var harness = await TestAiGatewayHost.StartAsync();

        var result = await harness.RunInspectionAsync();

        result.ExecutedAgentProfiles.Should().ContainInOrder(
            "evidence_gap_agent",
            "inspection_decision_agent");
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.AiGateway.Tests/WmsAi.AiGateway.Tests.csproj --filter InboundInspectionWorkflowTests -v minimal`  
Expected: FAIL

- [ ] **Step 3: 实现 AiDb、Session 持久化与双 Agent Workflow**

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/AiGateway/WmsAi.AiGateway.Application/Agents/EvidenceGapAgent.cs
public sealed class EvidenceGapAgent
{
    public const string ProfileCode = "evidence_gap_agent";

    public Task<AgentStepResult> ExecuteAsync(InspectionContext context, CancellationToken cancellationToken)
        => Task.FromResult(AgentStepResult.Next(ProfileCode, "evidence_checked"));
}
```

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/AiGateway/WmsAi.AiGateway.Application/Agents/InspectionDecisionAgent.cs
public sealed class InspectionDecisionAgent
{
    public const string ProfileCode = "inspection_decision_agent";

    public Task<InspectionSuggestion> ExecuteAsync(InspectionContext context, CancellationToken cancellationToken)
        => Task.FromResult(new InspectionSuggestion("pass", 0.97m, []));
}
```

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/AiGateway/WmsAi.AiGateway.Application/Workflows/InboundInspectionWorkflow.cs
public sealed class InboundInspectionWorkflow
{
    public async Task<WorkflowResult> RunAsync(InspectionContext context, CancellationToken cancellationToken)
    {
        var executed = new List<string>();

        await _gapAgent.ExecuteAsync(context, cancellationToken);
        executed.Add(EvidenceGapAgent.ProfileCode);

        var suggestion = await _decisionAgent.ExecuteAsync(context, cancellationToken);
        executed.Add(InspectionDecisionAgent.ProfileCode);

        return new WorkflowResult(executed, suggestion);
    }
}
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.AiGateway.Tests/WmsAi.AiGateway.Tests.csproj -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/tengfengsu/wfcodes/wms-ai add src/AiGateway tests/WmsAi.AiGateway.Tests
git -C /Users/tengfengsu/wfcodes/wms-ai commit -m "feat: add aigateway runtime and dual-agent workflow"
```

## Task 7: AiGateway 业务访问边界、AG-UI 与多租户上下文

**Files:**
- Modify: `src/AiGateway/WmsAi.AiGateway.Application/Workflows/InboundInspectionWorkflow.cs`
- Create: `src/AiGateway/WmsAi.AiGateway.Application/Functions/LoadQcTaskFunction.cs`
- Create: `src/AiGateway/WmsAi.AiGateway.Application/Functions/FinalizeDecisionCommandFunction.cs`
- Create: `src/AiGateway/WmsAi.AiGateway.Host/Endpoints/AiSessionEndpoints.cs`
- Create: `tests/WmsAi.AiGateway.Tests/TenantAwareFunctionCallingTests.cs`

- [ ] **Step 1: 写失败的租户上下文透传测试**

```csharp
using FluentAssertions;

namespace WmsAi.AiGateway.Tests;

public class TenantAwareFunctionCallingTests
{
    [Fact]
    public async Task Function_calling_should_pass_tenant_warehouse_user_and_membership()
    {
        var harness = await TestAiGatewayHost.StartAsync();

        await harness.RunInspectionAsync();

        harness.LastFunctionContext.Should().BeEquivalentTo(new
        {
            TenantId = "tenant-demo",
            WarehouseId = "wh-sz-01",
            UserId = "qc-inspector",
            MembershipId = "membership-001"
        });
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.AiGateway.Tests/WmsAi.AiGateway.Tests.csproj --filter TenantAwareFunctionCallingTests -v minimal`  
Expected: FAIL

- [ ] **Step 3: 实现受控 Function Calling 与 AG-UI Session 接口**

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/AiGateway/WmsAi.AiGateway.Application/Functions/LoadQcTaskFunction.cs
public sealed class LoadQcTaskFunction
{
    public Task<QcTaskSnapshot> ExecuteAsync(RequestExecutionContext context, string taskId, CancellationToken cancellationToken)
        => _inboundClient.GetQcTaskAsync(context.TenantId, context.WarehouseId!, taskId, cancellationToken);
}
```

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/AiGateway/WmsAi.AiGateway.Host/Endpoints/AiSessionEndpoints.cs
public static class AiSessionEndpoints
{
    public static IEndpointRouteBuilder MapAiSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ai/sessions", CreateSessionAsync);
        endpoints.MapGet("/api/ai/sessions/{sessionId}/stream", StreamSessionAsync);
        return endpoints;
    }
}
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.AiGateway.Tests/WmsAi.AiGateway.Tests.csproj -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/tengfengsu/wfcodes/wms-ai add src/AiGateway tests/WmsAi.AiGateway.Tests
git -C /Users/tengfengsu/wfcodes/wms-ai commit -m "feat: enforce tenant-aware function calling and ag-ui endpoints"
```

## Task 8: YARP 网关、认证上下文与路由编排

**Files:**
- Create: `src/Gateway/WmsAi.Gateway.Host/WmsAi.Gateway.Host.csproj`
- Create: `src/Gateway/WmsAi.Gateway.Host/Program.cs`
- Create: `src/Gateway/WmsAi.Gateway.Host/appsettings.json`
- Create: `src/Gateway/WmsAi.Gateway.Host/Auth/FakeIdentityMiddleware.cs`
- Test: `tests/WmsAi.Integration.Tests/GatewayRoutingTests.cs`

- [ ] **Step 1: 写失败的网关路由测试**

```csharp
using FluentAssertions;

namespace WmsAi.Integration.Tests;

public class GatewayRoutingTests
{
    [Fact]
    public async Task Gateway_should_route_platform_inbound_and_ai_requests()
    {
        var client = await TestGatewayHost.CreateClientAsync();

        var response = await client.GetAsync("/api/platform/tenants");

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.Integration.Tests/WmsAi.Integration.Tests.csproj --filter GatewayRoutingTests -v minimal`  
Expected: FAIL

- [ ] **Step 3: 实现 YARP 路由和身份头透传**

```json
// /Users/tengfengsu/wfcodes/wms-ai/src/Gateway/WmsAi.Gateway.Host/appsettings.json
{
  "ReverseProxy": {
    "Routes": {
      "platform": { "ClusterId": "platform", "Match": { "Path": "/api/platform/{**catch-all}" } },
      "inbound": { "ClusterId": "inbound", "Match": { "Path": "/api/inbound/{**catch-all}" } },
      "qc": { "ClusterId": "inbound", "Match": { "Path": "/api/qc/{**catch-all}" } },
      "ai": { "ClusterId": "ai-gateway", "Match": { "Path": "/api/ai/{**catch-all}" } }
    }
  }
}
```

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/Gateway/WmsAi.Gateway.Host/Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.UseMiddleware<FakeIdentityMiddleware>();
app.MapReverseProxy();
app.Run();
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.Integration.Tests/WmsAi.Integration.Tests.csproj --filter GatewayRoutingTests -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/tengfengsu/wfcodes/wms-ai add src/Gateway tests/WmsAi.Integration.Tests
git -C /Users/tengfengsu/wfcodes/wms-ai commit -m "feat: add yarp gateway and identity propagation"
```

## Task 9: Nacos、启动迁移/种子、Hangfire 运维作业

**Files:**
- Create: `src/Operations/WmsAi.Operations.Host/WmsAi.Operations.Host.csproj`
- Create: `src/Operations/WmsAi.Operations.Host/Program.cs`
- Create: `src/Operations/WmsAi.Operations.Host/Bootstrap/StartupBootstrapper.cs`
- Create: `src/Operations/WmsAi.Operations.Host/Seeds/demo-tenants.json`
- Create: `src/Operations/WmsAi.Operations.Host/Seeds/demo-skus.json`
- Create: `src/Operations/WmsAi.Operations.Host/Jobs/ScanPendingAiRunsJob.cs`
- Create: `src/Operations/WmsAi.Operations.Host/Jobs/BuildDailyQcMetricsJob.cs`
- Test: `tests/WmsAi.Integration.Tests/BootstrapAndSeedTests.cs`

- [ ] **Step 1: 写失败的自举测试**

```csharp
using FluentAssertions;

namespace WmsAi.Integration.Tests;

public class BootstrapAndSeedTests
{
    [Fact]
    public async Task Startup_bootstrap_should_apply_migrations_and_seed_when_switches_are_true()
    {
        var host = await TestOperationsHost.StartAsync(new()
        {
            ["Bootstrap:ApplyMigrationsOnStartup"] = "true",
            ["Bootstrap:ApplySeedDataOnStartup"] = "true"
        });

        host.SeedSummary.TotalImportedTenants.Should().BeGreaterThan(0);
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.Integration.Tests/WmsAi.Integration.Tests.csproj --filter BootstrapAndSeedTests -v minimal`  
Expected: FAIL

- [ ] **Step 3: 实现 Nacos 配置消费、Bootstrap 与 Hangfire**

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/Operations/WmsAi.Operations.Host/Program.cs
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHangfire(x => x.UseInMemoryStorage());
builder.Services.AddHangfireServer();
builder.Services.AddHostedService<StartupBootstrapper>();
builder.Services.AddScoped<ScanPendingAiRunsJob>();
builder.Services.AddScoped<BuildDailyQcMetricsJob>();
```

```csharp
// /Users/tengfengsu/wfcodes/wms-ai/src/Operations/WmsAi.Operations.Host/Bootstrap/StartupBootstrapper.cs
public sealed class StartupBootstrapper : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_configuration.GetValue("Bootstrap:ApplyMigrationsOnStartup", true))
            await _migrator.ApplyAsync(cancellationToken);

        if (_configuration.GetValue("Bootstrap:ApplySeedDataOnStartup", true))
            await _seedRunner.RunAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.Integration.Tests/WmsAi.Integration.Tests.csproj --filter BootstrapAndSeedTests -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/tengfengsu/wfcodes/wms-ai add src/Operations tests/WmsAi.Integration.Tests
git -C /Users/tengfengsu/wfcodes/wms-ai commit -m "feat: add nacos-backed bootstrap and hangfire operations"
```

## Task 10: Vue 前端骨架、页面路由与作业台闭环

**Files:**
- Create: `web/wms-ai-web/package.json`
- Create: `web/wms-ai-web/src/main.ts`
- Create: `web/wms-ai-web/src/router/index.ts`
- Create: `web/wms-ai-web/src/stores/auth.ts`
- Create: `web/wms-ai-web/src/api/platform.ts`
- Create: `web/wms-ai-web/src/api/inbound.ts`
- Create: `web/wms-ai-web/src/api/ai.ts`
- Create: `web/wms-ai-web/src/views/platform/TenantListView.vue`
- Create: `web/wms-ai-web/src/views/inbound/InboundNoticeListView.vue`
- Create: `web/wms-ai-web/src/views/workbench/QcWorkbenchView.vue`
- Create: `web/wms-ai-web/src/views/platform/ModelProfilesView.vue`
- Test: `tests/WmsAi.Web.Tests/package.json`
- Test: `tests/WmsAi.Web.Tests/src/qc-workbench.spec.ts`

- [ ] **Step 1: 写失败的前端作业台测试**

```ts
import { describe, expect, it } from 'vitest'
import { buildWorkbenchActions } from '../../web/wms-ai-web/src/views/workbench/buildWorkbenchActions'

describe('qc workbench actions', () => {
  it('shows upload and start ai when task is pending inspection', () => {
    const actions = buildWorkbenchActions({
      status: 'pending_inspection',
      allowedActions: ['upload_evidence', 'start_ai_inspection']
    })

    expect(actions).toEqual(['upload_evidence', 'start_ai_inspection'])
  })
})
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `cd /Users/tengfengsu/wfcodes/wms-ai/web/wms-ai-web && npm test`  
Expected: FAIL，提示 `buildWorkbenchActions` 和页面骨架不存在

- [ ] **Step 3: 实现路由、API 客户端和关键页面**

```ts
// /Users/tengfengsu/wfcodes/wms-ai/web/wms-ai-web/src/router/index.ts
import { createRouter, createWebHistory } from 'vue-router'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/platform/tenants', component: () => import('../views/platform/TenantListView.vue') },
    { path: '/platform/ai/model-profiles', component: () => import('../views/platform/ModelProfilesView.vue') },
    { path: '/inbound/notices', component: () => import('../views/inbound/InboundNoticeListView.vue') },
    { path: '/workbench/qc/:taskId', component: () => import('../views/workbench/QcWorkbenchView.vue') }
  ]
})
```

```ts
// /Users/tengfengsu/wfcodes/wms-ai/web/wms-ai-web/src/api/ai.ts
export async function createAiSession(payload: {
  tenantId: string
  userId: string
  sessionType: string
  businessObjectType: string
  businessObjectId: string
}) {
  return fetch('/api/ai/sessions', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  }).then(r => r.json())
}
```

- [ ] **Step 4: 运行前端测试与构建**

Run: `cd /Users/tengfengsu/wfcodes/wms-ai/web/wms-ai-web && npm test && npm run build`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/tengfengsu/wfcodes/wms-ai add web/wms-ai-web tests/WmsAi.Web.Tests
git -C /Users/tengfengsu/wfcodes/wms-ai commit -m "feat: add vue shell and qc workbench pages"
```

## Task 11: AG-UI 流式事件、人工复核与黄金链路 E2E

**Files:**
- Modify: `src/AiGateway/WmsAi.AiGateway.Host/Endpoints/AiSessionEndpoints.cs`
- Modify: `src/Inbound/WmsAi.Inbound.Host/Program.cs`
- Create: `tests/WmsAi.E2E/WmsAi.E2E.csproj`
- Create: `tests/WmsAi.E2E/QcGoldenFlowTests.cs`

- [ ] **Step 1: 写失败的黄金链路 E2E 测试**

```csharp
using FluentAssertions;

namespace WmsAi.E2E;

public class QcGoldenFlowTests
{
    [Fact]
    public async Task Golden_flow_should_finish_from_receipt_to_auto_pass_preview()
    {
        var app = await TestSystem.StartAsync();

        var result = await app.RunGoldenFlowAsync();

        result.FinalPreviewStatus.Should().Be("auto_pass_candidate");
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.E2E/WmsAi.E2E.csproj --filter QcGoldenFlowTests -v minimal`  
Expected: FAIL

- [ ] **Step 3: 补齐事件流、决策预览和人工复核 API**

```csharp
// in ai session stream endpoint
await response.WriteAsync("event: inspection.progress\n");
await response.WriteAsync("data: {\"step\":\"check_evidence\",\"message\":\"正在检查证据完整性\"}\n\n");
await response.WriteAsync("event: inspection.suggestion\n");
await response.WriteAsync("data: {\"suggestedDecision\":\"pass\",\"confidence\":0.97,\"riskTags\":[]}\n\n");
```

```csharp
// inbound host minimal endpoints
app.MapGet("/api/qc/tasks/{taskId}/decision-preview", async (string taskId, IPreviewService service)
    => Results.Ok(await service.GetAsync(taskId)));

app.MapPost("/api/qc/decisions/manual-review", async (ManualReviewRequest request, IManualReviewService service)
    => Results.Ok(await service.SubmitAsync(request)));
```

- [ ] **Step 4: 运行 E2E 与完整测试集**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.E2E/WmsAi.E2E.csproj -v minimal && dotnet test /Users/tengfengsu/wfcodes/wms-ai/wms-ai.sln -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/tengfengsu/wfcodes/wms-ai add src/AiGateway src/Inbound tests/WmsAi.E2E
git -C /Users/tengfengsu/wfcodes/wms-ai commit -m "feat: complete ag-ui flow and golden e2e path"
```

## Task 12: 文档对齐、运维验收与发布前检查

**Files:**
- Modify: `README.md`
- Create: `docs/runbooks/local-development.md`
- Create: `docs/runbooks/bootstrap-and-seed.md`
- Create: `docs/runbooks/ai-session-recovery.md`
- Test: `tests/WmsAi.ArchitectureTests/RunbookSmokeTests.cs`

- [ ] **Step 1: 写失败的文档烟雾测试**

```csharp
using FluentAssertions;

namespace WmsAi.ArchitectureTests;

public class RunbookSmokeTests
{
    [Theory]
    [InlineData("docs/runbooks/local-development.md")]
    [InlineData("docs/runbooks/bootstrap-and-seed.md")]
    [InlineData("docs/runbooks/ai-session-recovery.md")]
    public void Required_runbooks_should_exist(string relativePath)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        File.Exists(Path.Combine(root, relativePath)).Should().BeTrue();
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj --filter RunbookSmokeTests -v minimal`  
Expected: FAIL

- [ ] **Step 3: 补齐运行文档**

```md
<!-- /Users/tengfengsu/wfcodes/wms-ai/docs/runbooks/local-development.md -->
# Local Development

1. 运行 `dotnet run --project src/AppHost/WmsAi.AppHost`
2. 在 Aspire Dashboard 中确认 `postgres / redis / rabbitmq / minio / nacos` 都为 healthy
3. 打开 `web/wms-ai-web` 开发服务器
```

```md
<!-- /Users/tengfengsu/wfcodes/wms-ai/docs/runbooks/ai-session-recovery.md -->
# AI Session Recovery

1. 先查 `maf_sessions`
2. 再查 `maf_checkpoints`
3. 如果 workflow 停在 `waiting_manual_review`，先确认 `qc_decisions`
```

- [ ] **Step 4: 运行文档烟雾测试**

Run: `dotnet test /Users/tengfengsu/wfcodes/wms-ai/tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj --filter RunbookSmokeTests -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/tengfengsu/wfcodes/wms-ai add README.md docs/runbooks tests/WmsAi.ArchitectureTests
git -C /Users/tengfengsu/wfcodes/wms-ai commit -m "docs: add runbooks and release checklist"
```

## 自检

- 规格覆盖检查：
  - `DDD + 三库 + 乐观锁`：Task 2, 3, 4, 6
  - `Aspire + YARP + Nacos + Hangfire`：Task 1, 8, 9
  - `CAP + RabbitMQ + 分布式事务`：Task 5
  - `AiGateway + AG-UI + 双 Agent + MAF 持久化`：Task 6, 7, 11
  - `Vue 单前端 + 页面矩阵 + API 调用`：Task 10, 11
  - `启动迁移 + 种子数据`：Task 9
- 占位符检查：未使用 `TODO/TBD`
- 范围检查：只覆盖第一期“入库质检”主线，不扩到退货、出库和计费
