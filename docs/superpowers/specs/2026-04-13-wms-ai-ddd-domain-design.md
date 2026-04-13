# WMS AI DDD 领域设计

## 目标

把方案从“分层命名”推进到真正的 DDD 设计：

- Bounded Context
- Aggregate Root
- Repository
- Domain Service
- 不变量

## 一、Bounded Context 划分

第一期建议固定 3 个上下文：

## 1. Platform Context

职责：

- Tenant
- Warehouse
- User
- Role
- Membership
- 平台模板与平台配置

输出：

- 组织与身份真相

## 2. Inbound Quality Context

职责：

- Supplier
- Sku
- SkuQualityProfile
- InboundNotice
- Receipt
- QcPlan
- QcTask
- QcDecision
- Evidence

输出：

- 入库质检业务真相

## 3. AI Runtime Context

职责：

- MAF session
- workflow run
- checkpoint
- summary snapshot
- model profile
- routing policy
- AI suggestion

输出：

- AI 运行真相与可回放审计链

## 二、聚合设计

## 1. Platform Context

### Aggregate: `TenantAggregate`

Root:

- `Tenant`

Children:

- `Warehouse`
- `Membership`

不变量：

- 同一租户下仓库编码唯一
- 已停用租户不能新建仓库
- Membership 必须属于同一个租户

### Aggregate: `UserAggregate`

Root:

- `User`

Children:

- `Membership`

不变量：

- 用户名全局唯一
- 已停用用户不能被分配新 membership

## 2. Inbound Quality Context

### Aggregate: `InboundNoticeAggregate`

Root:

- `InboundNotice`

Children:

- `InboundNoticeLine`

不变量：

- `notice_no` 在 `tenant + warehouse` 范围唯一
- 已完成或已收货的 ASN 不可修改明细

### Aggregate: `ReceiptAggregate`

Root:

- `Receipt`

Children:

- `ReceiptLine`

不变量：

- 一个收货单必须关联一个 ASN
- 已完成收货不能重复提交
- 收货数量不能为负

### Aggregate: `QcTaskAggregate`

Root:

- `QcTask`

Children:

- `QcFinding`
- `EvidenceBinding`

关联但不内嵌：

- `EvidenceAsset`
- `AiSuggestion`
- `QcDecision`

不变量：

- 已终结任务不可再次进入 AI 运行
- 证据不足不能进入自动通过
- 一个任务只能有一个正式 `QcDecision`

### Aggregate: `QcDecisionAggregate`

Root:

- `QcDecision`

不变量：

- 正式结论一旦 finalized，不可被 AI 覆盖
- 人工复核优先级高于自动通过

## 3. AI Runtime Context

### Aggregate: `MafSessionAggregate`

Root:

- `MafSession`

Children:

- `MafMessage`
- `MafCheckpoint`
- `MafSummarySnapshot`

不变量：

- 一个 session 只能有一个 `last_checkpoint`
- 同一 session 的消息 sequence 必须递增

### Aggregate: `MafWorkflowRunAggregate`

Root:

- `MafWorkflowRun`

Children:

- `MafWorkflowStepRun`

不变量：

- step sequence 在 run 内唯一
- completed run 不可追加普通运行步骤
- waiting_manual_review 状态下只能由恢复动作推进

### Aggregate: `AiInspectionRunAggregate`

Root:

- `AiInspectionRun`

Children:

- `AiSuggestion`
- `MafModelCallLog`
- `MafToolCallLog`

不变量：

- 一个 inspection run 最多只产生一个最终建议版本
- 同一 run 的 model call 必须按顺序可追溯

## 三、Repository 设计

Repository 只按 aggregate root 提供，不按表提供。

示例：

- `ITenantRepository`
- `IUserRepository`
- `IInboundNoticeRepository`
- `IReceiptRepository`
- `IQcTaskRepository`
- `IQcDecisionRepository`
- `IMafSessionRepository`
- `IMafWorkflowRunRepository`
- `IAiInspectionRunRepository`

不建议：

- `IInboundNoticeLineRepository`
- `IQcFindingRepository`
- `IMafCheckpointRepository`

这些更适合作为聚合内部对象管理。

## 四、Domain Service

## 1. `QcTaskPlanningDomainService`

职责：

- 根据 SKU 质检档案和收货事实生成 `QcTask`

## 2. `EvidenceCompletenessDomainService`

职责：

- 判断当前证据是否满足规则

## 3. `AiSuggestionDecisionDomainService`

职责：

- 把 AI 建议映射成业务分支
- 决定自动通过或人工复核

## 4. `TenantProvisioningDomainService`

职责：

- 控制租户、仓库、成员关系的一致性规则

## 五、应用服务与领域服务边界

### Application Service

负责：

- 用例编排
- 调用 repository
- 发 `CAP` 事件
- 事务边界

### Domain Service

负责：

- 纯业务规则
- 不变量校验
- 聚合状态迁移

## 六、为什么这套方案才算 DDD

如果只写：

- Host
- Application
- Domain
- Infrastructure

那还只是“分层风格”。

要达到 DDD，必须同时写清：

- 上下文边界
- 聚合边界
- 不变量
- Repository 只围绕聚合根
- Domain Service 只处理领域规则

这份文档就是把这一步补上。
