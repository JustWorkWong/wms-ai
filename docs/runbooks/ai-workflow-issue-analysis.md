# AI 质检流程问题分析报告

## 问题总结

✅ **已确认工作的部分：**
1. 后端服务（Aspire）正常启动
2. API 端点正常工作（创建到货通知、记录收货、查询质检任务）
3. CAP 事件成功发布到 RabbitMQ（BusinessDb.cap.published 表有记录）
4. RabbitMQ 交换机 `wmsai.events` 已创建
5. 项目结构与文档完全一致

❌ **问题所在：**
1. **AiGateway 没有接收到 CAP 事件**（AiDb.cap.received 表为空）
2. 没有 MAF 工作流运行记录
3. 没有 AI 检验运行记录
4. 没有 AI 相关日志输出

## 根本原因

**CAP 订阅者未被正确注册和发现**

### 证据

1. **数据库查询结果：**
   ```
   BusinessDb.cap.published: 3 条 qctask.created.v1 事件 ✓
   AiDb.cap.received: 0 条记录 ✗
   maf_workflow_runs: 0 条记录 ✗
   ai_inspection_runs: 0 条记录 ✗
   ```

2. **RabbitMQ 状态：**
   - 交换机 `wmsai.events` 存在 ✓
   - 但没有为 AiGateway 创建订阅队列 ✗

3. **日志状态：**
   - 没有任何 `QcTaskCreatedV1`、`EvidenceGapAgent`、`InspectionDecisionAgent` 相关日志

### 技术分析

CAP 框架需要满足以下条件才能正确订阅事件：

1. ✓ 订阅者类需要注册到 DI 容器
2. ✓ 订阅方法需要 `[CapSubscribe]` 特性
3. ✗ **订阅者类需要被实例化或通过某种方式让 CAP 发现**

当前问题：
```csharp
// Program.cs
builder.Services.AddScoped<InboundEventConsumer>();  // 只注册，但从未实例化
```

CAP 在启动时扫描已注册的服务，但 **Scoped 服务只有在请求时才会创建实例**。如果没有任何地方主动使用 `InboundEventConsumer`，CAP 可能无法发现它的订阅方法。

## 解决方案

### 方案 1：将订阅者注册为 Singleton（推荐）

```csharp
// WmsAi.AiGateway.Host/Program.cs
builder.Services.AddSingleton<InboundEventConsumer>();
```

**优点：**
- CAP 在启动时就能发现订阅者
- 简单直接

**缺点：**
- 如果 InboundEventConsumer 依赖 Scoped 服务（如 DbContext），需要手动管理 Scope

### 方案 2：在启动时强制实例化订阅者

```csharp
// WmsAi.AiGateway.Host/Program.cs
var app = builder.Build();

// 初始化数据库
await AiGatewayDatabaseInitializer.InitializeAsync(app.Services);

// 强制实例化订阅者，让 CAP 发现它
using (var scope = app.Services.CreateScope())
{
    var _ = scope.ServiceProvider.GetRequiredService<InboundEventConsumer>();
}

app.Run();
```

### 方案 3：使用 CAP 的 ICapSubscribe 接口（最佳实践）

```csharp
// InboundEventConsumer.cs
public sealed class InboundEventConsumer : ICapSubscribe  // 实现 CAP 接口
{
    // ... 现有代码
}

// Program.cs
builder.Services.AddSingleton<ICapSubscribe, InboundEventConsumer>();
```

**优点：**
- 明确告诉 CAP 这是一个订阅者
- 符合 CAP 框架的最佳实践

## 验证步骤

修复后，按以下步骤验证：

### 1. 重启服务

```bash
# 停止当前服务
# 重新启动
cd src/AppHost/WmsAi.AppHost
dotnet run
```

### 2. 运行测试

```bash
cd /Users/tengfengsu/wfcodes/wms-ai

TENANT_ID="test-tenant-$(date +%s)"
WAREHOUSE_ID="test-warehouse-$(date +%s)"

# 创建到货通知
NOTICE_RESPONSE=$(curl -s -X POST "http://localhost:5002/api/inbound/notices" \
  -H "Content-Type: application/json" \
  -d "{
    \"tenantId\": \"$TENANT_ID\",
    \"warehouseId\": \"$WAREHOUSE_ID\",
    \"noticeNo\": \"IBN-$(date +%s)\",
    \"lines\": [{
      \"skuCode\": \"TEST-SKU-001\",
      \"productName\": \"测试商品\",
      \"expectedQuantity\": 100
    }]
  }")

NOTICE_ID=$(echo "$NOTICE_RESPONSE" | jq -r '.inboundNoticeId')

# 记录收货（触发 AI）
curl -s -X POST "http://localhost:5002/api/inbound/receipts" \
  -H "Content-Type: application/json" \
  -d "{
    \"tenantId\": \"$TENANT_ID\",
    \"warehouseId\": \"$WAREHOUSE_ID\",
    \"inboundNoticeId\": \"$NOTICE_ID\",
    \"receiptNo\": \"RCV-$(date +%s)\",
    \"lines\": [{
      \"skuCode\": \"TEST-SKU-001\",
      \"receivedQuantity\": 100
    }]
  }"

sleep 5
```

### 3. 查询数据库验证

```bash
export PATH="/opt/homebrew/opt/postgresql@16/bin:$PATH"

# 查询 CAP 接收记录
psql "postgresql://postgres:postgres@localhost:60037/AiDb" -c "
SELECT \"Name\", \"Added\", \"StatusName\"
FROM cap.received
WHERE \"Name\" = 'qctask.created.v1'
ORDER BY \"Added\" DESC
LIMIT 3;"

# 查询 MAF 工作流
psql "postgresql://postgres:postgres@localhost:60037/AiDb" -c "
SELECT \"Id\", \"WorkflowName\", \"Status\", \"CreatedAt\"
FROM maf_workflow_runs
ORDER BY \"CreatedAt\" DESC
LIMIT 3;"
```

### 4. 检查 RabbitMQ 队列

```bash
curl -s -u wmsai:wmsai http://localhost:60015/api/queues | jq '.[] | {name: .name, messages: .messages}'
```

**预期结果：**
- 应该看到为 AiGateway 创建的队列（如 `cap.queue.aigateway.qctask.created.v1`）
- 队列中消息数为 0（已消费）

### 5. 查看 Aspire Dashboard 日志

打开 http://localhost:15170，在 AiGateway 服务日志中搜索：
- `Received QcTaskCreatedV1 event`
- `EvidenceGapAgent`
- `InspectionDecisionAgent`
- `Calling AI model: qwen3-1.7b`

## 项目结构验证

✅ **已确认与文档一致：**

```
src/
├── Platform/          ✓ UserDb
├── Inbound/           ✓ BusinessDb
├── AiGateway/         ✓ AiDb
│   ├── Domain/
│   │   ├── Workflows/
│   │   │   └── MafWorkflowRun.cs ✓
│   │   └── Inspections/
│   │       └── AiInspectionRun.cs ✓
│   ├── Infrastructure/
│   │   ├── Agents/
│   │   │   ├── EvidenceGapAgent.cs ✓
│   │   │   └── InspectionDecisionAgent.cs ✓
│   │   └── Services/
│   │       └── OpenAiCompatibleClient.cs ✓
│   └── Host/
│       └── Events/
│           └── InboundEventConsumer.cs ✓
├── Operations/        ✓ HangfireDb
├── Gateway/           ✓ YARP
└── BuildingBlocks/
    └── WmsAi.Contracts/
        └── Events/
            └── QcTaskCreatedV1.cs ✓
```

## 下一步行动

1. **立即修复：** 应用方案 3（实现 ICapSubscribe 接口）
2. **验证修复：** 按上述验证步骤确认 AI 流程正常工作
3. **补充文档：** 更新 `docs/runbooks/ai-testing-best-practices.md`，说明 CAP 订阅者的正确注册方式
4. **添加监控：** 在 Aspire Dashboard 中配置 CAP 事件监控

## 相关文档

- [CAP 官方文档 - 订阅者](https://cap.dotnetcore.xyz/user-guide/zh/cap/subscribe/)
- [AI 模型集成测试指南](ai-integration-testing.md)
- [故障排查指南](troubleshooting.md)
