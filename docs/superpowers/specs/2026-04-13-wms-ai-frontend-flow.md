# WMS AI 前后端整链接口设计

## 目标

把前端页面、后端接口、请求体、响应体和业务状态流串成一条完整链，避免“只有后端结构，没有前端调用路径”。

## 一、作业台主链 ASCII 图

```text
+-------------------+       +-----------+       +-----------+       +-----------+
| Vue Workbench     | ----> | Gateway   | ----> | Inbound   | ----> | BusinessDb|
| QcWorkbenchView   |       |           |       |           |       |           |
+-------------------+       +-----------+       +-----------+       +-----------+
         |                         |
         |                         +----------------------------------------------+
         |                                                                |
         v                                                                v
+-------------------+       +-----------+       +-----------+       +-----------+
| AiInspectorPanel  | ----> | Gateway   | ----> | AiGateway | ----> | AiDb      |
| AG-UI / SSE       |       |           |       |           |       |           |
+-------------------+       +-----------+       +-----------+       +-----------+
         |
         v
+-------------------+
| Evidence Upload   |
| MinIO / S3        |
+-------------------+
```

## 二、页面到接口映射

## 1. ASN 列表页

页面：

- `/inbound/notices`

接口：

- `GET /api/inbound/notices`

请求参数：

```json
{
  "warehouseId": "wh_sz_01",
  "status": "pending_receipt",
  "page": 1,
  "pageSize": 20
}
```

响应示例：

```json
{
  "items": [
    {
      "id": "asn_001",
      "noticeNo": "ASN_DEMO_001",
      "warehouseId": "wh_sz_01",
      "status": "pending_receipt",
      "supplierName": "演示供应商"
    }
  ],
  "total": 1
}
```

## 2. 收货提交

页面动作：

- ASN 详情页点击“提交收货”

接口：

- `POST /api/inbound/receipts`

请求示例：

```json
{
  "tenantId": "tenant_demo",
  "warehouseId": "wh_sz_01",
  "noticeId": "asn_001",
  "receiptNo": "RCV_DEMO_001",
  "lines": [
    {
      "skuId": "sku_001",
      "receivedQty": 100
    }
  ]
}
```

响应示例：

```json
{
  "receiptId": "receipt_001",
  "status": "received",
  "qcPlanId": "qcp_001"
}
```

## 3. 质检任务详情页

页面：

- `/workbench/qc/:taskId`

接口：

- `GET /api/qc/tasks/{taskId}`

响应示例：

```json
{
  "taskId": "qct_001",
  "taskNo": "QCT_DEMO_001",
  "tenantId": "tenant_demo",
  "warehouseId": "wh_sz_01",
  "skuName": "手机壳-透明款",
  "status": "pending_inspection",
  "requiredEvidenceRules": [
    "front_view",
    "back_view",
    "outer_box_label"
  ],
  "boundEvidence": [],
  "allowedActions": [
    "upload_evidence",
    "start_ai_inspection"
  ]
}
```

## 4. 创建上传会话

页面动作：

- 质检员点“上传图片”

接口：

- `POST /api/evidence/upload-sessions`

请求示例：

```json
{
  "tenantId": "tenant_demo",
  "warehouseId": "wh_sz_01",
  "fileName": "front.jpg",
  "contentType": "image/jpeg"
}
```

响应示例：

```json
{
  "objectKey": "tenant_demo/wh_sz_01/qct_001/front.jpg",
  "bucketName": "wms-ai-evidence",
  "uploadUrl": "https://minio.local/presigned-put"
}
```

## 5. 绑定证据到任务

接口：

- `POST /api/evidence/bindings`

请求示例：

```json
{
  "tenantId": "tenant_demo",
  "warehouseId": "wh_sz_01",
  "qcTaskId": "qct_001",
  "objectKey": "tenant_demo/wh_sz_01/qct_001/front.jpg",
  "bindingType": "inspection_photo"
}
```

响应示例：

```json
{
  "bindingId": "bind_001",
  "status": "bound"
}
```

## 6. 启动 AI 检验会话

页面动作：

- 质检员点击“开始分析”

接口：

- `POST /api/ai/sessions`

请求示例：

```json
{
  "tenantId": "tenant_demo",
  "userId": "qc_inspector",
  "sessionType": "workbench_inspection",
  "businessObjectType": "qc_task",
  "businessObjectId": "qct_001"
}
```

响应示例：

```json
{
  "sessionId": "ais_001",
  "status": "active",
  "streamUrl": "/api/ai/sessions/ais_001/stream"
}
```

## 7. 订阅 AG-UI / SSE 事件流

接口：

- `GET /api/ai/sessions/{sessionId}/stream`

事件示例：

```text
event: session.bootstrap
data: {"sessionId":"ais_001","status":"running"}

event: inspection.progress
data: {"step":"check_evidence","message":"正在检查证据完整性"}

event: inspection.suggestion
data: {"suggestedDecision":"pass","confidence":0.97,"riskTags":[]}
```

## 8. AI 建议回写业务

服务间动作：

- `AiGateway` 发布 `ai_suggestion_created`
- `Inbound` 消费并形成业务分支

前端查询接口：

- `GET /api/qc/tasks/{taskId}/decision-preview`

响应示例：

```json
{
  "taskId": "qct_001",
  "previewStatus": "auto_pass_candidate",
  "suggestedDecision": "pass",
  "confidence": 0.97,
  "riskTags": [],
  "missingEvidence": []
}
```

## 9. 人工复核提交

接口：

- `POST /api/qc/decisions/manual-review`

请求示例：

```json
{
  "tenantId": "tenant_demo",
  "warehouseId": "wh_sz_01",
  "qcTaskId": "qct_001",
  "decisionStatus": "manual_pass",
  "reasonSummary": "外箱轻微褶皱，不影响入库"
}
```

响应示例：

```json
{
  "decisionId": "qcd_001",
  "status": "finalized"
}
```

## 三、前端状态流

## 1. 任务状态

- `pending_inspection`
- `pending_evidence`
- `ai_running`
- `pending_manual_review`
- `auto_passed`
- `finalized`

## 2. 页面动作映射

- `pending_inspection`
  可上传证据、可开始 AI 分析
- `pending_evidence`
  只可补证据
- `ai_running`
  只可查看事件流
- `pending_manual_review`
  主管可提交复核
- `auto_passed`
  只读
- `finalized`
  只读

## 四、前后端穿透链

这条链必须在评审时能讲通：

1. 前端查 `qc_task`
2. 前端创建上传会话
3. 前端上传到对象存储
4. 前端绑定证据
5. 前端启动 `ai_session`
6. `AiGateway` 走 `MAF Workflow`
7. `AiGateway` 通过 AG-UI/SSE 推送进度
8. `AiGateway` 发布 `ai_suggestion_created`
9. `Inbound` 形成自动通过或人工复核状态
10. 前端显示最终状态
