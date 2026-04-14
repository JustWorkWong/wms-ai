# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

WMS AI 是基于 DDD、事件驱动架构和 CQRS 模式构建的智能仓储管理系统。通过多智能体工作流（MAF）自动完成质检判断，并协助优化仓内作业流程。

技术栈：.NET 10 + PostgreSQL 16 + Vue 3 + Aspire + CAP + RabbitMQ

## 核心架构原则

### 限界上下文（Bounded Contexts）

系统按 DDD 拆分为 5 个限界上下文，每个上下文独立数据库：

1. **Platform**（UserDb）：租户、仓库、用户、成员关系管理
2. **Inbound**（BusinessDb）：到货通知、收货、质检任务、质检结论
3. **AiGateway**（AiDb）：AI 工作流、MAF 会话、检验运行、模型配置
4. **Operations**（HangfireDb）：后台任务与定时任务（Hangfire）
5. **Gateway**：YARP 网关，聚合路由与统一入口

### 分层架构

每个限界上下文遵循整洁架构分层：

```
Domain → Application → Infrastructure → Host
```

- **Domain**：聚合根、实体、值对象、领域事件、仓储接口
- **Application**：用例处理器（Command/Query Handler）、DTO
- **Infrastructure**：EF Core 持久化、CAP 事件发布、外部服务集成
- **Host**：Web API 入口、中间件、路由映射

### 多租户隔离

- **TenantScopedAggregateRoot**：租户级聚合根（如 Tenant、Warehouse）
- **WarehouseScopedAggregateRoot**：仓库级聚合根（如 InboundNotice、QcTask）
- **RequestExecutionContext**：请求执行上下文，包含 TenantId、WarehouseId、UserId、MembershipId、CorrelationId
- 所有业务操作必须在租户/仓库上下文内执行

### 事件驱动集成

- **CAP + RabbitMQ**：跨服务事件发布与订阅，保证最终一致性
- **事件契约**：统一定义在 `WmsAi.Contracts/Events/` 中，版本化命名（如 `QcTaskCreatedV1`）
- **Outbox 模式**：CAP 自动保证事件发布与数据库事务的原子性
- **跨聚合通信**：优先使用领域事件，避免直接调用

## 常用命令

### 启动后端（Aspire）

```bash
cd src/AppHost/WmsAi.AppHost
dotnet run
```

启动后控制台会输出 Aspire Dashboard 的实际地址（端口可能不同）。

### 启动前端

```bash
cd web/wms-ai-web
npm install
npm run dev
```

前端访问地址：`http://localhost:5173`

### 运行测试

```bash
# 单元测试
dotnet test tests/WmsAi.Platform.Tests
dotnet test tests/WmsAi.Inbound.Tests

# 集成测试
dotnet test tests/WmsAi.Integration.Tests

# 架构测试
dotnet test tests/WmsAi.ArchitectureTests
```

### 数据库迁移

```bash
# Platform (UserDb)
cd src/Platform/WmsAi.Platform.Infrastructure
dotnet ef migrations add MigrationName --context UserDbContext
dotnet ef database update --context UserDbContext

# Inbound (BusinessDb)
cd src/Inbound/WmsAi.Inbound.Infrastructure
dotnet ef migrations add MigrationName --context BusinessDbContext
dotnet ef database update --context BusinessDbContext

# AiGateway (AiDb)
cd src/AiGateway/WmsAi.AiGateway.Infrastructure
dotnet ef migrations add MigrationName --context AiDbContext
dotnet ef database update --context AiDbContext
```

### 代码质量

```bash
dotnet format
dotnet build /p:TreatWarningsAsErrors=true
```

## 开发约定

### 新增功能流程

1. **确定限界上下文**：功能属于哪个上下文？如果跨上下文，通过事件集成
2. **Domain 层建模**：定义聚合根、实体、值对象、领域事件
3. **Application 层**：编写 Command/Query Handler
4. **Infrastructure 层**：实现仓储、配置 EF Core 映射
5. **Host 层**：映射 HTTP 路由
6. **测试**：补齐单元测试（Domain + Application）

### 跨上下文通信

- **禁止直接调用其他上下文的仓储或数据库**
- **使用领域事件**：在 `WmsAi.Contracts/Events/` 定义事件契约
- **CAP 发布事件**：在 Infrastructure 层通过 `ICapPublisher.PublishAsync()` 发布
- **CAP 订阅事件**：在 Host 层通过 `[CapSubscribe]` 订阅

### 命名约定

- **聚合根**：`InboundNotice`、`QcTask`、`MafWorkflowRun`
- **仓储接口**：`IInboundNoticeRepository`
- **Command**：`CreateInboundNoticeCommand`
- **Handler**：`CreateInboundNoticeHandler`
- **事件**：`QcTaskCreatedV1`（版本化）
- **DbContext**：`UserDbContext`、`BusinessDbContext`、`AiDbContext`

### 数据库迁移策略

- **开发环境**：使用 `EnsureCreatedAsync()` 快速原型验证，稳定后创建迁移
- **生产环境**：必须使用迁移，禁止 `EnsureCreated()`
- **迁移命名**：`AddColumnName`、`RemoveColumnName`、`AddIndexOnColumn`
- **迁移测试**：先在 staging 环境验证，再上生产

## 关键技术细节

### Aspire 服务编排

- **服务依赖**：通过 `.WaitFor()` 声明启动顺序
- **服务引用**：通过 `.WithReference()` 注入连接字符串
- **端口映射**：Platform(5001)、Inbound(5002)、AiGateway(5003)、Operations(5004)、Gateway(5000)

### CAP 事件总线配置

- **RabbitMQ 连接**：用户名 `wmsai`，密码 `wmsai`（开发环境）
- **事件表**：CAP 自动创建 `cap.published` 和 `cap.received` 表
- **重试策略**：CAP 默认重试 3 次，间隔 60 秒

### 多智能体质检工作流（MAF）

- **证据缺口智能体**：分析当前证据是否完整
- **检验决策智能体**：基于规则和证据给出质检建议
- **会话管理**：支持持久化会话、检查点与恢复
- **人机协同**：当 AI 置信度不足时，升级到人工复核

### 前端技术栈

- **Vue 3 Composition API + TypeScript**
- **Element Plus**：组件库
- **Pinia**：状态管理
- **Axios**：HTTP 客户端
- **Vite**：开发与构建工具

## 常见问题

### Aspire Dashboard 端口不固定

启动后端时，控制台会输出实际端口，不要假定固定是 15888。

### RabbitMQ 连接失败

确认用户名密码是 `wmsai/wmsai`，不要使用 `guest`（RabbitMQ 4.x 默认禁止远程 guest 连接）。

### 数据库迁移冲突

如果多人同时创建迁移，按时间戳合并，必要时手动调整迁移顺序。

### CAP 事件未消费

检查 RabbitMQ 管理后台（`http://localhost:15672`），确认队列是否正常创建，消息是否堆积。

## 相关文档

- [架构文档](docs/architecture/README.md)
- [本地开发指南](docs/runbooks/local-development.md)
- [故障排查指南](docs/runbooks/troubleshooting.md)
- [数据库迁移说明](docs/runbooks/database-migrations.md)
- [监控说明](docs/runbooks/monitoring.md)
- [发布检查清单](docs/deployment/release-checklist.md)
