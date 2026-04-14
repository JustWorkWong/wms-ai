# AI 质检流程问题解决方案

## 🎉 重大进展

✅ **已成功修复 CAP 订阅问题**
- InboundEventConsumer 实现了 ICapSubscribe 接口
- CAP 成功创建订阅队列：`cap.queue.wmsai.aigateway.host.v1`
- 事件成功接收并触发 AI 工作流

✅ **AI 工作流已执行**
- CAP 事件已接收（AiDb.cap.received 有记录）
- MAF 工作流已创建（maf_workflow_runs 有记录）
- AI 检验记录已创建（ai_inspection_runs 有记录）

## ❌ 当前问题

**错误信息：** `QC task not found: 0f216714-4b1a-438f-b4ed-d2e8caf8f8cd`

**根本原因：** API 端点不匹配

### 问题分析

1. **AiGateway 期望的 API：**
   ```
   GET /api/inbound/qc/tasks/{qcTaskId}
   ```

2. **Inbound 实际提供的 API：**
   ```
   GET /api/inbound/qc/tasks?tenantId={tenantId}&warehouseId={warehouseId}
   ```

3. **缺失的端点：**
   - 按 ID 获取单个质检任务详情
   - 获取质检任务的证据列表
   - 获取 SKU 的质量规则

## 解决方案

### 方案 1：补充 Inbound API 端点（推荐）

在 Inbound 服务中添加缺失的 API 端点：

```csharp
// WmsAi.Inbound.Host/Program.cs

// 1. 按 ID 获取质检任务详情
app.MapGet("/api/inbound/qc/tasks/{qcTaskId:guid}", async (
    Guid qcTaskId,
    string tenantId,
    string warehouseId,
    GetQcTaskByIdHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.Handle(qcTaskId, tenantId, warehouseId, cancellationToken);
    return result != null ? Results.Ok(result) : Results.NotFound();
});

// 2. 获取质检任务的证据列表
app.MapGet("/api/inbound/qc/tasks/{qcTaskId:guid}/evidence", async (
    Guid qcTaskId,
    string tenantId,
    string warehouseId,
    GetQcEvidenceHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.Handle(qcTaskId, tenantId, warehouseId, cancellationToken);
    return Results.Ok(result);
});

// 3. 获取 SKU 质量规则
app.MapGet("/api/inbound/skus/{skuCode}/quality-profile", async (
    string skuCode,
    string tenantId,
    GetSkuQualityProfileHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.Handle(skuCode, tenantId, cancellationToken);
    return result != null ? Results.Ok(result) : Results.NotFound();
});
```

**需要实现的 Handler：**
1. `GetQcTaskByIdHandler` - 从数据库查询单个质检任务
2. `GetQcEvidenceHandler` - 查询质检任务的证据记录
3. `GetSkuQualityProfileHandler` - 查询 SKU 的质量规则

### 方案 2：修改 AiGateway 使用现有 API（临时方案）

修改 `InboundBusinessFunctions` 使用列表查询 API：

```csharp
public async Task<QcTaskDetails?> GetQcTaskDetailsAsync(
    Guid qcTaskId,
    string tenantId,
    string warehouseId,
    CancellationToken cancellationToken = default)
{
    // 使用列表 API 并过滤
    var response = await _apiClient.GetAsync<List<QcTaskDetailsDto>>(
        $"/api/inbound/qc/tasks?tenantId={tenantId}&warehouseId={warehouseId}",
        tenantId,
        warehouseId,
        cancellationToken);

    if (response == null)
        return null;

    var task = response.FirstOrDefault(t => t.QcTaskId == qcTaskId);
    if (task == null)
        return null;

    return new QcTaskDetails(
        task.QcTaskId,
        task.TaskNo,
        task.SkuCode,
        task.Quantity,
        task.Status,
        task.InboundNoticeId,
        task.ReceiptId);
}
```

**缺点：**
- 性能较差（需要查询所有任务再过滤）
- 无法获取证据和质量规则（这些端点完全不存在）

## 推荐实施步骤

### 第一步：补充 Inbound API 端点

1. **创建 Handler**

```csharp
// WmsAi.Inbound.Application/Qc/GetQcTaskByIdHandler.cs
public sealed class GetQcTaskByIdHandler(IBusinessDbContext dbContext)
{
    public async Task<QcTaskDetailsDto?> Handle(
        Guid qcTaskId,
        string tenantId,
        string warehouseId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.QcTasks
            .Where(t => t.Id == qcTaskId 
                && t.TenantId == tenantId 
                && t.WarehouseId == warehouseId)
            .Select(t => new QcTaskDetailsDto(
                t.Id,
                t.TaskNo,
                t.SkuCode,
                100, // TODO: 从 Receipt 获取实际数量
                t.Status.ToString(),
                t.InboundNoticeId,
                t.ReceiptId))
            .FirstOrDefaultAsync(cancellationToken);

        return task;
    }
}

public sealed record QcTaskDetailsDto(
    Guid QcTaskId,
    string TaskNo,
    string SkuCode,
    decimal Quantity,
    string Status,
    Guid InboundNoticeId,
    Guid ReceiptId);
```

2. **注册 Handler**

```csharp
// WmsAi.Inbound.Infrastructure/Persistence/BusinessDbContext.cs
services.AddScoped<GetQcTaskByIdHandler>();
services.AddScoped<GetQcEvidenceHandler>();
services.AddScoped<GetSkuQualityProfileHandler>();
```

3. **添加 API 端点**（见方案 1）

### 第二步：验证修复

```bash
# 1. 重启服务
cd src/AppHost/WmsAi.AppHost
dotnet run

# 2. 测试新端点
curl "http://localhost:5002/api/inbound/qc/tasks/{qcTaskId}" \
  -H "X-Tenant-Id: test-tenant" \
  -H "X-Warehouse-Id: test-warehouse"

# 3. 运行完整测试
./scripts/ai-event-driven-test.sh

# 4. 查询数据库验证
psql "postgresql://postgres:postgres@localhost:60970/AiDb" -c "
SELECT \"Status\" FROM ai_inspection_runs ORDER BY \"CreatedAt\" DESC LIMIT 1;"
```

**预期结果：**
- 状态应该是 `Completed` 或 `WaitingManualReview`（而不是 `Failed`）
- 应该有 AI 调用日志（EvidenceGapAgent、InspectionDecisionAgent）

## 当前状态总结

### ✅ 已完成
1. CAP 订阅者注册修复
2. 事件发布和接收正常
3. AI 工作流触发成功

### ⚠️ 待完成
1. 补充 Inbound API 端点
2. 实现 Handler 逻辑
3. 验证完整的 AI 决策流程

### 📊 项目结构验证
- ✅ 与文档完全一致
- ✅ 所有关键文件都存在
- ✅ CAP 事件契约正确

## 下一步行动

**立即执行：** 补充 Inbound API 端点（方案 1）

**优先级：**
1. 高：`GET /api/inbound/qc/tasks/{qcTaskId}` - 获取任务详情
2. 中：`GET /api/inbound/qc/tasks/{qcTaskId}/evidence` - 获取证据
3. 低：`GET /api/inbound/skus/{skuCode}/quality-profile` - 获取质量规则（可以先返回空）

## 相关文档

- [AI 工作流问题分析](ai-workflow-issue-analysis.md)
- [AI 测试最佳实践](ai-testing-best-practices.md)
- [CAP 订阅者注册指南](cap-subscriber-registration.md)
