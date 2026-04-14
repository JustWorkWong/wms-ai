# AI 模型集成测试指南

## 已完成的改造

### 1. 创建 OpenAI 兼容客户端

**文件：** `WmsAi.AiGateway.Infrastructure/Services/OpenAiCompatibleClient.cs`

- 实现 `IAiModelClient` 接口
- 支持 OpenAI 兼容的 `/chat/completions` 端点
- 自动处理 JSON 序列化/反序列化
- 包含详细的日志记录

### 2. 改造证据缺口智能体

**文件：** `WmsAi.AiGateway.Infrastructure/Agents/EvidenceGapAgent.cs`

**功能：**
- 调用通义千问分析证据完整性
- 使用结构化 Prompt 引导 AI 返回 JSON 格式
- 自动解析 AI 响应（支持 markdown 代码块）
- 失败时自动回退到规则分析

**Prompt 设计：**
- System Prompt：定义角色和输出格式
- User Prompt：提供质检任务上下文、必需证据类型、当前证据列表

### 3. 改造检验决策智能体

**文件：** `WmsAi.AiGateway.Infrastructure/Agents/InspectionDecisionAgent.cs`

**功能：**
- 调用通义千问做出质检决策（Accept/Reject/Conditional）
- 基于证据和质量规则进行推理
- 识别质量问题并给出详细描述
- 失败时自动回退到规则决策

**决策类型：**
- Accept: 质量合格
- Reject: 质量不合格
- Conditional: 需要人工复核（置信度 < 0.7）

## 测试步骤

### 1. 确认配置

检查 `appsettings.Local.json` 是否包含真实 API Key：

```json
{
  "AiProviders": {
    "Qwen": {
      "Endpoint": "https://dashscope.aliyuncs.com/compatible-mode/v1",
      "ApiKey": "sk-b8ad253140944f71912e330f2138db78",
      "DeploymentName": "qwen3-1.7b"
    }
  }
}
```

### 2. 启动后端

```bash
cd src/AppHost/WmsAi.AppHost
dotnet run
```

### 3. 观察日志

启动后，AiGateway 服务会输出：

```
info: WmsAi.AiGateway.Infrastructure.AiGatewayModuleExtensions[0]
      AI Model Client registered successfully
```

### 4. 触发质检工作流

通过 API 创建质检任务，触发智能体调用：

```bash
# 创建质检任务（示例）
curl -X POST http://localhost:5003/api/ai/inspections/start \
  -H "Content-Type: application/json" \
  -d '{
    "qcTaskId": "00000000-0000-0000-0000-000000000001",
    "tenantId": "tenant-001",
    "warehouseId": "warehouse-001",
    "userId": "user-001"
  }'
```

### 5. 检查 AI 调用日志

成功调用时会看到：

```
info: WmsAi.AiGateway.Infrastructure.Agents.EvidenceGapAgent[0]
      Analyzing evidence gaps for QcTask xxx, tenant xxx

info: WmsAi.AiGateway.Infrastructure.Services.OpenAiCompatibleClient[0]
      Calling AI model: qwen3-1.7b, messages: 2, temperature: 0.3

info: WmsAi.AiGateway.Infrastructure.Agents.EvidenceGapAgent[0]
      AI response: {"isComplete":false,"gaps":[...],"reasoning":"...","confidenceScore":0.85}
```

### 6. 测试回退机制

如果 API Key 错误或网络失败，会看到：

```
error: WmsAi.AiGateway.Infrastructure.Agents.EvidenceGapAgent[0]
      Failed to analyze evidence gaps, falling back to rule-based analysis

info: WmsAi.AiGateway.Infrastructure.Agents.EvidenceGapAgent[0]
      Using fallback rule-based analysis
```

## 配置说明

### AI 模型参数

在 `appsettings.json` 中配置：

```json
{
  "AiSettings": {
    "RequestTimeoutSeconds": 60,
    "MaxRetries": 3,
    "RetryDelaySeconds": 2,
    "FallbackToRuleBasedOnError": true
  }
}
```

### 日志级别

调整日志级别以查看详细信息：

```json
{
  "Logging": {
    "LogLevel": {
      "WmsAi.AiGateway.Infrastructure.Agents": "Debug",
      "WmsAi.AiGateway.Infrastructure.Services.OpenAiCompatibleClient": "Debug"
    }
  }
}
```

## 常见问题

### 问题 1：AI 调用返回 401 Unauthorized

**原因：** API Key 错误或已过期

**解决：**
1. 检查 `appsettings.Local.json` 中的 API Key
2. 确认 API Key 有访问权限
3. 检查通义千问控制台是否有余额

### 问题 2：AI 返回的不是 JSON 格式

**原因：** 模型没有严格遵循 Prompt 指令

**解决：**
- 代码已自动处理 markdown 代码块（```json ... ```）
- 如果仍然解析失败，会自动回退到规则分析
- 可以调整 System Prompt 更明确地要求 JSON 格式

### 问题 3：AI 响应太慢

**原因：** 模型推理时间长或网络延迟

**解决：**
1. 调整 `RequestTimeoutSeconds` 配置
2. 切换到更快的模型（如 `qwen-turbo`）
3. 减少 `MaxTokens` 限制

### 问题 4：回退机制一直触发

**原因：** AI 调用持续失败

**排查：**
1. 检查网络连接
2. 查看详细错误日志
3. 验证 API Endpoint 是否正确
4. 确认模型名称是否支持

## 性能优化建议

### 1. 调整 Temperature

- **证据分析**：Temperature = 0.3（更确定性）
- **决策推理**：Temperature = 0.2（更保守）

### 2. 限制 Token 使用

- 证据分析：MaxTokens = 1000
- 决策推理：MaxTokens = 1500

### 3. 启用缓存（未来优化）

对于相同的质检规则，可以缓存 AI 响应。

### 4. 批量处理（未来优化）

如果有多个质检任务，可以批量调用 AI 模型。

## 下一步优化

1. **添加重试策略**：使用 Polly 实现指数退避重试
2. **添加熔断器**：防止 AI 服务故障影响整体系统
3. **Prompt 优化**：根据实际效果调整 Prompt 模板
4. **多模型支持**：支持切换不同的 AI 模型
5. **成本监控**：记录 Token 使用量，监控 API 成本

## 相关文档

- [AI 模型配置指南](ai-model-configuration.md)
- [故障排查指南](troubleshooting.md)
- [AiGateway 架构文档](../architecture/ai-gateway.md)
