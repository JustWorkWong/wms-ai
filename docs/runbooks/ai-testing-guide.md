# AI 质检流程测试指南

## 快速开始

### 一键完整测试（推荐）

```bash
./scripts/run-full-test.sh
```

这个脚本会自动：
1. 启动后端服务（Aspire）
2. 启动前端服务（Vue 3）
3. 等待所有服务就绪
4. 启动日志监控
5. 执行完整的 AI 质检流程测试
6. 检查数据库状态
7. 生成测试报告

### 分步测试

如果你想更细粒度地控制测试流程：

#### 1. 启动后端

```bash
cd src/AppHost/WmsAi.AppHost
dotnet run
```

启动后，控制台会输出 Aspire Dashboard 的地址（例如 `http://localhost:15888`）。

#### 2. 启动前端

```bash
cd web/wms-ai-web
npm install
npm run dev
```

前端地址：`http://localhost:5173`

#### 3. 监控 AI 日志

```bash
./scripts/monitor-ai-logs.sh
```

实时查看 AI 相关的日志输出。

#### 4. 执行集成测试

```bash
./scripts/ai-integration-test.sh
```

自动化测试流程：
- 创建租户和仓库
- 创建到货通知
- 创建质检任务
- 上传质检证据
- 触发 AI 质检流程
- 轮询执行状态
- 获取 AI 决策结果
- 验证数据库状态

#### 5. 检查数据库

```bash
./scripts/check-ai-db.sh
```

查看 AI 相关表的数据：
- `maf_workflow_runs`：MAF 工作流运行记录
- `inspection_runs`：检验运行记录
- `agent_executions`：智能体执行记录

## 配置要求

### 1. API Key 配置

在 `src/AiGateway/WmsAi.AiGateway.Host/appsettings.Local.json` 中配置真实的 API Key：

```json
{
  "AiProviders": {
    "Qwen": {
      "Endpoint": "https://dashscope.aliyuncs.com/compatible-mode/v1",
      "ApiKey": "sk-your-real-api-key-here",
      "DeploymentName": "qwen3-1.7b"
    }
  }
}
```

**重要：** `appsettings.Local.json` 不会提交到 Git，请手动创建。

### 2. 数据库配置

确保 PostgreSQL 已启动，数据库连接信息正确：

```bash
# 检查 PostgreSQL 状态
psql -h localhost -p 5432 -U wmsai -d wmsai_ai -c "SELECT 1;"
```

### 3. RabbitMQ 配置

确保 RabbitMQ 已启动：

```bash
# 检查 RabbitMQ 状态
curl -u wmsai:wmsai http://localhost:15672/api/overview
```

## 测试场景

### 场景 1：完整证据 + 合格质量

```bash
# 上传所有必需证据
curl -X POST http://localhost:5002/api/qc-tasks/{taskId}/evidences \
  -H "Content-Type: application/json" \
  -d '{
    "evidenceType": "Photo",
    "fileUrl": "https://example.com/photo.jpg"
  }'

curl -X POST http://localhost:5002/api/qc-tasks/{taskId}/evidences \
  -H "Content-Type: application/json" \
  -d '{
    "evidenceType": "Measurement",
    "measurementValue": 99.5
  }'

# 触发 AI 质检
curl -X POST http://localhost:5003/api/ai/inspections/start \
  -H "Content-Type: application/json" \
  -d '{
    "qcTaskId": "{taskId}"
  }'
```

**预期结果：**
- 证据缺口智能体：`isComplete: true`
- 检验决策智能体：`decision: Accept`
- 置信度：> 0.7

### 场景 2：证据不完整

```bash
# 只上传部分证据
curl -X POST http://localhost:5002/api/qc-tasks/{taskId}/evidences \
  -H "Content-Type: application/json" \
  -d '{
    "evidenceType": "Photo",
    "fileUrl": "https://example.com/photo.jpg"
  }'

# 触发 AI 质检
curl -X POST http://localhost:5003/api/ai/inspections/start \
  -H "Content-Type: application/json" \
  -d '{
    "qcTaskId": "{taskId}"
  }'
```

**预期结果：**
- 证据缺口智能体：`isComplete: false`, `gaps: ["Measurement"]`
- 检验决策智能体：`decision: Conditional`（需要人工复核）

### 场景 3：质量不合格

```bash
# 上传不合格的测量数据
curl -X POST http://localhost:5002/api/qc-tasks/{taskId}/evidences \
  -H "Content-Type: application/json" \
  -d '{
    "evidenceType": "Measurement",
    "measurementValue": 50.0
  }'

# 触发 AI 质检
curl -X POST http://localhost:5003/api/ai/inspections/start \
  -H "Content-Type: application/json" \
  -d '{
    "qcTaskId": "{taskId}"
  }'
```

**预期结果：**
- 检验决策智能体：`decision: Reject`
- 质量问题：`["尺寸不符合标准"]`

## 日志分析

### 成功调用 AI 的日志

```
info: WmsAi.AiGateway.Infrastructure.Agents.EvidenceGapAgent[0]
      Analyzing evidence gaps for QcTask xxx, tenant xxx

info: WmsAi.AiGateway.Infrastructure.Services.OpenAiCompatibleClient[0]
      Calling AI model: qwen3-1.7b, messages: 2, temperature: 0.3

info: WmsAi.AiGateway.Infrastructure.Agents.EvidenceGapAgent[0]
      AI response: {"isComplete":false,"gaps":[...],"reasoning":"...","confidenceScore":0.85}
```

### 回退到规则分析的日志

```
error: WmsAi.AiGateway.Infrastructure.Agents.EvidenceGapAgent[0]
      Failed to analyze evidence gaps, falling back to rule-based analysis

info: WmsAi.AiGateway.Infrastructure.Agents.EvidenceGapAgent[0]
      Using fallback rule-based analysis
```

## 数据库验证

### 查看 MAF 工作流运行记录

```sql
SELECT
    id,
    workflow_type,
    status,
    created_at,
    completed_at
FROM maf_workflow_runs
ORDER BY created_at DESC
LIMIT 10;
```

### 查看检验运行记录

```sql
SELECT
    id,
    qc_task_id,
    decision,
    confidence_score,
    created_at
FROM inspection_runs
ORDER BY created_at DESC
LIMIT 10;
```

### 查看智能体执行记录

```sql
SELECT
    workflow_run_id,
    agent_name,
    status,
    execution_time_ms,
    created_at
FROM agent_executions
ORDER BY created_at DESC
LIMIT 10;
```

## 常见问题

### 问题 1：服务启动失败

**排查步骤：**
1. 检查端口是否被占用：`lsof -i :5001`
2. 检查数据库连接：`psql -h localhost -p 5432 -U wmsai -d wmsai_ai`
3. 检查 RabbitMQ 连接：`curl http://localhost:15672`

### 问题 2：AI 调用返回 401

**原因：** API Key 错误或已过期

**解决：**
1. 检查 `appsettings.Local.json` 中的 API Key
2. 确认 API Key 有访问权限
3. 检查通义千问控制台是否有余额

### 问题 3：AI 响应太慢

**原因：** 模型推理时间长或网络延迟

**解决：**
1. 调整 `RequestTimeoutSeconds` 配置
2. 切换到更快的模型（如 `qwen-turbo`）
3. 减少 `MaxTokens` 限制

### 问题 4：前端无法连接后端

**排查步骤：**
1. 检查 CORS 配置
2. 确认后端服务已启动
3. 检查前端 `.env` 文件中的 API 地址

## 性能基准

### 预期性能指标

- **证据缺口分析**：< 2 秒
- **检验决策**：< 3 秒
- **完整流程**：< 5 秒

### 监控指标

- **AI 调用成功率**：> 95%
- **回退到规则分析的比例**：< 5%
- **平均置信度**：> 0.7

## 下一步

1. **添加更多测试场景**：边界情况、异常输入
2. **性能测试**：并发质检任务
3. **压力测试**：大量证据上传
4. **成本监控**：Token 使用量统计

## 相关文档

- [AI 模型集成测试指南](ai-integration-testing.md)
- [故障排查指南](troubleshooting.md)
- [AiGateway 架构文档](../architecture/ai-gateway.md)
