# WMS AI 入库质检系统设计

## 目标

设计一个新的、生产级的 `WMS AI` 项目，聚焦 `入库质检` 主线，满足以下约束：

- 后端技术栈：`.NET + MAF + EF Core`
- 前端技术栈：`Vue`
- AI 交互协议：`AG-UI`
- 平台形态：`SaaS 混合型`
- 数据范围：`多租户 + 多仓库`
- AI 能力：`规则 + 图像 + 操作记录` 的混合输入
- 自动化策略：`半自动质检`，低风险自动通过，高风险或低置信度转人工
- 部署目标：`云原生生产环境`

这次项目是一个全新项目，不延续旧仓库结构。旧项目只作为经验参考，不作为本次设计边界。

## 边界

本设计覆盖：

- 多租户、多仓库、统一账号权限底座
- ASN、到货、收货、入库质检、人工复核、正式结论
- 单一 `Vue` 前端项目下的平台管理、租户业务、质检作业、管理分析助手
- `Gateway + Platform Core + Inbound QC Core + AI Gateway`
- `AI Gateway` 的 session、checkpoint、上下文压缩、模型配置、AG-UI 转接
- `PostgreSQL + Redis + MQ + Object Storage + Config Center + Observability`
- 生产级审计、恢复、幂等、可回放能力

本设计不覆盖：

- 退货质检
- 出库、波次、库内作业、盘点
- 复杂计费结算
- 全量数据中台
- 将所有域一开始拆成细粒度微服务

## 产品定位

系统定位为一个 `AI 增强型 WMS 入库质检平台`，其核心不是“把 AI 嵌进聊天框”，而是把 AI 作为入库质检流程中的建议、解释、追问和辅助判定层。

系统同时面向两类视角：

- 平台侧：管理租户、仓库、平台级配置、模型策略、平台审计
- 租户侧：管理 ASN、收货、质检规则、质检作业、异常分析

AI 不持有业务真相。正式质检结论必须由业务域落库，且自动通过与人工确认都必须进入统一审计链。

## 总体架构

### 1. 入口层

外部访问入口由 `Gateway` 提供，负责：

- 统一鉴权
- TLS / CORS / 限流
- 服务路由
- 请求日志与基础安全治理

前端是一个单独的 `Vue` 项目，在同一工程下承载四类界面：

- 平台管理台
- 租户业务后台
- 质检作业台
- 管理分析助手

普通业务接口通过 `Gateway` 直达业务服务。AI 相关接口通过 `Gateway` 转发到 `AI Gateway`。

### 2. 核心服务层

系统第一期采用“双核心业务服务 + 一个 AI 转接服务”的形态：

#### `Platform Core`

负责平台底座能力：

- Tenant
- Warehouse
- User / Role / Membership
- 平台配置
- 租户配置
- 平台级规则模板
- 平台审计查询

#### `Inbound QC Core`

负责入库质检业务真相：

- ASN / InboundNotice
- Receipt / ReceiptLine
- QcPlan / QcTask
- QcFinding / QcDecision
- 人工复核
- 异常处置
- 自动通过判定落库

#### `AI Gateway`

`AI Gateway` 不是通用 BFF，而是一个职责收敛的 `AI 协议转接与会话编排服务`，只负责：

- `AG-UI` 协议入口与事件转发
- AI session 创建、恢复、关闭
- checkpoint 记录与恢复
- 上下文压缩与摘要快照
- 模型配置解析与场景路由
- 多模态推理结果的结构化输出
- 将 AI 建议推回业务服务

`AI Gateway` 明确不负责：

- ASN、收货、质检等业务真相
- 通用 CRUD 聚合
- 平台管理接口
- 普通页面 DTO 编排

### 3. 平台基础设施层

第一期平台基础设施按生产级要求设计：

- `PostgreSQL`：主业务数据、AI 元数据、审计链、配置快照
- `Redis`：session 热缓存、幂等键、短期上下文、分布式锁
- `Object Storage`：图片、视频、证据文件、导出报告、摘要附件
- `MQ / Event Bus`：AI 异步任务、重试补偿、状态事件
- `Config Center`：环境配置、模型配置、租户覆盖、特性开关
- `Observability`：日志、指标、链路追踪、告警

## 核心业务对象

### 1. 平台对象

- `Tenant`
- `Warehouse`
- `User`
- `Role`
- `Membership`

### 2. 主数据对象

- `Owner`
- `Supplier`
- `Sku`
- `SkuQualityProfile`

### 3. 入库对象

- `InboundNotice`
- `InboundNoticeLine`
- `Receipt`
- `ReceiptLine`

### 4. 质检对象

- `QcPlan`
- `QcTask`
- `QcTaskSample`
- `QcFinding`
- `QcDecision`

### 5. 证据对象

- `EvidenceAsset`
- `EvidenceBinding`

### 6. AI 对象

- `AiSession`
- `AiCheckpoint`
- `AiSummarySnapshot`
- `AiInspectionRun`
- `AiInspectionStep`
- `AiSuggestion`
- `AiModelProfile`
- `AiRoutingPolicy`

## 多租户与多仓库隔离

隔离采用“双层作用域”：

- 所有租户数据强制带 `TenantId`
- 仓级业务数据再强制带 `WarehouseId`

隔离要求：

- EF Core 使用全局查询过滤作为默认安全网
- 应用服务层必须显式校验当前用户的租户和仓库作用域
- 关键唯一索引必须将 `TenantId`、必要时 `WarehouseId` 纳入组合键
- 平台管理员的跨租户访问必须落单独审计日志

不允许只靠“代码约定”做隔离，必须在实体、索引、授权和查询入口四层同时体现。

## 权限模型

第一期使用平台统一账号体系，角色分四层：

- `PlatformAdmin`
- `TenantAdmin`
- `WarehouseSupervisor`
- `Inspector`

职责边界：

- `PlatformAdmin`：租户开通、平台配置、平台审计、平台模型策略
- `TenantAdmin`：本租户用户、仓库、租户配置、租户规则
- `WarehouseSupervisor`：本仓监管、人工复核、异常处理、指标查看
- `Inspector`：收货、质检作业、证据上传、AI 会话交互、提交复核

## 入库质检主流程

主流程固定为：

1. 租户在指定仓库下创建 `ASN`
2. 到货后生成 `Receipt` 和 `ReceiptLine`
3. 系统依据 `SkuQualityProfile`、供应商策略、仓库规则生成 `QcPlan` 和 `QcTask`
4. 质检员在作业台录入检查项、上传图片/视频、补充异常描述
5. `AI Gateway` 汇总规则、图像、操作记录，触发多模态判定
6. AI 输出结构化建议，包括风险点、建议结论、缺失证据、建议动作和置信度
7. `Inbound QC Core` 根据策略判断：
   - 低风险且高置信度：自动通过
   - 高风险、证据不足、低置信度：进入人工复核
8. 最终由 `QcDecision` 形成业务真相，并回写入库状态

## AI Gateway 设计

### 1. Session 模型

Session 必须按以下维度分区：

- `TenantId`
- `UserId`
- `SessionType`
- `BusinessObjectType`
- `BusinessObjectId`

第一期至少支持两类 session：

- `WorkbenchInspectionSession`
- `ManagementAssistantSession`

这样可以确保作业台会话和管理分析会话隔离，避免上下文污染。

### 2. Checkpoint 设计

Checkpoint 是 AI 会话可恢复的核心能力。第一期必须在以下节点强制落点：

- 模型调用前
- 模型响应后
- 工具调用前
- 工具调用后
- 人工中断前
- 任务恢复前
- 业务建议提交前

每个 checkpoint 至少记录：

- Session 标识
- 当前场景
- 上下文摘要引用
- 输入消息范围
- 关联证据引用
- 模型配置快照
- 当前状态机位置
- 恢复游标

### 3. 上下文压缩

长会话不能依赖无限拼接消息，第一期采用“三层上下文”：

- `短期窗口`：最近 N 条对话和最新动作
- `摘要快照`：阶段性压缩结果
- `证据引用`：对象存储文件、业务对象、规则命中结果的引用

压缩后的摘要必须可落库，并与原会话、checkpoint 关联，保证：

- 会话恢复时可重建
- 审计时可回放
- 长会话成本可控

### 4. 模型配置

模型配置必须支持：

- 平台默认配置
- 租户覆盖配置
- 场景级路由
- 灰度开关
- 版本审计
- 回滚

典型场景包括：

- 图像判定模型
- 文本解释模型
- 摘要压缩模型
- 管理分析问答模型

第一期禁止把模型参数硬编码在业务代码中。模型选择、温度、超时、重试、token 上限、降级策略必须来自配置中心和配置快照。

### 5. AI 输出契约

AI 输出必须结构化，第一期建议固定为：

- `SuggestedDecision`
- `Confidence`
- `RiskTags`
- `MissingEvidence`
- `RecommendedActions`
- `ReasoningSummary`
- `EvidenceRefs`
- `ModelProfileVersion`

`Inbound QC Core` 只接受建议对象，不接受“模型直接写业务状态”。

## Gateway 与 AI Gateway 的关系

系统保留统一 `Gateway` 作为对外入口：

- 普通业务流量：`Gateway -> Platform Core / Inbound QC Core`
- AI 会话流量：`Gateway -> AI Gateway`

这样做的目的是：

- 继续保持你熟悉的“网关直连业务服务”主路径
- 只把 `AG-UI`、session、checkpoint、模型路由这类高复杂度能力收敛进 `AI Gateway`
- 避免引入一个全站型 BFF 造成不必要耦合

## 单前端项目设计

前端只保留一个 `Vue` 项目，不拆多个工程。通过路由、权限和布局组织出四类入口：

- 平台管理
- 租户业务
- 质检作业
- 管理分析助手

前端与后端的交互原则：

- 普通业务 CRUD：前端经 `Gateway` 直接调用业务服务
- AI 会话和流式事件：前端经 `Gateway` 接入 `AI Gateway`

这样既保留简单链路，也把 AI 复杂度从前端拆出去。

## 可靠性设计

### 1. 多实例

`AI Gateway` 必须无状态化，支持多实例水平扩容：

- 热状态放 `Redis`
- checkpoint 和元数据放 `PostgreSQL`
- 文件和证据放对象存储

不允许依赖本机内存持久保留会话关键状态。

### 2. 断线恢复

AG-UI / SSE 交互按“连接态 + 可恢复态”设计：

- 连接不断时，当前实例直接桥接流式事件
- 连接中断后，前端使用 `sessionId + checkpointId` 恢复
- `AI Gateway` 根据 checkpoint 和摘要快照重建上下文

第一期可以不引入复杂事件总线重放模型，但必须保证断线后会话可恢复。

### 3. 异步化

模型慢调用不能占住主业务线程。AI 分析任务必须异步化，通过 `MQ / Event Bus` 触发和补偿：

- 提交分析任务
- 处理重试
- 记录状态变更
- 投递审计事件

### 4. 幂等

以下操作必须具备幂等保障：

- 上传证据后的分析触发
- 自动通过提交
- 人工确认提交
- 会话恢复请求

## 审计设计

AI 判定必须可回放到四类证据：

- 输入上下文快照
- 使用的模型与参数版本
- 规则命中结果
- 建议输出与最终正式结论

自动通过必须记录：

- 操作主体
- 租户与仓库
- 任务标识
- 命中的策略和阈值
- 模型配置版本
- 置信度
- 关联 checkpoint

平台管理员跨租户查看会话、证据或结论时必须进入高等级审计。

## 配置中心设计

配置中心至少承载以下配置：

- 环境连接配置
- 模型配置
- 场景路由策略
- 特性开关
- 租户覆盖配置
- 自动通过阈值
- 文件上传限制

配置读取要求：

- 服务启动时加载基础配置
- 关键 AI 配置支持热更新或版本切换
- 每次 AI 运行都要固化一份配置快照，避免后续审计失真

## 观测与运维

第一期观测体系至少覆盖：

- 应用日志
- 结构化审计日志
- 指标
- 分布式链路追踪
- 告警

关键观测指标：

- 每租户、每仓库的质检吞吐
- 自动通过率
- 人工复核率
- AI 平均响应时长
- AI 超时与重试率
- session 恢复成功率
- 模型降级触发次数

## 测试策略

### 1. Platform Core

重点验证：

- 多租户隔离
- 多仓库作用域
- 平台管理员跨租户审计
- 统一账号角色授权

### 2. Inbound QC Core

重点验证：

- ASN 到收货再到质检的业务流
- 自动通过与人工复核分支
- 正式结论落库
- 幂等提交

### 3. AI Gateway

重点验证：

- session 生命周期
- checkpoint 恢复
- 上下文压缩正确性
- 模型配置路由
- AI 输出契约稳定性

### 4. 前端

重点验证：

- 质检作业台黄金路径
- AG-UI 事件流渲染
- 不同角色下的页面可见性与动作权限

### 5. 端到端

至少覆盖一条黄金链路：

`创建 ASN -> 收货 -> 上传证据 -> AI 判定 -> 自动通过或转人工 -> 正式结论落库`

## 部署形态

生产部署按云原生方式设计，但第一期仍保持服务数量克制：

- Gateway
- Platform Core
- Inbound QC Core
- AI Gateway
- PostgreSQL
- Redis
- MQ / Event Bus
- Object Storage
- Config Center
- Observability Stack

这套结构满足“云原生生产级”目标，同时避免在第一期把业务域过度切碎。

## 风险与取舍

- 如果一开始就把所有域拆成细粒度微服务，首期交付会被分布式复杂度拖慢。
- 如果不把 AI 会话层独立出来，session、checkpoint、模型路由会污染业务服务。
- 如果只做租户隔离而不做仓库作用域，仓内权限会很快失控。
- 如果 AI 输出不结构化，自动通过和审计都无法生产化。
- 如果摘要压缩不落库，会话恢复与回放会失真。

因此第一期采用的核心取舍是：

- 业务服务保持克制拆分
- AI 复杂度集中到 `AI Gateway`
- 普通 CRUD 保持简洁链路
- 对 AI 相关状态、配置、审计进行生产级设计

## 第一期开工边界

第一期必须交付：

- 多租户、多仓库、统一账号权限
- ASN、收货、入库质检主流程
- 图像 + 规则 + 操作记录的混合输入
- 半自动质检
- `AI Gateway` 的 session / checkpoint / 压缩 / 模型配置
- 单一 `Vue` 前端项目
- `Gateway + Config Center + Redis + MQ + Object Storage + Observability`

第一期明确不交付：

- 退货质检
- 出库和库内其他域
- 复杂计费
- 全站型通用 BFF
- 细粒度全微服务化

## 自检清单

- 是否只聚焦在“生产级 WMS AI 入库质检”，没有滑向泛 AI 平台。
- 是否明确了 `Gateway`、`Platform Core`、`Inbound QC Core`、`AI Gateway` 的职责边界。
- 是否明确了 `AI Gateway` 只承载 AI 协议转接与会话编排，而不是通用 BFF。
- 是否覆盖了 session、checkpoint、上下文压缩、模型配置、审计和恢复。
- 是否明确了“单前端项目 + 普通业务直连网关 + AI 走 AI Gateway”的交互模式。
