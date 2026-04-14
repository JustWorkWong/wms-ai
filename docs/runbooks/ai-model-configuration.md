# AI 模型配置指南

## 配置方式

### 1. 本地开发环境（推荐）

使用 `appsettings.Local.json`（已在 .gitignore 中排除）：

```bash
cd src/AiGateway/WmsAi.AiGateway.Host
cp appsettings.json appsettings.Local.json
```

编辑 `appsettings.Local.json`：

```json
{
  "AiProviders": {
    "Qwen": {
      "Endpoint": "https://dashscope.aliyuncs.com/compatible-mode/v1",
      "ApiKey": "sk-your-actual-api-key-here",
      "DeploymentName": "qwen3-1.7b"
    }
  }
}
```

### 2. 使用 .NET User Secrets（可选）

```bash
cd src/AiGateway/WmsAi.AiGateway.Host
dotnet user-secrets init
dotnet user-secrets set "AiProviders:Qwen:ApiKey" "sk-your-actual-api-key-here"
```

### 3. 使用环境变量（生产环境）

```bash
export AiProviders__Qwen__Endpoint="https://dashscope.aliyuncs.com/compatible-mode/v1"
export AiProviders__Qwen__ApiKey="sk-your-actual-api-key-here"
export AiProviders__Qwen__DeploymentName="qwen3-1.7b"
```

## 配置优先级

从高到低：

1. 环境变量
2. User Secrets
3. `appsettings.Local.json`
4. `appsettings.Development.json`
5. `appsettings.json`

## 支持的 AI 模型

### 通义千问（DashScope）

```json
{
  "AiProviders": {
    "Qwen": {
      "Endpoint": "https://dashscope.aliyuncs.com/compatible-mode/v1",
      "ApiKey": "sk-xxx",
      "DeploymentName": "qwen3-1.7b"
    }
  }
}
```

可用模型：
- `qwen3-1.7b`
- `qwen-turbo`
- `qwen-plus`
- `qwen-max`

### DeepSeek

```json
{
  "AiProviders": {
    "DeepSeek": {
      "Endpoint": "https://api.deepseek.com/v1",
      "ApiKey": "sk-xxx",
      "DeploymentName": "deepseek-chat"
    }
  }
}
```

### OpenAI

```json
{
  "AiProviders": {
    "OpenAI": {
      "Endpoint": "https://api.openai.com/v1",
      "ApiKey": "sk-xxx",
      "DeploymentName": "gpt-4"
    }
  }
}
```

### Azure OpenAI

```json
{
  "AiProviders": {
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com",
      "ApiKey": "xxx",
      "DeploymentName": "gpt-4-deployment-name"
    }
  }
}
```

## 安全最佳实践

### ✅ 推荐做法

1. **本地开发**：使用 `appsettings.Local.json` 或 User Secrets
2. **CI/CD**：使用环境变量或密钥管理服务
3. **生产环境**：使用 Azure Key Vault、AWS Secrets Manager 等
4. **团队协作**：在 README 中说明配置方式，但不提交实际密钥

### ❌ 禁止做法

1. 不要把密钥写在 `appsettings.json` 或 `appsettings.Development.json` 中
2. 不要把密钥提交到 Git 仓库
3. 不要在代码中硬编码密钥
4. 不要在日志中打印密钥

## 验证配置

启动 AiGateway 服务后，检查日志：

```bash
cd src/AppHost/WmsAi.AppHost
dotnet run
```

如果配置正确，日志中会显示：

```
info: WmsAi.AiGateway.Infrastructure.AiGatewayModuleExtensions[0]
      AI Model Client configured: https://dashscope.aliyuncs.com/compatible-mode/v1
```

如果配置错误，会抛出异常：

```
System.InvalidOperationException: AiProviders:Qwen:Endpoint not configured
```

## 故障排查

### 问题：启动时报错 "AiProviders:Qwen:Endpoint not configured"

**原因：** 未配置 AI 模型端点

**解决：** 创建 `appsettings.Local.json` 并填入配置

### 问题：API 调用返回 401 Unauthorized

**原因：** API Key 错误或已过期

**解决：** 检查 API Key 是否正确，是否有访问权限

### 问题：API 调用超时

**原因：** 网络问题或模型响应慢

**解决：** 
1. 检查网络连接
2. 调整超时时间（默认 60 秒）
3. 切换到更快的模型

## 多模型配置（高级）

如果需要同时配置多个模型提供商：

```json
{
  "AiProviders": {
    "Qwen": {
      "Endpoint": "https://dashscope.aliyuncs.com/compatible-mode/v1",
      "ApiKey": "sk-xxx",
      "DeploymentName": "qwen3-1.7b"
    },
    "DeepSeek": {
      "Endpoint": "https://api.deepseek.com/v1",
      "ApiKey": "sk-xxx",
      "DeploymentName": "deepseek-chat"
    }
  }
}
```

在代码中通过 `IConfiguration` 读取不同的配置：

```csharp
var qwenEndpoint = configuration["AiProviders:Qwen:Endpoint"];
var deepseekEndpoint = configuration["AiProviders:DeepSeek:Endpoint"];
```

## 相关文档

- [本地开发指南](local-development.md)
- [故障排查指南](troubleshooting.md)
- [AiGateway 架构文档](../architecture/ai-gateway.md)
