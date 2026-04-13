# WMS AI 后台作业与定时任务设计

## 目标

明确回答：

1. 这个系统要不要引入定时任务
2. 哪些任务应该是事件驱动，哪些应该是定时驱动
3. 第一期开工时需要哪些后台作业

## 结论

要引入，但只能作为辅助系统能力，不能替代主业务流程。

### 主业务链

必须用：

- `MAF Workflow`
- `CAP` 事件

不应用：

- cron 直接驱动质检主流程

### 定时任务

只负责：

- 补偿扫描
- 超时扫描
- 清理归档
- 指标汇总
- 健康检查扩展任务

## 调度框架

第一期推荐：

- `Quartz.NET`

不推荐让简单 `BackgroundService` 承担全部调度职责。

## 任务分类

## 1. 运行补偿类

### `scan_pending_tenant_provisioning`

用途：

- 扫描平台已建租户但业务空间或 AI 空间未完成开通的记录

触发：

- 每 5 分钟

动作：

- 重新发布或补做初始化事件

### `scan_pending_ai_runs`

用途：

- 扫描长时间停留在 `pending_ai` 或 `running` 的检验运行

触发：

- 每 2 分钟

动作：

- 判断是否需要恢复 workflow
- 或标记为 `manual_intervention_required`

## 2. 超时治理类

### `expire_upload_sessions`

用途：

- 清理超时未完成的上传会话

触发：

- 每 10 分钟

### `expire_idle_ai_sessions`

用途：

- 标记长时间无交互的 AI session

触发：

- 每 30 分钟

## 3. 数据治理类

### `archive_completed_ai_runs`

用途：

- 把已完成且超过保留期的 AI 运行归档

触发：

- 每天凌晨

### `compact_ai_summaries`

用途：

- 对超长会话做二次摘要压缩

触发：

- 每小时

## 4. 运营统计类

### `build_daily_qc_metrics`

用途：

- 计算每日质检吞吐、自动通过率、人工复核率

触发：

- 每天凌晨

### `build_supplier_risk_snapshot`

用途：

- 汇总供应商风险快照

触发：

- 每天凌晨

## 调度边界

## 应该由事件驱动

- 收货后生成质检任务
- 证据绑定后触发 AI 检验
- AI 建议回写业务结论
- 人工复核完成后回写 AI 会话

## 应该由定时任务驱动

- 漏处理扫描
- 超时关闭
- 归档压缩
- 指标汇总

## 多实例要求

Quartz 必须按持久化模式部署，避免多实例重复执行。

要求：

- 使用持久化 job store
- 开启集群协调
- 作业参数和执行历史可追踪

## Aspire 集成要求

`AppHost` 应把后台调度服务视为正式资源：

- `Quartz` worker 或 hosted service
- 与 `PostgreSQL`、`Redis`、`RabbitMQ` 一起被编排
- 日志和 tracing 进入 `Aspire Dashboard`

## 评审结论

评审时必须讲清楚：

- 这个系统需要定时任务
- 但定时任务不是主业务编排引擎
- 主链靠 `Workflow + CAP`
- 定时任务只承担“扫漏、超时、归档、统计、补偿”
