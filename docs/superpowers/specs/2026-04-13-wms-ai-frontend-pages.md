# WMS AI 前端页面设计

## 目标

把前端从“有一条作业链”推进到“有完整页面矩阵、页面功能和页面级接口”。

## 页面总览

第一期前端是一个 `Vue` 项目，但页面要分 4 组：

1. 平台管理
2. 租户与主数据管理
3. 入库与质检作业
4. 管理分析与 AI 助手

## 一、平台管理页

## 1. 租户列表页

路径：

- `/platform/tenants`

功能：

- 查看租户
- 创建租户
- 启用/停用租户

接口：

- `GET /api/platform/tenants`
- `POST /api/platform/tenants`

## 2. 仓库管理页

路径：

- `/platform/warehouses`

功能：

- 按租户查看仓库
- 创建仓库
- 启用/停用仓库

接口：

- `GET /api/platform/warehouses`
- `POST /api/platform/warehouses`

## 3. 用户与成员关系页

路径：

- `/platform/users`

功能：

- 查看用户
- 创建用户
- 分配成员关系与角色

接口：

- `GET /api/platform/users`
- `POST /api/platform/users`
- `POST /api/platform/memberships`

## 4. 模型配置管理页

路径：

- `/platform/ai/model-profiles`

功能：

- 查看 provider
- 查看/创建模型 profile
- 配置租户路由策略
- 启停 profile

接口：

- `GET /api/platform/ai/model-providers`
- `GET /api/platform/ai/model-profiles`
- `POST /api/platform/ai/model-profiles`
- `POST /api/platform/ai/routing-policies`

## 二、租户与主数据管理页

## 1. SKU 列表页

路径：

- `/master/skus`

功能：

- 查看 SKU
- 创建 SKU
- 编辑基础信息

接口：

- `GET /api/master/skus`
- `POST /api/master/skus`

## 2. 质检规则档案页

路径：

- `/master/sku-quality-profiles`

功能：

- 查看每个 SKU 的质检档案
- 维护证据规则
- 维护自动通过阈值

接口：

- `GET /api/master/sku-quality-profiles`
- `POST /api/master/sku-quality-profiles`

## 3. 供应商页

路径：

- `/master/suppliers`

功能：

- 查看供应商
- 创建供应商

接口：

- `GET /api/master/suppliers`
- `POST /api/master/suppliers`

## 三、入库与质检作业页

## 1. ASN 列表页

路径：

- `/inbound/notices`

功能：

- 查看 ASN
- 创建 ASN
- 按状态筛选

接口：

- `GET /api/inbound/notices`
- `POST /api/inbound/notices`

## 2. ASN 详情页

路径：

- `/inbound/notices/:noticeId`

功能：

- 查看明细
- 发起收货

接口：

- `GET /api/inbound/notices/{noticeId}`
- `POST /api/inbound/receipts`

## 3. 质检任务列表页

路径：

- `/qc/tasks`

功能：

- 按仓库、状态、分配人筛选
- 查看待补证据、待人工复核、已完成任务

接口：

- `GET /api/qc/tasks`

## 4. 质检作业台

路径：

- `/workbench/qc/:taskId`

功能：

- 查看任务详情
- 查看规则要求
- 上传证据
- 启动 AI 检验
- 查看 AG-UI 事件流
- 查看建议预览

接口：

- `GET /api/qc/tasks/{taskId}`
- `POST /api/evidence/upload-sessions`
- `POST /api/evidence/bindings`
- `POST /api/ai/sessions`
- `GET /api/ai/sessions/{sessionId}/stream`
- `GET /api/qc/tasks/{taskId}/decision-preview`

## 5. 人工复核页

路径：

- `/qc/reviews/:taskId`

功能：

- 查看证据
- 查看 AI 建议
- 提交人工结论

接口：

- `GET /api/qc/tasks/{taskId}`
- `GET /api/qc/tasks/{taskId}/decision-preview`
- `POST /api/qc/decisions/manual-review`

## 四、管理分析与 AI 助手页

## 1. 质检分析看板

路径：

- `/analytics/qc`

功能：

- 查看吞吐
- 查看自动通过率
- 查看人工复核率
- 查看供应商风险分布

接口：

- `GET /api/analytics/qc/summary`
- `GET /api/analytics/qc/supplier-risk`

## 2. 管理 AI 助手页

路径：

- `/analytics/assistant`

功能：

- 自然语言查询异常趋势
- 查询仓库表现
- 查询规则命中情况

接口：

- `POST /api/ai/sessions`
- `GET /api/ai/sessions/{sessionId}/stream`

## 页面级 ASCII 图

```text
[平台管理]
  ├─ 租户列表
  ├─ 仓库管理
  ├─ 用户与成员关系
  └─ 模型配置管理

[主数据管理]
  ├─ SKU 列表
  ├─ 质检规则档案
  └─ 供应商页

[入库与质检]
  ├─ ASN 列表
  ├─ ASN 详情
  ├─ 质检任务列表
  ├─ 质检作业台
  └─ 人工复核页

[分析与助手]
  ├─ 质检分析看板
  └─ 管理 AI 助手页
```

## 页面功能与接口穿透

### 质检作业台

1. 页面加载：`GET /api/qc/tasks/{taskId}`
2. 上传会话：`POST /api/evidence/upload-sessions`
3. 文件上传：直传 `MinIO/S3`
4. 绑定证据：`POST /api/evidence/bindings`
5. 启动会话：`POST /api/ai/sessions`
6. 订阅事件：`GET /api/ai/sessions/{sessionId}/stream`
7. 查看建议：`GET /api/qc/tasks/{taskId}/decision-preview`

### 模型配置管理页

1. 页面加载：`GET /api/platform/ai/model-providers`
2. 查询 profile：`GET /api/platform/ai/model-profiles`
3. 创建 profile：`POST /api/platform/ai/model-profiles`
4. 租户路由：`POST /api/platform/ai/routing-policies`

## 需要补在实现里的前端约束

- 每个页面都必须明确角色可见性
- 每个页面都必须定义筛选项、表格列、详情卡片字段
- 每个页面都必须绑定对应错误码和空状态

这样前端才不是“只有几条路由”，而是完整业务系统界面。
