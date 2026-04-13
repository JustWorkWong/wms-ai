# WMS AI 设计文档索引

这组文档替代此前的单文件设计稿，目的是把“业务、技术选型、架构、数据设计”拆开，便于逐步审阅。

## 阅读顺序

1. `2026-04-13-wms-ai-business.md`
2. `2026-04-13-wms-ai-technical-options.md`
3. `2026-04-13-wms-ai-architecture.md`
4. `2026-04-13-wms-ai-data-design.md`
5. `2026-04-13-wms-ai-distributed-transactions.md`
6. `2026-04-13-wms-ai-frontend-flow.md`
7. `2026-04-13-wms-ai-operations-and-scheduling.md`

## 文档分工

- `2026-04-13-wms-ai-business.md`
  只讲业务边界、业务流程、实体流转、角色与第一期范围
- `2026-04-13-wms-ai-technical-options.md`
  只讲技术选型、可选方案、取舍、推荐方案、分布式事务与 Aspire 能力边界
- `2026-04-13-wms-ai-architecture.md`
  只讲服务拆分、MAF workflow、Skill/MCP/Function Calling 配合、部署与观测
- `2026-04-13-wms-ai-data-design.md`
  只讲三库拆分、核心表设计、模型配置入库、初始化示例数据
- `2026-04-13-wms-ai-distributed-transactions.md`
  只讲 CAP 选型、事件清单、Outbox/Inbox、补偿和幂等
- `2026-04-13-wms-ai-frontend-flow.md`
  只讲前端 ASCII 流程图、页面到接口映射、核心入参与出参
- `2026-04-13-wms-ai-operations-and-scheduling.md`
  只讲后台作业、定时任务、运行维护、补偿扫描和调度框架

## 当前状态

- 旧的汇总稿：`2026-04-13-wms-ai-inbound-qc-design.md`
- 新的主审阅入口：本索引和上面的四份拆分文档
- 旧 implementation plan 当前暂停，等待这组拆分文档确认后重写
