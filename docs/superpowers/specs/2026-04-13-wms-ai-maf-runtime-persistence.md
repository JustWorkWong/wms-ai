# WMS AI MAF 运行态持久化设计

## 目标

把 `MAF` 在本项目里的作用落到数据库和审计模型，而不是只停留在“有 workflow、有 session”的描述层。

## 一、核心结论

`MAF` 在本项目中不只是：

- 调模型
- 挂工具
- 做流式输出

还必须承担：

- session 持久化
- message 历史持久化
- workflow run / step 持久化
- checkpoint 恢复
- tool / MCP / function 调用审计
- model API 调用审计

所以 `AiDb` 里需要的不只是 `ai_sessions` 和 `ai_suggestions`。

## 二、命名策略

为了和 `MAF` 语义对齐，`AiDb` 中与运行时强相关的表建议统一使用 `maf_` 前缀，而不是全部使用泛化的 `ai_` 前缀。

建议拆成两类：

### 1. `maf_*`

代表 MAF 运行时对象：

- `maf_sessions`
- `maf_messages`
- `maf_checkpoints`
- `maf_summary_snapshots`
- `maf_workflow_runs`
- `maf_workflow_step_runs`
- `maf_tool_call_logs`
- `maf_model_call_logs`

### 2. `ai_*`

代表项目级 AI 业务对象：

- `ai_inspection_runs`
- `ai_suggestions`
- `ai_model_providers`
- `ai_model_profiles`
- `ai_routing_policies`
- `ai_prompt_assets`

## 三、推荐表设计

## 1. `maf_sessions`

作用：

- 一条可恢复的 AgentSession

关键字段：

- `id`
- `tenant_id`
- `user_id`
- `session_type`
- `business_object_type`
- `business_object_id`
- `status`
- `agent_session_json`
- `last_checkpoint_id`
- `created_at`
- `updated_at`

说明：

- `agent_session_json` 用于保存 MAF 运行时序列化结果

## 2. `maf_messages`

作用：

- 保存消息历史

关键字段：

- `id`
- `session_id`
- `sequence`
- `role`
- `message_type`
- `content_text`
- `content_json`
- `is_summary`
- `created_at`

不变量：

- `(session_id, sequence)` 唯一

## 3. `maf_checkpoints`

作用：

- 保存恢复点

关键字段：

- `id`
- `session_id`
- `checkpoint_name`
- `workflow_run_id`
- `workflow_step_run_id`
- `summary_snapshot_id`
- `cursor`
- `created_at`

## 4. `maf_summary_snapshots`

作用：

- 保存长会话压缩结果

关键字段：

- `id`
- `session_id`
- `summary_text`
- `evidence_refs_json`
- `message_range_json`
- `created_at`

## 5. `maf_workflow_runs`

作用：

- 保存 workflow 总实例

建议字段和你现有 demo 保持同类语义：

- `id`
- `workflow_name`
- `status`
- `requested_by`
- `user_input`
- `routing_json`
- `result_json`
- `error_message`
- `current_node`
- `created_at`
- `updated_at`
- `completed_at`

## 6. `maf_workflow_step_runs`

作用：

- 保存每个节点的执行历史

字段建议沿用你现有 demo 的表达：

- `id`
- `workflow_run_id`
- `sequence`
- `node_name`
- `step_kind`
- `status`
- `attempt_count`
- `message`
- `input_json`
- `payload_json`
- `evidence_json`
- `error_message`
- `started_at`
- `completed_at`

## 7. `maf_tool_call_logs`

作用：

- 记录 tool / function / MCP 调用

关键字段：

- `id`
- `session_id`
- `workflow_run_id`
- `workflow_step_run_id`
- `call_type`
- `tool_name`
- `input_json`
- `output_json`
- `status`
- `duration_ms`
- `created_at`

`call_type` 示例：

- `function`
- `mcp_tool`
- `mcp_resource`

## 8. `maf_model_call_logs`

作用：

- 记录模型 API 调用元数据

关键字段：

- `id`
- `session_id`
- `workflow_run_id`
- `workflow_step_run_id`
- `provider_code`
- `model_name`
- `profile_code`
- `request_tokens`
- `response_tokens`
- `total_tokens`
- `latency_ms`
- `finish_reason`
- `request_meta_json`
- `response_meta_json`
- `error_message`
- `created_at`

说明：

- 这里默认不落完整敏感 prompt 原文
- 只落审计必需元数据和必要摘要

## 四、还需要保存什么

除了 session、workflow 和 suggestion，还建议保存：

- `agent_session_json`
- message 历史
- summary 快照
- tool / MCP / function 调用历史
- model API 调用元数据
- 失败轨迹和重试轨迹
- prompt / skill 版本
- 运行时模型配置快照

## 五、模型提供方信息

你提到“AI 的 API 信息没有数据库设计”，这里需要补。

### `ai_model_providers`

建议增加：

- `provider_code`
- `provider_name`
- `api_base_url`
- `api_version`
- `auth_mode`
- `credential_ref`
- `status`

说明：

- 不建议直接把明文密钥放数据库
- 数据库只保存 `credential_ref`
- 真正密钥放密钥管理系统或受保护配置源

### `ai_model_profiles`

建议增加：

- `provider_id`
- `profile_code`
- `scene_code`
- `model_name`
- `temperature`
- `top_p`
- `max_tokens`
- `timeout_seconds`
- `retry_policy_json`
- `prompt_asset_version`
- `is_active`
- `version`

## 六、Agent 是否需要落库

需要，但不是把“Agent”这个概念单独做一张空洞表。

真正应该落库的是 Agent 的运行痕迹：

- session
- messages
- workflow run
- workflow step run
- tool call
- model call
- checkpoint

这样才能回答生产问题：

- 这轮到底跑到了哪一步
- 为什么失败
- 调了什么工具
- 用了哪个模型
- 能不能恢复

## 七、与 DDD 的关系

`MAF` 的运行态属于 `AI Runtime Context` 的聚合，不属于 `Inbound Quality Context` 的业务真相。

所以：

- `QcDecision` 放 `BusinessDb`
- `MafSession / MafWorkflowRun / MafModelCallLog` 放 `AiDb`

这样能把“业务真相”和“AI 运行真相”分开。
