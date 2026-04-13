# WMS AI 入库质检基础版 Implementation Plan

> Status: Paused and superseded by `2026-04-13-wms-ai-production-foundation.md`. This plan was written before the design was refined around `MAF`, `Aspire`, `Nacos`, `YARP`, `Hangfire`, three databases, distributed transaction strategy, and split design documents. Do not execute this plan.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付一个可运行的第一期系统，打通“平台账号与租户仓库 -> ASN/收货 -> 证据上传 -> AI 判定 -> 自动通过/人工复核 -> 正式结论落库”的黄金链路。

**Architecture:** 保持 `Gateway + Platform Core + Inbound QC Core + AI Gateway` 四层边界。普通业务流量经网关直达业务服务，AI 会话与 `AG-UI` 流量统一经 `AI Gateway`，会话热状态进 `Redis`，checkpoint 和业务真相进 `PostgreSQL`，证据文件进对象存储。

**Tech Stack:** `.NET 10`, `ASP.NET Core`, `EF Core`, `PostgreSQL`, `Redis`, `RabbitMQ`, `MinIO(S3)`, `YARP`, `OpenTelemetry`, `Vue 3`, `Vite`, `TypeScript`, `Vitest`, `Playwright`, `@ag-ui/client`, `@ag-ui/core`

---

## 文件结构

### 根目录

- `wms-ai.sln`: 解决方案入口
- `Directory.Build.props`: 统一 SDK、nullable、analyzers、测试约定
- `.editorconfig`: 基础编码规范
- `docker-compose.yml`: 本地基础设施，至少包含 `postgres`、`redis`、`rabbitmq`、`minio`
- `docs/superpowers/specs/2026-04-13-wms-ai-inbound-qc-design.md`: 已确认设计
- `docs/superpowers/plans/2026-04-13-wms-ai-inbound-qc-foundation.md`: 当前计划

### 后端

- `src/Apps/WmsAi.Gateway/`: 外部统一入口，`YARP` 路由与鉴权中间件
- `src/Apps/WmsAi.PlatformCore.Api/`: 租户、仓库、账号、角色、配置接口
- `src/Apps/WmsAi.InboundQcCore.Api/`: `ASN`、收货、质检任务、复核、结论接口
- `src/Apps/WmsAi.AiGateway.Api/`: `AG-UI`、session、checkpoint、模型路由、流式事件接口
- `src/Shared/WmsAi.SharedKernel/`: 领域基类、租户仓库作用域、审计抽象、Result 模型
- `src/Shared/WmsAi.Contracts/`: 跨服务 DTO 和事件契约

### 测试

- `tests/WmsAi.ArchitectureTests/`: 结构约束、依赖边界、必备配置烟雾测试
- `tests/WmsAi.PlatformCore.Tests/`: 平台域集成测试
- `tests/WmsAi.InboundQcCore.Tests/`: 入库质检域集成测试
- `tests/WmsAi.AiGateway.Tests/`: session/checkpoint/压缩/模型配置测试
- `tests/WmsAi.WebApp.Tests/`: 前端 `Vitest`
- `tests/WmsAi.E2E/`: 端到端 `Playwright`

### 前端

- `web/`: 单一 `Vue` 项目，承载平台管理、租户业务、质检作业与分析助手

## Task 1: 脚手架与基础设施骨架

**Files:**
- Create: `wms-ai.sln`
- Create: `Directory.Build.props`
- Create: `.editorconfig`
- Create: `docker-compose.yml`
- Create: `src/Apps/WmsAi.Gateway/WmsAi.Gateway.csproj`
- Create: `src/Apps/WmsAi.PlatformCore.Api/WmsAi.PlatformCore.Api.csproj`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/WmsAi.InboundQcCore.Api.csproj`
- Create: `src/Apps/WmsAi.AiGateway.Api/WmsAi.AiGateway.Api.csproj`
- Create: `src/Shared/WmsAi.SharedKernel/WmsAi.SharedKernel.csproj`
- Create: `src/Shared/WmsAi.Contracts/WmsAi.Contracts.csproj`
- Create: `tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj`
- Test: `tests/WmsAi.ArchitectureTests/SolutionLayoutTests.cs`

- [ ] **Step 1: 写失败的结构烟雾测试**

```csharp
using FluentAssertions;

namespace WmsAi.ArchitectureTests;

public class SolutionLayoutTests
{
    [Theory]
    [InlineData("src/Apps/WmsAi.Gateway/WmsAi.Gateway.csproj")]
    [InlineData("src/Apps/WmsAi.PlatformCore.Api/WmsAi.PlatformCore.Api.csproj")]
    [InlineData("src/Apps/WmsAi.InboundQcCore.Api/WmsAi.InboundQcCore.Api.csproj")]
    [InlineData("src/Apps/WmsAi.AiGateway.Api/WmsAi.AiGateway.Api.csproj")]
    [InlineData("web/package.json")]
    [InlineData("docker-compose.yml")]
    public void Required_paths_should_exist(string relativePath)
    {
        var root = FindRepoRoot();
        File.Exists(Path.Combine(root, relativePath))
            .Should().BeTrue($"{relativePath} must be created in bootstrap task");
    }

    private static string FindRepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj -v minimal`  
Expected: FAIL，提示缺少项目文件和 `docker-compose.yml`

- [ ] **Step 3: 创建解决方案、项目和本地基础设施**

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:17
  redis:
    image: redis:7
  rabbitmq:
    image: rabbitmq:4-management
  minio:
    image: minio/minio:latest
```

```bash
dotnet new sln -n wms-ai
dotnet new webapi -n WmsAi.Gateway -o src/Apps/WmsAi.Gateway --no-https
dotnet new webapi -n WmsAi.PlatformCore.Api -o src/Apps/WmsAi.PlatformCore.Api --no-https
dotnet new webapi -n WmsAi.InboundQcCore.Api -o src/Apps/WmsAi.InboundQcCore.Api --no-https
dotnet new webapi -n WmsAi.AiGateway.Api -o src/Apps/WmsAi.AiGateway.Api --no-https
dotnet new classlib -n WmsAi.SharedKernel -o src/Shared/WmsAi.SharedKernel
dotnet new classlib -n WmsAi.Contracts -o src/Shared/WmsAi.Contracts
dotnet new xunit -n WmsAi.ArchitectureTests -o tests/WmsAi.ArchitectureTests
npm create vite@latest web -- --template vue-ts
```

- [ ] **Step 4: 运行结构测试与解决方案构建**

Run: `dotnet test tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj -v minimal && dotnet build wms-ai.sln`  
Expected: PASS，所有必需路径存在，解决方案可编译

- [ ] **Step 5: 提交**

```bash
git add .
git commit -m "chore: bootstrap solution and local infrastructure"
```

## Task 2: 共享内核与租户仓库作用域

**Files:**
- Create: `src/Shared/WmsAi.SharedKernel/Tenancy/TenantScopedEntity.cs`
- Create: `src/Shared/WmsAi.SharedKernel/Tenancy/WarehouseScopedEntity.cs`
- Create: `src/Shared/WmsAi.SharedKernel/Execution/RequestScopeContext.cs`
- Create: `src/Shared/WmsAi.SharedKernel/Results/AppResult.cs`
- Create: `src/Shared/WmsAi.Contracts/Auth/UserContextDto.cs`
- Test: `tests/WmsAi.ArchitectureTests/ScopePrimitivesTests.cs`

- [ ] **Step 1: 写失败的作用域测试**

```csharp
using FluentAssertions;
using WmsAi.SharedKernel.Execution;

namespace WmsAi.ArchitectureTests;

public class ScopePrimitivesTests
{
    [Fact]
    public void Warehouse_scope_should_always_include_tenant()
    {
        var scope = RequestScopeContext.ForWarehouse("tenant-a", "wh-1", "u-1");

        scope.TenantId.Should().Be("tenant-a");
        scope.WarehouseId.Should().Be("wh-1");
        scope.UserId.Should().Be("u-1");
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj --filter ScopePrimitivesTests`  
Expected: FAIL，提示 `RequestScopeContext` 尚未实现

- [ ] **Step 3: 实现共享作用域与返回模型**

```csharp
namespace WmsAi.SharedKernel.Execution;

public sealed record RequestScopeContext(
    string TenantId,
    string? WarehouseId,
    string UserId)
{
    public static RequestScopeContext ForWarehouse(string tenantId, string warehouseId, string userId)
        => new(tenantId, warehouseId, userId);
}
```

```csharp
namespace WmsAi.SharedKernel.Tenancy;

public abstract class WarehouseScopedEntity : TenantScopedEntity
{
    public required string WarehouseId { get; init; }
}
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj --filter ScopePrimitivesTests`  
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/Shared tests/WmsAi.ArchitectureTests
git commit -m "feat: add shared tenancy scope primitives"
```

## Task 3: Platform Core 最小闭环

**Files:**
- Create: `src/Apps/WmsAi.PlatformCore.Api/Data/PlatformDbContext.cs`
- Create: `src/Apps/WmsAi.PlatformCore.Api/Domain/Tenant.cs`
- Create: `src/Apps/WmsAi.PlatformCore.Api/Domain/Warehouse.cs`
- Create: `src/Apps/WmsAi.PlatformCore.Api/Domain/AppUser.cs`
- Create: `src/Apps/WmsAi.PlatformCore.Api/Endpoints/Tenants/CreateTenantEndpoint.cs`
- Create: `src/Apps/WmsAi.PlatformCore.Api/Endpoints/Warehouses/CreateWarehouseEndpoint.cs`
- Create: `src/Apps/WmsAi.PlatformCore.Api/Endpoints/Auth/LoginEndpoint.cs`
- Test: `tests/WmsAi.PlatformCore.Tests/TenantWarehouseFlowTests.cs`

- [ ] **Step 1: 写失败的租户与仓库集成测试**

```csharp
public class TenantWarehouseFlowTests : IClassFixture<PlatformApiFactory>
{
    [Fact]
    public async Task Tenant_admin_can_create_warehouse_inside_own_tenant()
    {
        var client = factory.CreateAuthenticatedClient(role: "TenantAdmin", tenantId: "t-1");
        var response = await client.PostAsJsonAsync("/api/platform/warehouses", new
        {
            tenantId = "t-1",
            code = "WH-SH-01",
            name = "Shanghai Main"
        });

        response.EnsureSuccessStatusCode();
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test tests/WmsAi.PlatformCore.Tests/WmsAi.PlatformCore.Tests.csproj --filter TenantWarehouseFlowTests`  
Expected: FAIL，缺少 `DbContext`、端点和认证测试夹具

- [ ] **Step 3: 实现平台最小域模型与接口**

```csharp
public sealed class Tenant
{
    public Guid Id { get; init; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}
```

```csharp
app.MapPost("/api/platform/warehouses", async (
    CreateWarehouseRequest request,
    PlatformDbContext db,
    ICurrentUserAccessor currentUser) =>
{
    currentUser.RequireTenantAdmin(request.TenantId);
    db.Warehouses.Add(new Warehouse { TenantId = request.TenantId, Code = request.Code, Name = request.Name });
    await db.SaveChangesAsync();
    return Results.Created($"/api/platform/warehouses/{request.Code}", null);
});
```

- [ ] **Step 4: 运行平台测试**

Run: `dotnet test tests/WmsAi.PlatformCore.Tests/WmsAi.PlatformCore.Tests.csproj -v minimal`  
Expected: PASS，租户与仓库创建链路通过

- [ ] **Step 5: 提交**

```bash
git add src/Apps/WmsAi.PlatformCore.Api tests/WmsAi.PlatformCore.Tests
git commit -m "feat: add platform core tenant and warehouse flow"
```

## Task 4: Inbound QC Core 的 ASN 到质检任务生成

**Files:**
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Data/InboundQcDbContext.cs`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Domain/InboundNotice.cs`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Domain/Receipt.cs`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Domain/QcTask.cs`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Services/QcTaskPlanner.cs`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Endpoints/Inbound/CreateInboundNoticeEndpoint.cs`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Endpoints/Receipts/CreateReceiptEndpoint.cs`
- Test: `tests/WmsAi.InboundQcCore.Tests/InboundToQcTaskTests.cs`

- [ ] **Step 1: 写失败的黄金路径测试**

```csharp
public class InboundToQcTaskTests : IClassFixture<InboundQcApiFactory>
{
    [Fact]
    public async Task Receipt_submission_should_generate_qc_tasks()
    {
        var client = factory.CreateAuthenticatedClient("Inspector", "t-1", "wh-1");
        await client.PostAsJsonAsync("/api/inbound/notices", new
        {
            warehouseId = "wh-1",
            noticeNo = "ASN-001",
            lines = new[] { new { skuCode = "SKU-001", expectedQty = 10 } }
        });

        var receiptResponse = await client.PostAsJsonAsync("/api/inbound/receipts", new
        {
            warehouseId = "wh-1",
            noticeNo = "ASN-001",
            lines = new[] { new { skuCode = "SKU-001", receivedQty = 10 } }
        });

        receiptResponse.EnsureSuccessStatusCode();
        var taskList = await client.GetFromJsonAsync<List<object>>("/api/qc/tasks?noticeNo=ASN-001");
        taskList.Should().NotBeEmpty();
    }
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test tests/WmsAi.InboundQcCore.Tests/WmsAi.InboundQcCore.Tests.csproj --filter InboundToQcTaskTests`  
Expected: FAIL，提示缺少 `ASN`、收货与任务生成能力

- [ ] **Step 3: 实现入库与任务生成最小模型**

```csharp
public sealed class QcTaskPlanner
{
    public IReadOnlyList<QcTask> Plan(Receipt receipt)
        => receipt.Lines.Select(line => new QcTask
        {
            TenantId = receipt.TenantId,
            WarehouseId = receipt.WarehouseId,
            NoticeNo = receipt.NoticeNo,
            SkuCode = line.SkuCode,
            Status = QcTaskStatus.Pending
        }).ToList();
}
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test tests/WmsAi.InboundQcCore.Tests/WmsAi.InboundQcCore.Tests.csproj -v minimal`  
Expected: PASS，收货后成功生成质检任务

- [ ] **Step 5: 提交**

```bash
git add src/Apps/WmsAi.InboundQcCore.Api tests/WmsAi.InboundQcCore.Tests
git commit -m "feat: add inbound notice to qc task flow"
```

## Task 5: 证据上传与对象存储绑定

**Files:**
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Domain/EvidenceAsset.cs`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Services/ObjectStorageService.cs`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Endpoints/Evidence/CreateUploadSessionEndpoint.cs`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Endpoints/Evidence/BindEvidenceEndpoint.cs`
- Test: `tests/WmsAi.InboundQcCore.Tests/EvidenceBindingTests.cs`

- [ ] **Step 1: 写失败的证据绑定测试**

```csharp
[Fact]
public async Task Uploaded_asset_should_bind_to_qc_task()
{
    var client = factory.CreateAuthenticatedClient("Inspector", "t-1", "wh-1");
    var upload = await client.PostAsJsonAsync("/api/evidence/upload-sessions", new
    {
        fileName = "damage-1.jpg",
        contentType = "image/jpeg"
    });

    upload.EnsureSuccessStatusCode();
    var bind = await client.PostAsJsonAsync("/api/evidence/bindings", new
    {
        qcTaskId = seededTaskId,
        objectKey = "tenant/t-1/damage-1.jpg"
    });

    bind.EnsureSuccessStatusCode();
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test tests/WmsAi.InboundQcCore.Tests/WmsAi.InboundQcCore.Tests.csproj --filter EvidenceBindingTests`  
Expected: FAIL，缺少上传会话与绑定接口

- [ ] **Step 3: 实现上传会话与绑定**

```csharp
public sealed record CreateUploadSessionResponse(string ObjectKey, Uri PutUrl);
```

```csharp
app.MapPost("/api/evidence/bindings", async (
    BindEvidenceRequest request,
    InboundQcDbContext db) =>
{
    db.EvidenceAssets.Add(new EvidenceAsset
    {
        TenantId = request.TenantId,
        WarehouseId = request.WarehouseId,
        QcTaskId = request.QcTaskId,
        ObjectKey = request.ObjectKey,
        ContentType = request.ContentType
    });
    await db.SaveChangesAsync();
    return Results.Ok();
});
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test tests/WmsAi.InboundQcCore.Tests/WmsAi.InboundQcCore.Tests.csproj --filter EvidenceBindingTests`  
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/Apps/WmsAi.InboundQcCore.Api tests/WmsAi.InboundQcCore.Tests
git commit -m "feat: add evidence upload and binding flow"
```

## Task 6: AI Gateway 的 session、checkpoint 与模型路由

**Files:**
- Create: `src/Apps/WmsAi.AiGateway.Api/Data/AiGatewayDbContext.cs`
- Create: `src/Apps/WmsAi.AiGateway.Api/Domain/AiSession.cs`
- Create: `src/Apps/WmsAi.AiGateway.Api/Domain/AiCheckpoint.cs`
- Create: `src/Apps/WmsAi.AiGateway.Api/Domain/AiSummarySnapshot.cs`
- Create: `src/Apps/WmsAi.AiGateway.Api/Services/AiSessionService.cs`
- Create: `src/Apps/WmsAi.AiGateway.Api/Services/AiCheckpointService.cs`
- Create: `src/Apps/WmsAi.AiGateway.Api/Services/ModelRoutingService.cs`
- Test: `tests/WmsAi.AiGateway.Tests/SessionRestoreTests.cs`
- Test: `tests/WmsAi.AiGateway.Tests/ModelRoutingTests.cs`

- [ ] **Step 1: 写失败的 session 恢复与模型路由测试**

```csharp
[Fact]
public async Task Restore_should_resume_from_last_checkpoint()
{
    var sessionId = await service.StartInspectionSessionAsync("t-1", "u-1", "QcTask", "task-1");
    await service.AppendCheckpointAsync(sessionId, "after-model");

    var restored = await service.RestoreAsync(sessionId);

    restored.LastCheckpointName.Should().Be("after-model");
}
```

```csharp
[Fact]
public void Inspection_scene_should_resolve_multimodal_profile()
{
    var model = routing.Resolve("inspection", tenantId: "t-1");
    model.ProfileName.Should().Be("inspection-multimodal");
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test tests/WmsAi.AiGateway.Tests/WmsAi.AiGateway.Tests.csproj -v minimal`  
Expected: FAIL，缺少 session/checkpoint/model routing 服务

- [ ] **Step 3: 实现 AI Gateway 核心对象与服务**

```csharp
public sealed class AiSession
{
    public Guid Id { get; init; }
    public required string TenantId { get; set; }
    public required string UserId { get; set; }
    public required string SessionType { get; set; }
    public required string BusinessObjectType { get; set; }
    public required string BusinessObjectId { get; set; }
}
```

```csharp
public sealed class ModelRoutingService
{
    public ModelProfile Resolve(string scene, string tenantId)
        => scene switch
        {
            "inspection" => new("inspection-multimodal", "gpt-4.1-mini"),
            "summary" => new("summary-compression", "gpt-4.1-mini"),
            _ => throw new InvalidOperationException($"Unsupported scene: {scene}")
        };
}
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test tests/WmsAi.AiGateway.Tests/WmsAi.AiGateway.Tests.csproj -v minimal`  
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/Apps/WmsAi.AiGateway.Api tests/WmsAi.AiGateway.Tests
git commit -m "feat: add ai gateway session checkpoint and model routing"
```

## Task 7: AI 建议落地、自动通过与人工复核分支

**Files:**
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Services/AiSuggestionEvaluator.cs`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Endpoints/Qc/SubmitAiSuggestionEndpoint.cs`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Endpoints/Qc/ConfirmReviewEndpoint.cs`
- Create: `src/Apps/WmsAi.InboundQcCore.Api/Domain/QcDecision.cs`
- Test: `tests/WmsAi.InboundQcCore.Tests/AiDecisionBranchTests.cs`

- [ ] **Step 1: 写失败的自动通过与转人工测试**

```csharp
[Fact]
public async Task High_confidence_low_risk_suggestion_should_auto_pass()
{
    var response = await client.PostAsJsonAsync("/api/qc/ai-suggestions", new
    {
        qcTaskId = seededTaskId,
        suggestedDecision = "Pass",
        confidence = 0.98m,
        riskTags = Array.Empty<string>()
    });

    response.EnsureSuccessStatusCode();
    var decision = await client.GetFromJsonAsync<QcDecisionDto>($"/api/qc/decisions/{seededTaskId}");
    decision!.Status.Should().Be("AutoPassed");
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test tests/WmsAi.InboundQcCore.Tests/WmsAi.InboundQcCore.Tests.csproj --filter AiDecisionBranchTests`  
Expected: FAIL，缺少 AI 建议入站和决策分支逻辑

- [ ] **Step 3: 实现分支判断器**

```csharp
public sealed class AiSuggestionEvaluator
{
    public QcDecisionStatus Evaluate(decimal confidence, IReadOnlyCollection<string> riskTags)
        => confidence >= 0.95m && riskTags.Count == 0
            ? QcDecisionStatus.AutoPassed
            : QcDecisionStatus.PendingManualReview;
}
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test tests/WmsAi.InboundQcCore.Tests/WmsAi.InboundQcCore.Tests.csproj --filter AiDecisionBranchTests`  
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/Apps/WmsAi.InboundQcCore.Api tests/WmsAi.InboundQcCore.Tests
git commit -m "feat: add ai suggestion decision branching"
```

## Task 8: Gateway 路由与统一认证入口

**Files:**
- Create: `src/Apps/WmsAi.Gateway/Program.cs`
- Create: `src/Apps/WmsAi.Gateway/appsettings.json`
- Create: `src/Apps/WmsAi.Gateway/Transforms/UserContextHeaderTransform.cs`
- Test: `tests/WmsAi.ArchitectureTests/GatewayRouteConfigTests.cs`

- [ ] **Step 1: 写失败的路由配置测试**

```csharp
[Fact]
public void Gateway_should_route_ai_requests_to_ai_gateway()
{
    var config = LoadGatewayConfig();
    config.Routes.Should().Contain(r => r.RouteId == "ai-gateway");
    config.Routes.Should().Contain(r => r.Match.Path!.StartsWith("/api/ai"));
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `dotnet test tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj --filter GatewayRouteConfigTests`  
Expected: FAIL，`YARP` 路由未配置

- [ ] **Step 3: 实现网关路由**

```json
{
  "ReverseProxy": {
    "Routes": {
      "ai-gateway": {
        "ClusterId": "ai-gateway",
        "Match": { "Path": "/api/ai/{**catch-all}" }
      }
    },
    "Clusters": {
      "ai-gateway": {
        "Destinations": {
          "d1": { "Address": "http://localhost:5203/" }
        }
      }
    }
  }
}
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test tests/WmsAi.ArchitectureTests/WmsAi.ArchitectureTests.csproj --filter GatewayRouteConfigTests`  
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/Apps/WmsAi.Gateway tests/WmsAi.ArchitectureTests
git commit -m "feat: add gateway routing for core services and ai gateway"
```

## Task 9: 单前端项目骨架与 AG-UI 工作台

**Files:**
- Create: `web/src/router/index.ts`
- Create: `web/src/stores/auth.ts`
- Create: `web/src/views/platform/TenantListView.vue`
- Create: `web/src/views/inbound/InboundNoticeListView.vue`
- Create: `web/src/views/workbench/QcWorkbenchView.vue`
- Create: `web/src/components/ai/AiInspectorPanel.vue`
- Test: `tests/WmsAi.WebApp.Tests/qcWorkbench.spec.ts`

- [ ] **Step 1: 写失败的前端工作台测试**

```ts
import { render, screen } from "@testing-library/vue";
import QcWorkbenchView from "../../web/src/views/workbench/QcWorkbenchView.vue";

test("renders ai inspector panel entry", async () => {
  render(QcWorkbenchView);
  expect(await screen.findByText("AI 质检助手")).toBeInTheDocument();
});
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `cd web && npm run test:unit -- qcWorkbench.spec.ts`  
Expected: FAIL，工作台页面和 AI 面板尚未实现

- [ ] **Step 3: 实现单前端骨架与 AG-UI 面板入口**

```ts
export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/platform/tenants", component: () => import("../views/platform/TenantListView.vue") },
    { path: "/inbound/notices", component: () => import("../views/inbound/InboundNoticeListView.vue") },
    { path: "/workbench/qc/:taskId", component: () => import("../views/workbench/QcWorkbenchView.vue") }
  ]
});
```

```vue
<template>
  <section>
    <h1>质检作业台</h1>
    <AiInspectorPanel title="AI 质检助手" />
  </section>
</template>
```

- [ ] **Step 4: 运行测试与前端构建**

Run: `cd web && npm run test:unit -- qcWorkbench.spec.ts && npm run build`  
Expected: PASS，单前端可构建，工作台展示 AI 面板入口

- [ ] **Step 5: 提交**

```bash
git add web tests/WmsAi.WebApp.Tests
git commit -m "feat: add single web app shell and qc workbench"
```

## Task 10: AG-UI 流式事件、观测、端到端烟雾

**Files:**
- Create: `src/Apps/WmsAi.AiGateway.Api/Endpoints/AgUi/StreamSessionEndpoint.cs`
- Create: `src/Apps/WmsAi.AiGateway.Api/Services/AgUiEventStreamWriter.cs`
- Create: `src/Apps/WmsAi.AiGateway.Api/Services/CheckpointCompressionService.cs`
- Create: `src/Apps/WmsAi.Gateway/Observability/OpenTelemetrySetup.cs`
- Create: `tests/WmsAi.E2E/inbound-qc-golden-path.spec.ts`

- [ ] **Step 1: 写失败的端到端黄金链路测试**

```ts
import { test, expect } from "@playwright/test";

test("asn to ai decision golden path", async ({ page }) => {
  await page.goto("/workbench/qc/task-1");
  await expect(page.getByText("AI 质检助手")).toBeVisible();
  await expect(page.getByRole("button", { name: "开始分析" })).toBeVisible();
});
```

- [ ] **Step 2: 运行测试并确认失败**

Run: `cd tests/WmsAi.E2E && npx playwright test inbound-qc-golden-path.spec.ts`  
Expected: FAIL，流式会话入口与工作台交互尚未打通

- [ ] **Step 3: 实现 AG-UI 流式端点和基础观测**

```csharp
app.MapGet("/api/ai/sessions/{sessionId}/stream", async (
    Guid sessionId,
    HttpContext httpContext,
    AgUiEventStreamWriter writer,
    CancellationToken cancellationToken) =>
{
    httpContext.Response.Headers.ContentType = "text/event-stream";
    await writer.WriteSessionBootstrapAsync(sessionId, httpContext.Response, cancellationToken);
});
```

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());
```

- [ ] **Step 4: 运行端到端与全量测试**

Run: `dotnet test wms-ai.sln -v minimal && cd web && npm run build && cd ../tests/WmsAi.E2E && npx playwright test`  
Expected: PASS，黄金路径页面、后端测试、前端构建均通过

- [ ] **Step 5: 提交**

```bash
git add src tests
git commit -m "feat: add ag-ui stream observability and e2e smoke"
```

## Self-Review

### Spec coverage

- 多租户、多仓库、统一账号权限：Task 2, Task 3
- ASN、收货、质检任务、正式结论：Task 4, Task 7
- 图像/规则/操作记录混合输入中的“证据输入”：Task 5
- `AI Gateway` session/checkpoint/压缩/模型配置：Task 6, Task 10
- 单前端项目与 AG-UI：Task 9, Task 10
- Gateway、对象存储、Redis、MQ、观测：Task 1, Task 8, Task 10

### Placeholder scan

- 没有 `TBD`、`TODO`、`后续补` 之类占位词
- 每个 task 都包含明确文件、测试、命令和提交点

### Type consistency

- 平台作用域统一使用 `TenantId + WarehouseId`
- AI 侧对象统一使用 `AiSession / AiCheckpoint / AiSummarySnapshot`
- 业务结论统一落到 `QcDecision`
