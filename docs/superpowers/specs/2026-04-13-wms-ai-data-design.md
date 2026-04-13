# WMS AI 数据设计

## 总原则

数据库分三套：

1. `UserDb`
2. `BusinessDb`
3. `AiDb`

对象存储独立：

- `MinIO / S3`

## 命名规范

数据库对象统一使用 `snake_case`，避免混用驼峰、缩写和大小写引用。

### 表名

- 使用复数名词
- 使用完整业务语义

示例：

- `inbound_notices`
- `receipt_lines`
- `qc_tasks`
- `ai_model_profiles`

避免：

- `InboundNotice`
- `QcTaskTbl`
- `sku_qp`

### 列名

- 主键统一：`id`
- 外键统一：`<entity>_id`
- 时间统一：`*_at`
- 状态统一：`status`
- 布尔统一：`is_*` 或 `has_*`

示例：

- `tenant_id`
- `warehouse_id`
- `created_at`
- `updated_at`
- `is_active`

### 索引与约束命名

- 主键：`pk_<table>`
- 外键：`fk_<table>__<ref_table>`
- 唯一约束：`uk_<table>__<col1>__<col2>`
- 普通索引：`ix_<table>__<col1>__<col2>`

示例：

- `pk_qc_tasks`
- `fk_qc_tasks__qc_plans`
- `uk_warehouses__tenant_id__code`
- `ix_ai_sessions__tenant_id__business_object_id`

### 枚举与状态字段

第一期优先用稳定的字符串代码，不在数据库层大量使用数据库私有 enum 类型。

示例：

- `status = 'pending_manual_review'`
- `decision_source = 'ai_auto_pass'`

这样更利于跨环境迁移和后续事件载荷对齐。

## 一、UserDb

## 1. 作用

只保存平台身份和组织结构：

- Tenant
- Warehouse
- User
- Role
- Membership

## 2. 建议核心表

### `tenants`

- `id`
- `code`
- `name`
- `status`
- `created_at`

### `warehouses`

- `id`
- `tenant_id`
- `code`
- `name`
- `status`
- `created_at`

唯一约束建议：

- `(tenant_id, code)`

### `users`

- `id`
- `username`
- `display_name`
- `password_hash`
- `status`

### `roles`

- `id`
- `code`
- `name`

### `memberships`

- `id`
- `tenant_id`
- `warehouse_id`
- `user_id`
- `role_code`
- `status`

## 二、BusinessDb

## 1. 作用

只保存业务真相：

- 主数据
- 入库单据
- 收货
- 质检任务
- 证据元数据
- 正式结论

## 2. 建议核心表

### `suppliers`

- `id`
- `tenant_id`
- `code`
- `name`

### `skus`

- `id`
- `tenant_id`
- `sku_code`
- `name`
- `spec`
- `unit`

### `sku_quality_profiles`

- `id`
- `tenant_id`
- `sku_id`
- `inspection_mode`
- `required_evidence_rules_json`
- `risk_threshold_json`
- `active_version`

### `inbound_notices`

- `id`
- `tenant_id`
- `warehouse_id`
- `notice_no`
- `supplier_id`
- `status`
- `expected_arrival_at`

### `inbound_notice_lines`

- `id`
- `notice_id`
- `sku_id`
- `expected_qty`

### `receipts`

- `id`
- `tenant_id`
- `warehouse_id`
- `notice_id`
- `receipt_no`
- `status`
- `received_at`

### `receipt_lines`

- `id`
- `receipt_id`
- `sku_id`
- `received_qty`

### `qc_plans`

- `id`
- `tenant_id`
- `warehouse_id`
- `notice_id`
- `receipt_id`
- `plan_status`

### `qc_tasks`

- `id`
- `tenant_id`
- `warehouse_id`
- `plan_id`
- `sku_id`
- `task_no`
- `status`
- `assigned_to_user_id`

### `evidence_assets`

- `id`
- `tenant_id`
- `warehouse_id`
- `object_key`
- `bucket_name`
- `content_type`
- `file_size`
- `sha256`
- `uploaded_by`

### `evidence_bindings`

- `id`
- `tenant_id`
- `warehouse_id`
- `qc_task_id`
- `evidence_asset_id`
- `binding_type`

### `qc_findings`

- `id`
- `tenant_id`
- `warehouse_id`
- `qc_task_id`
- `finding_type`
- `severity`
- `description`

### `qc_decisions`

- `id`
- `tenant_id`
- `warehouse_id`
- `qc_task_id`
- `decision_status`
- `decision_source`
- `reviewed_by`
- `reviewed_at`
- `reason_summary`

## 三、AiDb

## 1. 作用

只保存 AI 运行状态与配置：

- 会话
- checkpoint
- 摘要
- 检验运行
- 建议
- 模型配置
- 路由策略

## 2. 建议核心表

### `ai_sessions`

- `id`
- `tenant_id`
- `user_id`
- `session_type`
- `business_object_type`
- `business_object_id`
- `status`
- `last_checkpoint_id`

### `ai_checkpoints`

- `id`
- `session_id`
- `checkpoint_name`
- `workflow_node`
- `summary_snapshot_id`
- `model_profile_version`
- `cursor`
- `created_at`

### `ai_summary_snapshots`

- `id`
- `session_id`
- `summary_text`
- `evidence_refs_json`
- `message_range_json`

### `ai_inspection_runs`

- `id`
- `tenant_id`
- `session_id`
- `qc_task_id`
- `status`
- `started_at`
- `completed_at`

### `ai_suggestions`

- `id`
- `inspection_run_id`
- `suggested_decision`
- `confidence`
- `risk_tags_json`
- `missing_evidence_json`
- `recommended_actions_json`
- `reasoning_summary`

### `ai_model_providers`

- `id`
- `provider_code`
- `provider_name`
- `base_url`
- `auth_mode`
- `status`

### `ai_model_profiles`

- `id`
- `provider_id`
- `profile_code`
- `model_name`
- `scene_code`
- `temperature`
- `max_tokens`
- `timeout_seconds`
- `retry_policy_json`
- `is_active`
- `version`

### `ai_routing_policies`

- `id`
- `tenant_id`
- `scene_code`
- `default_profile_id`
- `fallback_profile_id`
- `strategy_json`

### `ai_prompt_assets`

- `id`
- `scene_code`
- `skill_code`
- `version`
- `content`
- `status`

## 四、模型配置入库要求

模型配置必须在 `AiDb` 中管理，不能只放配置文件。

至少要支持：

- 平台默认模型
- 租户覆盖模型
- 场景路由
- 版本化
- 回滚
- 运行快照绑定

每次 `AiInspectionRun` 执行时，都应固化：

- 使用的 `profile`
- `temperature`
- `max_tokens`
- `timeout`
- prompt/skill 版本

## 五、初始化样例数据

这部分不是临时 Seeder，而是后续演示环境、测试环境、PoC 环境都可以复用的样例数据基线。

## 1. 组织数据示例

```json
{
  "tenants": [
    { "code": "TENANT_DEMO", "name": "演示租户" }
  ],
  "warehouses": [
    { "tenantCode": "TENANT_DEMO", "code": "WH_SZ_01", "name": "深圳一仓" },
    { "tenantCode": "TENANT_DEMO", "code": "WH_SH_01", "name": "上海一仓" }
  ],
  "users": [
    { "username": "platform.admin", "displayName": "平台管理员", "role": "PlatformAdmin" },
    { "username": "tenant.admin", "displayName": "租户管理员", "role": "TenantAdmin", "tenantCode": "TENANT_DEMO" },
    { "username": "qc.supervisor", "displayName": "质检主管", "role": "WarehouseSupervisor", "tenantCode": "TENANT_DEMO", "warehouseCode": "WH_SZ_01" },
    { "username": "qc.inspector", "displayName": "质检员", "role": "Inspector", "tenantCode": "TENANT_DEMO", "warehouseCode": "WH_SZ_01" }
  ]
}
```

## 2. SKU 与规则示例

```json
{
  "skus": [
    {
      "skuCode": "SKU_IPHONE_CASE_001",
      "name": "手机壳-透明款",
      "spec": "透明 / 单个装"
    }
  ],
  "qualityProfiles": [
    {
      "skuCode": "SKU_IPHONE_CASE_001",
      "inspectionMode": "VisualAndLabel",
      "requiredEvidenceRules": [
        "front_view",
        "back_view",
        "outer_box_label"
      ],
      "riskThreshold": {
        "autoPassConfidence": 0.95,
        "maxAllowedRiskTags": 0
      }
    }
  ]
}
```

## 3. ASN 与收货示例

```json
{
  "inboundNotices": [
    {
      "tenantCode": "TENANT_DEMO",
      "warehouseCode": "WH_SZ_01",
      "noticeNo": "ASN_DEMO_001",
      "supplierCode": "SUP_DEMO_001",
      "lines": [
        { "skuCode": "SKU_IPHONE_CASE_001", "expectedQty": 100 }
      ]
    }
  ],
  "receipts": [
    {
      "noticeNo": "ASN_DEMO_001",
      "receiptNo": "RCV_DEMO_001",
      "lines": [
        { "skuCode": "SKU_IPHONE_CASE_001", "receivedQty": 100 }
      ]
    }
  ]
}
```

## 4. 模型配置示例

```json
{
  "modelProfiles": [
    {
      "profileCode": "inspection-multimodal-default",
      "sceneCode": "inspection",
      "providerCode": "openai",
      "modelName": "gpt-4.1-mini",
      "temperature": 0.1,
      "maxTokens": 3000,
      "timeoutSeconds": 45
    },
    {
      "profileCode": "inspection-summary-default",
      "sceneCode": "summary",
      "providerCode": "openai",
      "modelName": "gpt-4.1-mini",
      "temperature": 0.0,
      "maxTokens": 1500,
      "timeoutSeconds": 20
    }
  ]
}
```

## 六、数据初始化建议

初始化不要散落在代码里硬编码。建议：

- `seed/userdb/*.json`
- `seed/businessdb/*.json`
- `seed/aidb/*.json`

系统启动时提供：

- 导入器
- 幂等校验
- 版本标记

这样后续你就不需要反复手工初始化租户、仓库、用户、SKU、模型配置。
