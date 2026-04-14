# AI 质检流程测试 - 最佳实践

## 已完成的准备工作

✅ API Key 已配置（`appsettings.Local.json`）
✅ 后端服务已启动（Aspire Dashboard: http://localhost:15170）
✅ 测试脚本已创建并执行成功

## 验证 AI 流程的步骤

### 1. 打开 Aspire Dashboard

```bash
# Dashboard 地址（已启动）
http://localhost:15170/login?t=be73abaf8225a13de6b7b5487d3540b8
```

### 2. 查看 AiGateway 服务日志

在 Dashboard 中：
1. 点击左侧 "Resources"
2. 找到 "aigateway" 服务
3. 点击 "Logs" 标签
4. 搜索关键词：
   - `QcTaskCreatedV1` - 确认收到事件
   - `EvidenceGapAgent` - 证据分析智能体
   - `InspectionDecisionAgent` - 决策智能体
   - `OpenAiCompatibleClient` - AI 模型调用
   - `Calling AI model: qwen3-1.7b` - 实际 API 调用
   - `AI response:` - AI 返回结果

### 3. 预期的日志输出

**成功的日志应该包含：**

```
info: WmsAi.AiGateway.Host.Events.InboundEventConsumer
      Received QcTaskCreatedV1 event: QcTaskId=xxx, TaskNo=RCV-xxx-QC-001

info: WmsAi.AiGateway.Infrastructure.Agents.EvidenceGapAgent
      Analyzing evidence gaps for QcTask xxx

info: WmsAi.AiGateway.Infrastructure.Services.OpenAiCompatibleClient
      Calling AI model: qwen3-1.7b, messages: 2, temperature: 0.3

info: WmsAi.AiGateway.Infrastructure.Agents.EvidenceGapAgent
      AI response: {"isComplete":false,"gaps":["Photo","Measurement"],...}

info: WmsAi.AiGateway.Infrastructure.Agents.InspectionDecisionAgent
      Making inspection decision for QcTask xxx

info: WmsAi.AiGateway.Infrastructure.Services.OpenAiCompatibleClient
      Calling AI model: qwen3-1.7b, messages: 2, temperature: 0.2

info: WmsAi.AiGateway.Infrastructure.Agents.InspectionDecisionAgent
      AI response: {"decision":"Conditional","reasoning":"...","confidenceScore":0.65}
```

**如果看到回退日志：**

```
error: WmsAi.AiGateway.Infrastructure.Agents.EvidenceGapAgent
      Failed to analyze evidence gaps, falling back to rule-based analysis

info: WmsAi.AiGateway.Infrastructure.Agents.EvidenceGapAgent
      Using fallback rule-based analysis
```

说明 AI 调用失败，但系统自动回退到规则分析。

### 4. 检查 CAP 事件表（可选）

如果想确认事件是否发布成功：

```bash
# 连接到 BusinessDb
psql -h localhost -p 5432 -U postgres -d BusinessDb

# 查看已发布的事件
SELECT * FROM cap.published ORDER BY added DESC LIMIT 10;

# 查看已接收的事件
SELECT * FROM cap.received ORDER BY added DESC LIMIT 10;
```

### 5. 再次运行测试

```bash
cd /Users/tengfengsu/wfcodes/wms-ai

# 运行测试脚本
./scripts/ai-event-driven-test.sh

# 或者快速测试
TENANT_ID="test-tenant-$(date +%s)"
WAREHOUSE_ID="test-warehouse-$(date +%s)"

# 创建到货通知
curl -X POST "http://localhost:5002/api/inbound/notices" \
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
  }" | jq '.'

# 记录收货（触发 AI）
NOTICE_ID="<上面返回的 inboundNoticeId>"
curl -X POST "http://localhost:5002/api/inbound/receipts" \
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
  }" | jq '.'

# 立即查看 AiGateway 日志
```

## 常见问题排查

### 问题 1：没有看到 AI 调用日志

**可能原因：**
- CAP 事件订阅未生效
- RabbitMQ 连接失败
- AiGateway 服务未正确注册事件处理器

**排查：**
1. 检查 RabbitMQ 管理后台：http://localhost:15672（wmsai/wmsai）
2. 查看是否有 `qctask.created.v1` 队列
3. 查看队列中是否有消息堆积

### 问题 2：看到 AI 调用但返回 401

**原因：** API Key 错误或已过期

**解决：**
1. 检查 `appsettings.Local.json` 中的 API Key
2. 确认通义千问控制台有余额
3. 测试 API Key：
```bash
curl https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions \
  -H "Authorization: Bearer sk-b8ad253140944f71912e330f2138db78" \
  -H "Content-Type: application/json" \
  -d '{"model":"qwen3-1.7b","messages":[{"role":"user","content":"test"}]}'
```

### 问题 3：AI 一直回退到规则分析

**可能原因：**
- 网络问题
- API 响应格式不符合预期
- 模型返回非 JSON 格式

**解决：**
1. 查看详细错误日志
2. 调整 Prompt 模板
3. 切换到更稳定的模型（如 `qwen-turbo`）

## 成功标志

当你看到以下内容时，说明 AI 流程完全正常：

✅ CAP 事件成功发布和消费
✅ EvidenceGapAgent 调用 AI 并返回结果
✅ InspectionDecisionAgent 调用 AI 并返回决策
✅ 没有回退到规则分析
✅ 置信度 > 0.7

## 下一步优化

1. **添加证据上传**：测试有证据和无证据的不同场景
2. **测试不同决策**：Accept、Reject、Conditional
3. **性能测试**：并发创建多个质检任务
4. **监控 Token 使用**：记录 API 成本

## 相关文档

- [AI 模型集成测试指南](ai-integration-testing.md)
- [故障排查指南](troubleshooting.md)
- [AiGateway 架构文档](../architecture/ai-gateway.md)
