# WMS AI 技术选型方案

## 目标

这份文档只回答两个问题：

1. 每个关键技术点有哪些可选方案
2. 生产级第一期为什么选当前方案

## 1. AI 执行框架

### 方案 A: `Semantic Kernel` 为主

优点：

- prompt / plugin 资产组织成熟
- 生态资料较多

缺点：

- 对这次项目最关键的 `session / checkpoint / workflow state / 恢复 / 审计` 不是天然强项
- 容易把稳定业务流程写成 prompt 编排

### 方案 B: `MAF + 自定义 Workflow` 为主

优点：

- 更适合 Agent 会话、历史、流式执行、上下文 provider
- 适合把 AI 会话和业务 workflow 分层
- 和 `Microsoft.Extensions.AI` 组合自然

缺点：

- 业务 workflow 需要自己设计，不是全靠框架生成

### 推荐

选 `方案 B`。  
第一期主链路用 `MAF + 自定义 Workflow`，不用 `SK` 做主执行引擎。

## 2. 本地编排与交付出口

### 方案 A: `docker compose` 手写

优点：

- 简单直观

缺点：

- 本地环境、云端配置、监控接入容易分叉
- 对多服务依赖管理和可视化支持弱

### 方案 B: `Aspire`

优点：

- 本地编排、资源依赖、连接注入、观测统一
- 自带 dashboard
- 方便后续导出部署资产

缺点：

- 团队要接受新的本地启动方式

### 推荐

选 `Aspire`。  
`Aspire AppHost` 负责本地拉起服务、数据库、缓存、队列、对象存储和 dashboard，并作为后续交付资产的来源。

## 3. 数据库策略

### 方案 A: 单库

优点：

- 简单
- 跨域事务容易

缺点：

- 用户、业务、AI 数据耦合
- 后期拆分、扩展、权限隔离、性能治理困难

### 方案 B: 三库拆分

- `UserDb`
- `BusinessDb`
- `AiDb`

优点：

- 边界清晰
- 不同数据生命周期和索引策略可分开治理
- 便于后续独立扩缩容

缺点：

- 跨库事务不再能靠单数据库事务解决

### 推荐

选 `三库拆分`。

#### `UserDb`

只放：

- Tenant
- Warehouse
- User
- Role
- Membership

#### `BusinessDb`

只放：

- Supplier
- Sku
- SkuQualityProfile
- InboundNotice / Receipt
- QcTask / QcDecision / Evidence 元数据

#### `AiDb`

只放：

- AiSession
- AiCheckpoint
- AiSummarySnapshot
- AiInspectionRun
- AiSuggestion
- AiModelProfile
- AiRoutingPolicy

## 4. 分布式事务

三库拆分后，不能假设有传统单事务。

### 方案 A: 两阶段提交 / 分布式事务协调器

优点：

- 理论上一致性强

缺点：

- 工程复杂度高
- 云原生环境下成本高
- 对队列、缓存、对象存储并不友好

### 方案 B: 使用成熟开源框架实现 `Outbox/Inbox + Saga/补偿`

优点：

- 更贴近云原生实践
- 可审计、可恢复、可重放

缺点：

- 需要接受最终一致性
- 业务流程必须设计补偿动作

### 推荐

选 `方案 B`，并明确落到 `CAP`，不自研事件总线或 Outbox 框架。

### 分布式事务框架选型

本项目分布式事务采用：

- `CAP`
- `RabbitMQ`
- `EF Core`

原因：

- `CAP` 本身就是为事件总线和分布式事务问题设计
- 能与 `EF Core` 事务配合
- 更贴近当前 `.NET + PostgreSQL + RabbitMQ` 方案
- 比自己手写 `Outbox publisher / Inbox consumer / retry scheduler` 更稳

### 分布式事务策略

- 用户服务写 `UserDb` 时，只保证本地事务
- 业务服务写 `BusinessDb` 时，只保证本地事务
- AI 服务写 `AiDb` 时，只保证本地事务
- 跨库同步靠：
  - `CAP` 管理的消息持久化
  - 业务事件
  - Saga / 补偿

### 典型场景

#### 场景 1: 租户创建后开通业务空间

1. `Platform` 在 `UserDb` 建租户和仓库
2. 同事务写 `Outbox`
3. `Inbound` 消费事件，在 `BusinessDb` 建租户业务空间
4. `AiGateway` 消费事件，在 `AiDb` 建租户 AI 空间

#### 场景 2: AI 建议转业务结论

1. `AiGateway` 在 `AiDb` 落 `AiSuggestion`
2. 同事务写 `Outbox`
3. `Inbound` 消费事件，写 `QcDecision` 或待人工复核状态
4. 如果业务写入失败，保留重试与人工补偿入口

## 5. 模型配置存储

### 方案 A: 只放配置文件

优点：

- 简单

缺点：

- 无法做租户覆盖、灰度、历史追溯
- 生产审计不足

### 方案 B: 配置文件 + 数据库

优点：

- 平台默认值可放配置中心
- 运行中的模型策略、租户覆盖、版本审计可放数据库

缺点：

- 设计稍复杂

### 推荐

选 `方案 B`。

### 推荐模型配置层级

1. 代码默认值
2. 配置中心环境级默认值
3. `AiDb` 中的平台级模型配置
4. `AiDb` 中的租户覆盖配置
5. 运行时生成配置快照并绑定到 `AiInspectionRun`

## 6. 对象存储

### 方案 A: 文件直接落数据库

不推荐。大对象会拖垮数据库。

### 方案 B: 对象存储 + 元数据入库

推荐。文件放 `MinIO/S3`，业务和 AI 只保存引用、摘要和版本。

## 7. Aspire 的持久化和 UI

生产级本地环境要求：

- `PostgreSQL`、`Redis`、`RabbitMQ`、`MinIO` 都要持久化
- `Aspire Dashboard` 必须开启，供开发查看资源、日志、追踪和健康状态

本地设计要求：

- 数据卷持久化，不因重启丢库
- 容器资源在 `Aspire` dashboard 可见
- SQL、缓存、消息队列、对象存储都能从 `AppHost` 一键拉起

## 8. 日志与观测

### 方案 A: 只记应用日志

不够。

### 方案 B: 日志 + 指标 + 链路追踪

推荐。并且要覆盖：

- HTTP
- EF Core
- MQ
- Object Storage
- MAF session / workflow / tool / checkpoint

## 9. 初始化数据策略

### 方案 A: 运行时代码里硬编码 Seeder

优点：

- 快

缺点：

- 维护差
- 环境切换不清楚

### 方案 B: 样例数据脚本/JSON + 导入器

推荐。  
把租户、仓库、用户、SKU、ASN、规则、模型配置做成结构化样例数据，系统提供导入器。

好处：

- 后续演示环境、测试环境、PoC 环境不需要手写初始化
- 数据版本可控

## 10. 总结

第一期推荐技术路线：

- AI 执行：`MAF + 自定义 Workflow`
- 本地编排：`Aspire`
- 数据：`PostgreSQL` 三库拆分
- 跨库一致性：`Outbox/Inbox + Saga`
- 文件：`MinIO/S3`
- 缓存：`Redis`
- 异步：`RabbitMQ`
- 模型配置：`配置中心 + AiDb`
- 观测：`OpenTelemetry + Aspire Dashboard`
