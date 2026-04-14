# WMS AI - Microsoft Agent Framework 企业级实施方案

## 1. 方案概述

### 1.1 当前问题

项目虽然定义了 MAF 相关的数据结构（`MafSession`、`MafCheckpoint`、`MafWorkflowRun`），但**没有真正使用 Microsoft Agent Framework**：

- ❌ 没有安装 MAF NuGet 包
- ❌ 没有使用 `Workflow` 和 `WorkflowBuilder`
- ❌ 没有使用 `Executor` 构建完整流程
- ❌ 没有使用 `CheckpointManager` 实现状态恢复
- ❌ 只是手动串行调用两个 Agent

### 1.2 目标架构

使用 MAF 构建**完整的质检工作流**，包括：

```
数据加载 → 证据分析 → 质检决策 → 结果保存 → 人机协同
```

**核心能力**：
- ✅ 使用 `Executor` 封装数据库操作
- ✅ 使用 `Workflow State` 在 Executor 之间共享数据
- ✅ 使用 `CheckpointManager` 自动保存状态
- ✅ 使用 `RequestPort` 实现人机协同
- ✅ 支持从任意 Checkpoint 恢复

---

## 2. 技术架构

### 2.1 Workflow 流程图

```
┌─────────────────────────────────────────────────────────────────┐
│                      QC Inspection Workflow                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │ LoadQcTaskExecutor│ ← 从数据库加载质检任务
                    └──────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │LoadEvidenceExecutor│ ← 加载证据资产
                    └──────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │LoadRulesExecutor │ ← 加载质量规则
                    └──────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │EvidenceGapAgent  │ ← AI 分析证据缺口
                    └──────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │InspectionDecisionAgent│ ← AI 质检决策
                    └──────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │ConfidenceCheckExecutor│ ← 检查置信度
                    └──────────────────┘
                              │
                    ┌─────────┴─────────┐
                    │                   │
            置信度 ≥ 0.8          置信度 < 0.8
                    │                   │
                    ▼                   ▼
          ┌──────────────────┐  ┌──────────────────┐
          │SaveResultExecutor│  │  RequestPort     │
          │  (自动保存)      │  │  (人工审批)      │
          └──────────────────┘  └──────────────────┘
                    │                   │
                    │                   ▼
                    │          ┌──────────────────┐
                    │          │ 暂停 Workflow    │
                    │          │ 保存 Checkpoint  │
                    │          └──────────────────┘
                    │                   │
                    │          (人工审批后恢复)
                    │                   │
                    │                   ▼
                    │          ┌──────────────────┐
                    │          │SaveResultExecutor│
                    │          └──────────────────┘
                    │                   │
                    └───────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │PublishEventExecutor│ ← 发布 CAP 事件
                    └──────────────────┘
```

### 2.2 核心组件

| 组件 | 类型 | 职责 |
|------|------|------|
| `LoadQcTaskExecutor` | Executor | 从数据库加载质检任务详情 |
| `LoadEvidenceExecutor` | Executor | 加载证据资产（图片/视频） |
| `LoadRulesExecutor` | Executor | 加载质量规则 |
| `EvidenceGapAgentExecutor` | Executor | AI 分析证据是否完整 |
| `InspectionDecisionAgentExecutor` | Executor | AI 做出质检决策 |
| `ConfidenceCheckExecutor` | Executor | 检查置信度，决定自动/人工 |
| `SaveResultExecutor` | Executor | 保存结果到数据库 |
| `PublishEventExecutor` | Executor | 发布 CAP 事件 |
| `ApprovalRequestPort` | RequestPort | 人工审批通道 |

---

## 3. 实施步骤

### 3.1 安装 NuGet 包

```bash
cd src/AiGateway/WmsAi.AiGateway.Infrastructure
dotnet add package Microsoft.Agents.AI --prerelease
dotnet add package Microsoft.Agents.AI.Workflows --prerelease
dotnet add package Microsoft.Agents.AI.OpenAI --prerelease
```

### 3.2 定义 Workflow State

所有 Executor 共享的状态对象，包含：
- 输入参数（QcTaskId、TenantId、WarehouseId）
- 加载的数据（QcTask、Evidence、QualityRules）
- AI 分析结果（EvidenceGapAnalysis、InspectionDecision）
- 人工审批状态（RequiresHumanApproval、HumanApproval）

### 3.3 实现 Executor

每个 Executor 负责一个独立的步骤：

1. **LoadQcTaskExecutor**：调用 `IBusinessApiClient` 获取质检任务详情
2. **LoadEvidenceExecutor**：调用 `IBusinessApiClient` 获取证据资产
3. **LoadRulesExecutor**：调用 `IBusinessApiClient` 获取质量规则
4. **EvidenceGapAgentExecutor**：调用 `IChatClient` 进行 AI 分析
5. **InspectionDecisionAgentExecutor**：调用 `IChatClient` 进行 AI 决策
6. **ConfidenceCheckExecutor**：检查置信度，设置 `RequiresHumanApproval`
7. **SaveResultExecutor**：保存到 `AiInspectionRun` 表，调用 Inbound API
8. **PublishEventExecutor**：发布 CAP 事件

### 3.4 构建 Workflow

使用 `WorkflowBuilder` 连接所有 Executor：

```csharp
var workflow = new WorkflowBuilder(loadQcTaskExecutor)
    .AddEdge(loadQcTaskExecutor, loadEvidenceExecutor)
    .AddEdge(loadEvidenceExecutor, loadRulesExecutor)
    .AddEdge(loadRulesExecutor, evidenceGapAgentExecutor)
    .AddEdge(evidenceGapAgentExecutor, inspectionDecisionAgentExecutor)
    .AddEdge(inspectionDecisionAgentExecutor, confidenceCheckExecutor)
    .AddEdge(confidenceCheckExecutor, saveResultExecutor, condition: state => !state.RequiresHumanApproval)
    .AddEdge(confidenceCheckExecutor, approvalRequestPort, condition: state => state.RequiresHumanApproval)
    .AddEdge(approvalRequestPort, saveResultExecutor)
    .AddEdge(saveResultExecutor, publishEventExecutor)
    .WithOutputFrom(publishEventExecutor)
    .Build();
```

### 3.5 实现 Checkpoint 持久化

创建 `PostgresCheckpointStorage` 实现 `ICheckpointStorage` 接口，将 Checkpoint 保存到 PostgreSQL。

### 3.6 实现人机协同

- 低置信度时，Workflow 发送 `RequestInfoEvent`
- 外部系统监听事件，暂停 Workflow
- 人工审批后，调用 Resume API 恢复 Workflow

---

## 4. 数据持久化方案

### 4.1 Checkpoint 存储

**方案 1：扩展现有 MafCheckpoint 表**

```sql
ALTER TABLE "MafCheckpoints" 
ADD COLUMN "CheckpointJson" TEXT;
```

**方案 2：新建专用表**

```sql
CREATE TABLE "WorkflowCheckpoints" (
    "Id" UUID PRIMARY KEY,
    "WorkflowId" TEXT NOT NULL,
    "CheckpointData" JSONB NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL
);
```

### 4.2 Workflow 执行记录

复用现有的 `MafWorkflowRun` 和 `MafWorkflowStepRun` 表，每个 Executor 执行完成后记录一条 StepRun。

### 4.3 AI 分析结果

保存到 `AiInspectionRun` 表，包含：
- EvidenceGapAnalysis JSON
- InspectionDecision JSON
- ConfidenceScore
- Status（Running/Paused/Completed/Failed）

---

## 5. 人机协同流程

### 5.1 自动化路径（置信度 ≥ 0.8）

```
AI 决策 → 置信度检查 → 自动保存结果 → 发布事件 → 完成
```

### 5.2 人工审批路径（置信度 < 0.8）

```
AI 决策 → 置信度检查 → 发送 RequestInfoEvent → 暂停 Workflow → 保存 Checkpoint
                                                          ↓
                                                    前端展示审批界面
                                                          ↓
                                                    人工审批（同意/拒绝）
                                                          ↓
                                                    调用 Resume API
                                                          ↓
                                                    从 Checkpoint 恢复
                                                          ↓
                                                    保存结果 → 发布事件 → 完成
```

### 5.3 Resume API

```csharp
POST /api/ai/workflows/{workflowId}/resume
{
  "approved": true,
  "feedback": "同意 AI 建议",
  "approvedBy": "user123"
}
```

---

## 6. 企业级最佳实践

### 6.1 依赖注入

所有 Executor 通过构造函数注入依赖：
- `IServiceProvider`：用于创建 Scope 解析 Scoped 服务
- `IChatClient`：AI 模型客户端
- `ILogger`：结构化日志

### 6.2 错误处理

- Executor 内部捕获异常，记录日志
- 关键步骤失败时，Workflow 自动保存 Checkpoint
- 支持从失败点重试

### 6.3 可观测性

- 每个 Executor 记录开始/结束日志
- 记录 AI 调用的 Token 消耗、延迟
- 通过 OpenTelemetry 追踪完整链路

### 6.4 安全性

- Checkpoint 数据加密存储
- 敏感信息（API Key）不进入 Checkpoint
- 人工审批需要权限验证

### 6.5 性能优化

- 数据加载步骤可以并行执行（使用 `AddEdge` 的并行模式）
- Checkpoint 异步保存，不阻塞 Workflow
- AI 调用使用连接池

---

## 7. 迁移路径

### 7.1 Phase 1：基础设施（1-2 天）

- [ ] 安装 MAF NuGet 包
- [ ] 定义 `QcInspectionState`
- [ ] 实现 `PostgresCheckpointStorage`
- [ ] 注册 `CheckpointManager`

### 7.2 Phase 2：数据加载 Executor（2-3 天）

- [ ] 实现 `LoadQcTaskExecutor`
- [ ] 实现 `LoadEvidenceExecutor`
- [ ] 实现 `LoadRulesExecutor`
- [ ] 单元测试

### 7.3 Phase 3：AI Executor（2-3 天）

- [ ] 实现 `EvidenceGapAgentExecutor`
- [ ] 实现 `InspectionDecisionAgentExecutor`
- [ ] 实现 `ConfidenceCheckExecutor`
- [ ] 单元测试

### 7.4 Phase 4：结果保存 Executor（1-2 天）

- [ ] 实现 `SaveResultExecutor`
- [ ] 实现 `PublishEventExecutor`
- [ ] 单元测试

### 7.5 Phase 5：Workflow 编排（2-3 天）

- [ ] 实现 `QcInspectionWorkflowFactory`
- [ ] 实现 `RequestPort` 人机协同
- [ ] 实现 Resume API
- [ ] 集成测试

### 7.6 Phase 6：迁移现有代码（1-2 天）

- [ ] 重构 `InboundEventConsumer`
- [ ] 删除旧的手动编排代码
- [ ] 回归测试

---

## 8. 参考资料

- [Microsoft Agent Framework 概述](https://learn.microsoft.com/zh-cn/agent-framework/overview/?pivots=programming-language-csharp)
- [Workflows - Executors](https://learn.microsoft.com/en-us/agent-framework/workflows/executors)
- [Workflows - State](https://learn.microsoft.com/en-us/agent-framework/user-guide/workflows/state)
- [Workflows - Checkpoints](https://learn.microsoft.com/en-us/agent-framework/workflows/checkpoints)
- [Human-in-the-Loop Workflows](https://learn.microsoft.com/en-us/agent-framework/workflows/human-in-the-loop)
- [GitHub 示例代码](https://github.com/microsoft/agent-framework)

---

## 9. 常见问题

### Q1: 为什么不直接用 Semantic Kernel？

MAF 是 Semantic Kernel 和 AutoGen 的下一代产品，专为企业级多智能体场景设计，提供：
- 原生的 Workflow 编排能力
- 内置的 Checkpoint 和状态管理
- 更好的类型安全和编译时验证

### Q2: Executor 如何访问数据库？

通过构造函数注入 `IServiceProvider`，在 `HandleAsync` 方法内创建 Scope 解析 Scoped 服务（如 DbContext、Repository）。

### Q3: Checkpoint 会保存什么数据？

- Workflow State（包含所有业务数据）
- 当前执行位置（哪个 Executor）
- 待处理的消息队列
- 待响应的 Request

### Q4: 如何测试 Workflow？

- 单元测试：测试每个 Executor 的逻辑
- 集成测试：使用 `InMemoryCheckpointStorage` 测试完整 Workflow
- E2E 测试：使用真实数据库和 AI 模型

### Q5: 性能如何？

- 数据加载步骤可以并行执行
- AI 调用是主要瓶颈（1-3 秒）
- Checkpoint 保存是异步的，不阻塞执行
- 整体流程预计 3-5 秒完成

---

## 10. 总结

本方案将项目从**手动编排**升级到**企业级 MAF 工作流**，核心改进：

1. **完整流程**：从数据加载到结果保存，全部纳入 Workflow
2. **状态管理**：使用 Workflow State 在 Executor 之间共享数据
3. **容错恢复**：Checkpoint 自动保存，支持从任意点恢复
4. **人机协同**：低置信度自动暂停，人工审批后恢复
5. **可观测性**：每个步骤都有日志和追踪

这是一个**生产级**的实施方案，符合微软官方的最佳实践。
