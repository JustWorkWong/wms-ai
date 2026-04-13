# WMS AI 分布式事务与事件设计

## 目标

这份文档只回答三件事：

1. 跨 `UserDb / BusinessDb / AiDb` 怎么保证一致性
2. 用哪个成熟框架
3. 关键业务事件、补偿和幂等怎么落

## 框架选型

本项目不自研分布式事务基础设施，统一采用：

- `CAP`
- `RabbitMQ`
- `EF Core`

不用：

- 自研 Outbox 发布器
- 自研 Inbox 重放器
- 2PC / XA

## 原则

- 每个服务只对自己的数据库做本地事务
- 跨服务同步靠 `CAP` 发布领域事件
- 业务闭环靠 `Saga / 补偿`
- 所有消费者都必须幂等

## 事务边界

### Platform

- 本地事务：`UserDb`
- 发布：租户、仓库、用户、成员关系相关事件

### Inbound

- 本地事务：`BusinessDb`
- 发布：ASN、收货、质检任务、正式结论相关事件

### AiGateway

- 本地事务：`AiDb`
- 发布：AI 建议、checkpoint、会话状态相关事件

## 事件命名规范

事件统一使用过去式，表达“事实已经发生”：

- `tenant_created`
- `warehouse_created`
- `inbound_notice_created`
- `receipt_recorded`
- `qc_task_created`
- `evidence_bound`
- `ai_suggestion_created`
- `qc_decision_finalized`

不要用命令式名字，例如：

- `create_tenant`
- `do_ai_inspection`

## 事件载荷规范

每个事件统一携带：

- `event_id`
- `event_name`
- `occurred_at`
- `tenant_id`
- `warehouse_id`（如适用）
- `correlation_id`
- `causation_id`
- `producer`
- `payload`

## 幂等规则

每个消费者至少要有：

- `message_id`
- `consumer_name`
- `processed_at`

处理流程：

1. 先检查 `message_id + consumer_name`
2. 已处理则直接返回成功
3. 未处理才执行业务逻辑
4. 同事务记录已处理标记

## 关键跨服务链路

## 1. 租户开通链

### 主流程

1. `Platform` 在 `UserDb` 创建 `tenant`
2. 同事务通过 `CAP` 发布 `tenant_created`
3. `Inbound` 消费 `tenant_created`，初始化该租户业务空间
4. `AiGateway` 消费 `tenant_created`，初始化 AI 侧租户配置和默认模型策略

### 补偿

- 如果 `Inbound` 初始化失败：保留重试，状态记为 `pending_provision`
- 如果 `AiGateway` 初始化失败：保留重试，状态记为 `pending_ai_provision`
- `Platform` 不回滚已创建的租户主记录，而是显示“租户开通未完成”

## 2. 收货后生成质检任务链

### 主流程

1. `Inbound` 在 `BusinessDb` 写 `receipt`
2. 同事务发布 `receipt_recorded`
3. `Inbound` 自己或专门消费者生成 `qc_task_created`
4. `AiGateway` 订阅 `qc_task_created`，可预创建 AI session 模板

### 补偿

- 如果质检任务生成失败：`receipt` 状态保持 `pending_qc_plan`
- 系统支持重放 `receipt_recorded`

## 3. 证据上传后触发 AI 检验链

### 主流程

1. `Inbound` 绑定证据到 `qc_task`
2. 同事务发布 `evidence_bound`
3. `AiGateway` 消费后创建 `ai_inspection_run`
4. workflow 执行，落 `ai_suggestion_created`
5. `Inbound` 消费 `ai_suggestion_created`
6. `Inbound` 形成自动通过或待人工复核状态

### 补偿

- 如果 `AiGateway` 无法启动运行：保留 `qc_task` 为 `pending_ai`
- 如果 `Inbound` 消费建议失败：`ai_suggestion` 保持可重放

## 4. 人工复核完成链

### 主流程

1. `Inbound` 人工确认后写 `qc_decision`
2. 同事务发布 `qc_decision_finalized`
3. `AiGateway` 消费后更新 `ai_session` 摘要和状态

### 补偿

- 如果 `AiGateway` 更新会话失败：不影响 `qc_decision` 真相，只影响 AI 会话归档状态

## Saga 状态建议

建议至少维护以下 saga 状态：

- `pending`
- `processing`
- `completed`
- `failed`
- `compensating`
- `compensated`
- `manual_intervention_required`

## CAP 表与命名

`CAP` 自带持久化表建议保留统一前缀，例如：

- `cap_published`
- `cap_received`

业务自定义的幂等或 saga 表建议：

- `integration_consumer_offsets`
- `integration_saga_states`

## 错误处理

### 可自动重试

- RabbitMQ 暂时不可用
- 下游服务短时超时
- 数据库瞬时连接失败

### 需人工介入

- 事件载荷版本不兼容
- 业务对象已不存在且无法恢复
- 重试超过阈值

## 评审时必须明确的结论

- 本项目分布式事务框架固定为 `CAP`
- 本项目不采用 2PC
- 跨库一致性以“最终一致 + 可审计 + 可补偿”为准
- `QcDecision` 这种业务真相只能由 `Inbound` 服务负责写入
