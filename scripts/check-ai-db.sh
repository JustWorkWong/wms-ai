#!/bin/bash

# AI 数据库检查脚本
# 用途：快速查看 AI 相关表的数据

DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="wmsai_ai"
DB_USER="wmsai"

echo "🔍 检查 AI 数据库状态..."
echo "================================"
echo ""

# 检查 MafWorkflowRuns 表
echo "📋 MAF 工作流运行记录:"
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT
    id,
    workflow_type,
    status,
    created_at,
    completed_at
FROM maf_workflow_runs
ORDER BY created_at DESC
LIMIT 10;
"

echo ""
echo "📋 检验运行记录:"
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT
    id,
    qc_task_id,
    decision,
    confidence_score,
    created_at
FROM inspection_runs
ORDER BY created_at DESC
LIMIT 10;
"

echo ""
echo "📋 智能体执行记录:"
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT
    workflow_run_id,
    agent_name,
    status,
    execution_time_ms,
    created_at
FROM agent_executions
ORDER BY created_at DESC
LIMIT 10;
"

echo ""
echo "✅ 数据库检查完成"
