# WMS AI 架构设计

## 总体原则

- 业务服务负责业务真相
- AI 服务负责 AI 会话和建议
- 主流程由 `Workflow` 驱动，不由 Agent 自由发挥
- 普通业务接口走网关
- AG-UI 和 AI 会话走 `AiGateway`

## 服务拆分

## 1. Gateway

职责：

- 统一入口
- 鉴权、限流、CORS、TLS
- 路由到 `Platform`、`Inbound`、`AiGateway`

不负责：

- 业务编排
- AI session 状态

## 2. Platform

职责：

- Tenant
- Warehouse
- User / Role / Membership
- 平台模板
- 平台级配置

数据：

- `UserDb`

## 3. Inbound

职责：

- Sku / Supplier / QualityProfile
- ASN
- Receipt
- QcTask
- Evidence 元数据
- QcDecision

数据：

- `BusinessDb`

## 4. AiGateway

职责：

- AG-UI
- AiSession
- AiCheckpoint
- AiSummarySnapshot
- AiInspectionRun
- AiSuggestion
- 模型路由
- Skill / MCP / Function Calling 装配
- 多 workflow / 多 agent profile 路由
- 把内部事件流转换成 AG-UI 标准事件
- 恢复 session / checkpoint 并对前端续流
- 屏蔽模型、工具、workflow 细节，不让前端直连 `MAF`

数据：

- `AiDb`

对外边界：

- 前端只通过 `AiGateway` 使用 AG-UI
- `Inbound` 和 `Platform` 不暴露 AG-UI 协议
- 业务服务通过 HTTP / CAP 事件与 `AiGateway` 交互

内部定位：

- `AiGateway` 不是单一 agent 服务
- 它是 AI 编排入口
- 后面可以挂多个 `MAF workflow`
- 每个 workflow 下可以装配多个 agent profile

## 服务内部结构

每个服务按下面四层组织：

- `Host`
- `Application`
- `Domain`
- `Infrastructure`

不再使用“一个服务 = 一个 Api 项目”的表达方式。

## MAF Workflow 主链

主链不是聊天，而是 durable workflow。

### Workflow 节点建议

1. `PrepareInspectionContext`
2. `LoadQualitySkill`
3. `LoadRulesAndEvidence`
4. `CheckEvidenceCompleteness`
5. `RunInspectionAgent`
6. `NormalizeSuggestion`
7. `EvaluateConfidenceGate`
8. `PersistSuggestion`
9. `AutoPassOrEscalate`
10. `WaitManualReview`
11. `FinalizeDecision`

### 第一期开工建议至少 2 个 Agent

#### 1. `EvidenceGapAgent`

职责：

- 判断证据是否缺失
- 生成补证据项
- 形成可解释的缺失原因

#### 2. `InspectionDecisionAgent`

职责：

- 基于规则、图片、操作记录形成结构化建议
- 输出风险标签、结论建议、置信度、说明摘要

可选但不作为第一期阻塞项：

- `ReviewAssistAgent`
  - 在人工复核阶段生成摘要、冲突点和建议问题

这意味着第一期的 MAF 示例不再是“单 agent demo”，而是“一个 workflow + 两个 agent profile”的生产起点。

## AG-UI 的使用位置

`AG-UI` 不是全系统通信协议，只用于：

- `Vue` 前端 和 `AiGateway` 之间
- 流式事件、状态同步、人工确认、工具事件展示

不用在：

- `Gateway` 到业务服务之间
- `Platform` 和 `Inbound` 之间
- 业务服务内部应用层之间

这样可以把 AG-UI 限定为“AI 交互协议层”，而不是污染整个业务系统。

## 为什么 `AiGateway` 不是多余的

如果去掉 `AiGateway`，前端就要直接承担：

- `MAF session` 生命周期
- checkpoint 恢复
- AG-UI 事件兼容
- 模型路由和 profile 解析
- tool / MCP 事件过滤
- AI 鉴权、限流和审计

这会把 AI 协议细节泄漏到前端和业务服务。

所以 `AiGateway` 的价值不是“又多一层转发”，而是：

- `AG-UI` 协议适配层
- `MAF` 运行时会话层
- 模型配置解析层
- AI 运行审计边界

### 节点职责

#### `PrepareInspectionContext`

- 读取 `QcTask`
- 读取 `Evidence`
- 构建本轮上下文

#### `LoadQualitySkill`

- 装载该类检验任务的 SOP

#### `LoadRulesAndEvidence`

- 读取 SKU 质检规则
- 读取证据元数据和必要引用

#### `CheckEvidenceCompleteness`

- 先判断证据是否足够
- 不够就生成缺失项

#### `RunInspectionAgent`

- 由 `EvidenceGapAgent` 或 `InspectionDecisionAgent` 执行单步动态判断

#### `NormalizeSuggestion`

- 将模型输出规范成统一结构

#### `EvaluateConfidenceGate`

- 走确定性规则，判断自动通过还是人工复核

#### `PersistSuggestion`

- 落 `AiDb`

#### `AutoPassOrEscalate`

- 发业务事件到 `Inbound`

#### `WaitManualReview`

- 等待人工

#### `FinalizeDecision`

- 在 `BusinessDb` 形成正式结论

## Skill / MCP / Function Calling 分层

## 1. Skill

作用：

- 固化检验方法
- 约束模型行为
- 统一输出口径

示例：

- `inbound-qc-visual-inspection`
- `inbound-qc-label-validation`
- `inbound-qc-evidence-gap-check`
- `inbound-qc-supervisor-review`

## 2. MCP

作用：

- 接外部能力

示例：

- 图像预处理
- 规则仓库查询
- 历史异常查询
- 知识库检索

## 3. Function Calling

作用：

- 接服务内确定性能力

示例：

- 读取 `QcTask`
- 读取证据元数据
- 计算风险标签
- 计算置信度闸门
- 结构化建议校验
- 提交人工复核结果
- 写入业务命令请求

## MAF 执行器访问业务数据的边界

`MAF` 执行器可以操控业务能力，但不能直接裸写业务数据库。

必须遵守：

- 只通过受控的 `Function Calling` / 应用服务命令访问业务能力
- 每次调用都显式携带：
  - `tenant_id`
  - `warehouse_id`
  - `user_id`
  - `membership_id`
  - `correlation_id`
- 所有业务写动作都回到 `Platform` 或 `Inbound` 的应用服务边界
- `AiGateway` 不直接持有 `BusinessDb` 写权限

这样才能保证：

- 多租户隔离
- 仓库作用域隔离
- 用户责任可追溯
- AI 不能绕过业务规则直接改真相

## 检验流程图

```mermaid
flowchart TD
    A["Workflow: PrepareInspectionContext"] --> B["Workflow: LoadQualitySkill"]
    B --> C["Workflow: LoadRulesAndEvidence"]
    C --> D["Workflow: CheckEvidenceCompleteness"]
    D --> E{"证据充分?"}
    E -- "否" --> F["Function: 生成缺失证据项"]
    F --> G["Persist checkpoint + 返回补证据任务"]
    E -- "是" --> H["MAF Agent 执行检验"]
    H --> I["Skill: 检验方法约束"]
    H --> J["MCP: 读取外部能力"]
    H --> K["Function: 读取本地确定性能力"]
    I --> L["输出结构化建议"]
    J --> L
    K --> L
    L --> M["Workflow: EvaluateConfidenceGate"]
    M --> N{"自动通过?"}
    N -- "是" --> O["Workflow: FinalizeDecision"]
    N -- "否" --> P["Workflow: WaitManualReview"]
    P --> O
```

## AG-UI 最佳实践落位

当前更合理的做法不是让业务服务原生暴露 AG-UI，而是采用“middleware / adapter”模式：

- `MAF Workflow` 和业务服务保持自己的内部协议
- `AiGateway` 把内部运行态翻译成 AG-UI 事件流
- 前端消费 AG-UI 标准事件

这更适合已有系统接入，也更符合“协议适配层单独收口”的生产实践。

## 分布式事务落位

## 1. 原则

不用 2PC。  
采用：

- 本地事务
- Outbox
- Inbox
- Saga / 补偿

## 2. 关键跨库链路

### 平台开通

- `Platform` 写 `UserDb`
- 发租户开通事件
- `Inbound` 初始化业务空间
- `AiGateway` 初始化 AI 空间

### AI 建议转业务结论

- `AiGateway` 写 `AiDb`
- 发 `AiSuggestionCreated`
- `Inbound` 消费后形成 `QcDecision`

### 人工复核回写 AI 会话

- `Inbound` 写 `BusinessDb`
- 发 `QcDecisionFinalized`
- `AiGateway` 更新 session/summary

## Aspire 编排

`AppHost` 至少管理：

- Gateway
- Platform Host
- Inbound Host
- AiGateway Host
- PostgreSQL x 3 或逻辑三库
- Redis
- RabbitMQ
- MinIO
- OpenTelemetry Collector

并要求：

- 持久化卷
- Dashboard 可见
- 资源健康检查
- 服务日志、追踪、指标统一接入

## 观测要求

必须可看到：

- HTTP 请求
- EF Core SQL
- RabbitMQ 发布与消费
- 对象存储调用
- MAF session 生命周期
- workflow 节点生命周期
- tool/function/MCP 调用摘要
- checkpoint 创建和恢复
- 模型路由、耗时、token
