# WMS AI - 智能仓储管理系统

WMS AI 是一个基于领域驱动设计（DDD）、事件驱动架构和 CQRS 模式构建的 AI 仓储管理系统。系统通过多智能体工作流自动完成质检判断，并协助优化仓内作业流程。

## 项目价值

WMS AI 重点解决传统仓储系统里的几个问题：

- **AI 驱动质检**：通过证据缺口智能体与检验决策智能体协同完成自动质检。
- **事件驱动协作**：通过 CAP + RabbitMQ 完成跨服务实时通信和最终一致性。
- **多租户隔离**：按租户与仓库维度隔离数据与操作上下文。
- **可扩展微服务**：以限界上下文为边界拆分服务，职责清晰。
- **现代技术栈**：后端使用 .NET 10，前端使用 Vue 3，后端编排使用 Aspire。

## 架构概览

### 设计原则

- **领域驱动设计（DDD）**：按 Platform、Inbound、AiGateway 三个核心限界上下文建模。
- **事件驱动架构**：通过领域事件和消息总线解耦服务。
- **CQRS**：在适合的场景中拆分读写职责。
- **整洁架构**：采用 Domain → Application → Infrastructure → Host 分层。

### 限界上下文

1. **Platform**：租户、仓库、用户、成员关系管理。
2. **Inbound**：到货通知、收货、质检任务、质检结论。
3. **AiGateway**：AI 工作流、MAF 会话、检验运行、模型配置。
4. **Operations**：后台任务与定时任务（Hangfire）。
5. **Gateway**：YARP 网关，负责聚合路由与统一入口。

## 技术栈

### 后端

- **.NET 10**：使用最新运行时与语言特性。
- **PostgreSQL 16**：主数据库，支持 JSONB。
- **Entity Framework Core**：ORM、拦截器与持久化实现。
- **DotNetCore.CAP**：跨服务事务消息与最终一致性。
- **RabbitMQ**：事件总线消息中间件。
- **Hangfire**：后台任务调度。
- **YARP**：网关反向代理。

### 前端

- **Vue 3**：Composition API + TypeScript。
- **Element Plus**：组件库。
- **Pinia**：状态管理。
- **Axios**：HTTP 客户端。
- **Vite**：开发与构建工具。

### 基础设施

- **.NET Aspire**：统一编排与服务发现。
- **Redis**：缓存与会话状态。
- **MinIO**：兼容 S3 的对象存储。
- **Nacos**：配置中心与服务注册中心。

## 快速开始

### 环境要求

- .NET 10 SDK
- Node.js 20+
- Docker Desktop
- PostgreSQL 16（如果不使用 Docker）

### 配置 AI 模型密钥

**重要：密钥配置不会提交到 Git**

创建本地配置文件（已在 .gitignore 中排除）：

```bash
# 在 AiGateway 服务目录下创建本地配置
cd src/AiGateway/WmsAi.AiGateway.Host
cp appsettings.json appsettings.Local.json
```

编辑 `appsettings.Local.json`，填入真实密钥：

```json
{
  "AiProviders": {
    "Qwen": {
      "Endpoint": "https://dashscope.aliyuncs.com/compatible-mode/v1",
      "ApiKey": "sk-your-actual-api-key-here",
      "DeploymentName": "qwen3-1.7b"
    }
  },
  "ConnectionStrings": {
    "AiDb": "Host=localhost;Database=AiDb;Username=postgres;Password=postgres",
    "RabbitMQ": "amqp://wmsai:wmsai@localhost:5672"
  }
}
```

**配置优先级：** `appsettings.Local.json` > `appsettings.Development.json` > `appsettings.json`

**支持的 AI 模型：**
- 通义千问（DashScope）
- DeepSeek
- 任何支持 OpenAI 兼容接口的模型

### 使用 Aspire 启动后端

```bash
git clone <repository-url>
cd wms-ai
cd src/AppHost/WmsAi.AppHost
dotnet run
```

启动后，控制台会输出 Aspire Dashboard 的实际地址和登录链接。不同机器上端口可能不同，不要再假定固定是 `15888`。

### 启动前端

```bash
cd web/wms-ai-web
npm install
npm run dev
```

前端默认访问地址：`http://localhost:5173`

### 常用访问地址

- **Aspire Dashboard**：以控制台输出为准
- **Gateway**：通常为 `http://localhost:5000`
- **Platform**：`http://localhost:5001`
- **Inbound**：`http://localhost:5002`
- **AiGateway**：`http://localhost:5003`
- **Operations**：`http://localhost:5004`
- **前端**：`http://localhost:5173`
- **RabbitMQ 管理后台**：`http://localhost:15672`（`wmsai/wmsai`）
- **MinIO 控制台**：`http://localhost:9001`（`minioadmin/minioadmin`）
- **Nacos 控制台**：`http://localhost:8848/nacos`（`nacos/nacos`）

## 项目结构

```text
wms-ai/
├── src/
│   ├── AppHost/                    # Aspire 编排入口
│   │   └── WmsAi.AppHost/
│   ├── Platform/                   # 平台域
│   │   ├── WmsAi.Platform.Domain/
│   │   ├── WmsAi.Platform.Application/
│   │   ├── WmsAi.Platform.Infrastructure/
│   │   └── WmsAi.Platform.Host/
│   ├── Inbound/                    # 入库域
│   │   ├── WmsAi.Inbound.Domain/
│   │   ├── WmsAi.Inbound.Application/
│   │   ├── WmsAi.Inbound.Infrastructure/
│   │   └── WmsAi.Inbound.Host/
│   ├── AiGateway/                  # AI 网关域
│   │   ├── WmsAi.AiGateway.Domain/
│   │   ├── WmsAi.AiGateway.Application/
│   │   ├── WmsAi.AiGateway.Infrastructure/
│   │   └── WmsAi.AiGateway.Host/
│   ├── Operations/                 # 运维与后台任务
│   │   └── WmsAi.Operations.Host/
│   ├── Gateway/                    # YARP 网关
│   │   └── WmsAi.Gateway.Host/
│   ├── BuildingBlocks/             # 共享基础设施
│   │   ├── WmsAi.SharedKernel/
│   │   └── WmsAi.Contracts/
│   └── ServiceDefaults/            # Aspire 默认配置
│       └── WmsAi.ServiceDefaults/
├── web/
│   └── wms-ai-web/                 # Vue 3 前端
├── tests/
│   ├── WmsAi.Platform.Tests/
│   ├── WmsAi.Inbound.Tests/
│   ├── WmsAi.Integration.Tests/
│   └── WmsAi.ArchitectureTests/
└── docs/                           # 文档
    ├── architecture/
    ├── api/
    ├── runbooks/
    └── deployment/
```

## 核心能力

### 多智能体质检工作流（MAF）

- **证据缺口智能体**：分析当前证据是否完整。
- **检验决策智能体**：基于规则和证据给出质检建议。
- **会话管理**：支持持久化会话、检查点与恢复。
- **人机协同**：当 AI 置信度不足时，升级到人工复核。

### 事件驱动集成

- **CAP 事务消息**：通过 Outbox 模式可靠发布事件。
- **RabbitMQ**：提供跨服务消息传递能力。
- **事件契约**：统一定义在 `WmsAi.Contracts` 中。
- **最终一致性**：避免跨库分布式事务。

### 多租户执行上下文

- **租户隔离**：按租户维度隔离数据。
- **仓库上下文**：按仓库维度收敛业务操作。
- **成员关系**：按租户 / 仓库授予用户角色。
- **执行上下文注入**：通过中间件自动注入租户、仓库、用户信息。

## 相关文档

- [架构文档](docs/architecture/README.md)：系统架构、C4 图、事件流。
- [API 文档](docs/api/openapi.yaml)：OpenAPI 3.0 定义。
- [本地开发指南](docs/runbooks/local-development.md)：环境搭建与开发流程。
- [故障排查指南](docs/runbooks/troubleshooting.md)：常见问题与处理方法。
- [数据库迁移说明](docs/runbooks/database-migrations.md)：迁移管理方式。
- [监控说明](docs/runbooks/monitoring.md)：可观测性与监控指标。
- [发布检查清单](docs/deployment/release-checklist.md)：上线前检查步骤。

## 开发流程

### 运行测试

```bash
dotnet test tests/WmsAi.Platform.Tests
dotnet test tests/WmsAi.Inbound.Tests
dotnet test tests/WmsAi.Integration.Tests
dotnet test tests/WmsAi.ArchitectureTests
```

### 数据库迁移

```bash
cd src/Platform/WmsAi.Platform.Infrastructure
dotnet ef migrations add MigrationName --context UserDbContext
dotnet ef database update --context UserDbContext
```

### 代码质量

```bash
dotnet format
dotnet build /p:TreatWarningsAsErrors=true
```

## 贡献约定

1. 遵循 DDD 和整洁架构原则。
2. 为核心领域逻辑补齐单元测试。
3. 跨聚合通信优先使用领域事件。
4. 保持限界上下文边界清晰。
5. 对外接口和复杂流程必须写文档。

## License

[待补充]

## 支持

如果遇到问题，请先阅读 [故障排查指南](docs/runbooks/troubleshooting.md)；如果仍无法解决，再提交 issue。
