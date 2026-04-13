# WMS AI 接口与事件契约设计

## 目标

把系统对内对外契约补齐：

- HTTP API
- AG-UI / SSE 事件
- 业务事件
- 错误码
- 状态机
- 权限矩阵

## 一、HTTP API 设计原则

- 路径统一使用小写复数资源
- 业务资源归属清晰
- 请求和响应显式携带租户/仓库上下文
- 不把内部领域对象直接原样暴露给前端

## 二、核心 HTTP API

## 1. ASN

### `GET /api/inbound/notices`

用途：

- 分页查询 ASN

### `POST /api/inbound/notices`

用途：

- 创建 ASN

请求示例：

```json
{
  "tenantId": "tenant_demo",
  "warehouseId": "wh_sz_01",
  "noticeNo": "ASN_DEMO_001",
  "supplierId": "sup_001",
  "lines": [
    { "skuId": "sku_001", "expectedQty": 100 }
  ]
}
```

响应示例：

```json
{
  "noticeId": "asn_001",
  "status": "pending_receipt"
}
```

## 2. Receipt

### `POST /api/inbound/receipts`

用途：

- 提交收货

## 3. QcTask

### `GET /api/qc/tasks/{taskId}`

用途：

- 查询工作台详情

### `GET /api/qc/tasks`

用途：

- 按状态/仓库查询任务

## 4. Evidence

### `POST /api/evidence/upload-sessions`

用途：

- 创建上传会话

### `POST /api/evidence/bindings`

用途：

- 绑定证据到任务

## 5. AI Session

### `POST /api/ai/sessions`

用途：

- 创建 AI 会话

### `GET /api/ai/sessions/{sessionId}/stream`

用途：

- 订阅 AG-UI / SSE

## 6. Decision

### `GET /api/qc/tasks/{taskId}/decision-preview`

用途：

- 查询 AI 建议预览

### `POST /api/qc/decisions/manual-review`

用途：

- 提交人工复核结果

## 三、AG-UI / SSE 事件契约

## 事件命名

- `session.bootstrap`
- `inspection.progress`
- `inspection.missing_evidence`
- `inspection.suggestion`
- `inspection.failed`
- `inspection.completed`

## 统一字段

所有事件统一包含：

- `sessionId`
- `eventId`
- `timestamp`
- `sequence`
- `type`
- `payload`

## 示例

```json
{
  "sessionId": "ais_001",
  "eventId": "evt_001",
  "timestamp": "2026-04-13T10:00:00Z",
  "sequence": 5,
  "type": "inspection.suggestion",
  "payload": {
    "suggestedDecision": "pass",
    "confidence": 0.97,
    "riskTags": []
  }
}
```

## 四、业务事件契约

## 事件版本

每个事件都必须有：

- `eventName`
- `eventVersion`

第一期统一从 `v1` 起。

## 关键事件

### `tenant_created.v1`

```json
{
  "eventId": "evt_tenant_001",
  "eventName": "tenant_created",
  "eventVersion": 1,
  "tenantId": "tenant_demo",
  "occurredAt": "2026-04-13T10:00:00Z",
  "payload": {
    "tenantCode": "TENANT_DEMO",
    "tenantName": "演示租户"
  }
}
```

### `receipt_recorded.v1`

```json
{
  "eventId": "evt_receipt_001",
  "eventName": "receipt_recorded",
  "eventVersion": 1,
  "tenantId": "tenant_demo",
  "warehouseId": "wh_sz_01",
  "payload": {
    "receiptId": "receipt_001",
    "noticeId": "asn_001"
  }
}
```

### `ai_suggestion_created.v1`

```json
{
  "eventId": "evt_ai_001",
  "eventName": "ai_suggestion_created",
  "eventVersion": 1,
  "tenantId": "tenant_demo",
  "warehouseId": "wh_sz_01",
  "payload": {
    "inspectionRunId": "air_001",
    "qcTaskId": "qct_001",
    "suggestedDecision": "pass",
    "confidence": 0.97,
    "riskTags": []
  }
}
```

### `qc_decision_finalized.v1`

```json
{
  "eventId": "evt_qcd_001",
  "eventName": "qc_decision_finalized",
  "eventVersion": 1,
  "tenantId": "tenant_demo",
  "warehouseId": "wh_sz_01",
  "payload": {
    "decisionId": "qcd_001",
    "qcTaskId": "qct_001",
    "decisionStatus": "manual_pass"
  }
}
```

## 五、状态机

## 1. `inbound_notice.status`

- `pending_receipt`
- `receiving`
- `received`
- `qc_in_progress`
- `completed`

## 2. `qc_task.status`

- `pending_inspection`
- `pending_evidence`
- `ai_running`
- `pending_manual_review`
- `auto_passed`
- `finalized`

## 3. `ai_session.status`

- `active`
- `waiting_evidence`
- `running`
- `completed`
- `failed`
- `archived`

## 4. `qc_decision.decision_status`

- `auto_pass`
- `manual_pass`
- `manual_reject`
- `hold`

## 六、错误码

错误码统一格式：

- `DOMAIN_REASON`

示例：

### 通用

- `AUTH_UNAUTHORIZED`
- `AUTH_FORBIDDEN`
- `REQUEST_INVALID`
- `RESOURCE_NOT_FOUND`
- `CONFLICT_DUPLICATED_REQUEST`

### 业务

- `INBOUND_NOTICE_NOT_FOUND`
- `QC_TASK_NOT_FOUND`
- `QC_TASK_STATUS_INVALID`
- `EVIDENCE_INCOMPLETE`
- `DECISION_ALREADY_FINALIZED`

### AI

- `AI_SESSION_NOT_FOUND`
- `AI_CHECKPOINT_NOT_FOUND`
- `AI_MODEL_PROFILE_MISSING`
- `AI_MODEL_ROUTE_FAILED`
- `AI_SUGGESTION_INVALID`

## 错误响应格式

```json
{
  "code": "QC_TASK_STATUS_INVALID",
  "message": "Current task status does not allow this operation.",
  "traceId": "00-abcd",
  "details": {
    "taskId": "qct_001",
    "status": "finalized"
  }
}
```

## 七、权限矩阵

| 动作 | PlatformAdmin | TenantAdmin | WarehouseSupervisor | Inspector |
| --- | --- | --- | --- | --- |
| 创建租户 | Y | N | N | N |
| 创建仓库 | Y | Y(本租户) | N | N |
| 管理用户 | Y | Y(本租户) | N | N |
| 创建 ASN | N | Y | Y | N |
| 提交收货 | N | N | Y | Y |
| 上传证据 | N | N | Y | Y |
| 启动 AI 检验 | N | N | Y | Y |
| 人工复核 | N | N | Y | N |
| 查看平台模型配置 | Y | N | N | N |
| 查看租户模型配置 | Y | Y | N | N |

## 八、评审必问点

评审时必须回答：

- 哪些状态可以迁移，哪些不能
- 哪些接口是前端调用，哪些是服务间事件
- 错误码如何保持稳定
- 事件如何做版本升级
- 权限矩阵如何落到接口鉴权
