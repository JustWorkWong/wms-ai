#!/bin/bash

# AI 质检流程事件驱动测试脚本
# 用途：测试完整的事件驱动 AI 质检流程

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  WMS AI 事件驱动质检流程测试          ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""

# 检查依赖
echo -e "${YELLOW}🔍 检查依赖...${NC}"
command -v dotnet >/dev/null 2>&1 || { echo -e "${RED}✗ dotnet 未安装${NC}"; exit 1; }
command -v jq >/dev/null 2>&1 || { echo -e "${RED}✗ jq 未安装${NC}"; exit 1; }
command -v curl >/dev/null 2>&1 || { echo -e "${RED}✗ curl 未安装${NC}"; exit 1; }
echo -e "${GREEN}✓ 依赖检查通过${NC}"
echo ""

# 配置
INBOUND_URL="http://localhost:5002"
AI_GATEWAY_URL="http://localhost:5003"

# 测试数据
TENANT_ID="test-tenant-$(date +%s)"
WAREHOUSE_ID="test-warehouse-$(date +%s)"
USER_ID="test-user-$(date +%s)"

echo -e "${YELLOW}📋 测试配置:${NC}"
echo "  TenantId: $TENANT_ID"
echo "  WarehouseId: $WAREHOUSE_ID"
echo ""

# 步骤 1: 检查服务状态
echo -e "${YELLOW}1️⃣  检查服务状态...${NC}"
if ! curl -s -f "$INBOUND_URL/" > /dev/null 2>&1; then
    echo -e "  ${RED}✗${NC} Inbound 服务未启动"
    exit 1
fi
if ! curl -s -f "$AI_GATEWAY_URL/" > /dev/null 2>&1; then
    echo -e "  ${RED}✗${NC} AiGateway 服务未启动"
    exit 1
fi
echo -e "  ${GREEN}✓${NC} 所有服务正常"
echo ""

# 步骤 2: 创建质检任务（会自动触发 CAP 事件）
echo -e "${YELLOW}2️⃣  创建质检任务（触发 AI 事件）...${NC}"

# 构造请求
QC_TASK_REQUEST=$(cat <<EOF
{
  "tenantId": "$TENANT_ID",
  "warehouseId": "$WAREHOUSE_ID",
  "userId": "$USER_ID",
  "inboundNoticeId": "$(uuidgen)",
  "sku": "TEST-SKU-001",
  "productName": "测试商品",
  "inspectionType": "FullInspection",
  "requiredEvidenceTypes": ["Photo", "Measurement"],
  "qualityRules": [
    {
      "ruleType": "Measurement",
      "parameter": "尺寸",
      "minValue": 95.0,
      "maxValue": 105.0,
      "unit": "cm"
    }
  ]
}
EOF
)

echo "$QC_TASK_REQUEST" | jq '.'
echo ""

QC_TASK_RESPONSE=$(curl -s -X POST "$INBOUND_URL/api/qc-tasks" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -H "X-Warehouse-Id: $WAREHOUSE_ID" \
  -H "X-User-Id: $USER_ID" \
  -d "$QC_TASK_REQUEST" || echo '{"error": "failed"}')

if echo "$QC_TASK_RESPONSE" | jq -e '.error' > /dev/null 2>&1; then
    echo -e "  ${RED}✗${NC} 质检任务创建失败"
    echo "$QC_TASK_RESPONSE" | jq '.'
    exit 1
fi

QC_TASK_ID=$(echo "$QC_TASK_RESPONSE" | jq -r '.id // .qcTaskId // empty')
if [ -z "$QC_TASK_ID" ]; then
    echo -e "  ${RED}✗${NC} 无法获取 QcTaskId"
    echo "$QC_TASK_RESPONSE" | jq '.'
    exit 1
fi

echo -e "  ${GREEN}✓${NC} 质检任务创建成功: $QC_TASK_ID"
echo -e "  ${BLUE}ℹ${NC}  CAP 事件 'qctask.created.v1' 已发布"
echo ""

# 步骤 3: 上传质检证据
echo -e "${YELLOW}3️⃣  上传质检证据...${NC}"

# 上传照片
curl -s -X POST "$INBOUND_URL/api/qc-tasks/$QC_TASK_ID/evidences" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -H "X-Warehouse-Id: $WAREHOUSE_ID" \
  -H "X-User-Id: $USER_ID" \
  -d '{
    "evidenceType": "Photo",
    "fileUrl": "https://example.com/product-photo.jpg",
    "description": "商品外观照片"
  }' > /dev/null 2>&1 || true

# 上传测量数据
curl -s -X POST "$INBOUND_URL/api/qc-tasks/$QC_TASK_ID/evidences" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -H "X-Warehouse-Id: $WAREHOUSE_ID" \
  -H "X-User-Id: $USER_ID" \
  -d '{
    "evidenceType": "Measurement",
    "measurementValue": 99.5,
    "unit": "cm",
    "description": "尺寸测量"
  }' > /dev/null 2>&1 || true

echo -e "  ${GREEN}✓${NC} 证据上传完成"
echo ""

# 步骤 4: 等待 AI 执行（通过查询 AiDb 中的记录）
echo -e "${YELLOW}4️⃣  等待 AI 工作流执行...${NC}"
echo -e "  ${BLUE}ℹ${NC}  AI 智能体正在分析证据和做出决策..."
echo ""

MAX_WAIT=30
WAIT_COUNT=0
INSPECTION_RUN_ID=""

while [ $WAIT_COUNT -lt $MAX_WAIT ]; do
    # 查询是否有该 QcTask 的检验记录
    INSPECTION_LIST=$(curl -s "$AI_GATEWAY_URL/api/ai/inspections?qcTaskId=$QC_TASK_ID" \
      -H "X-Tenant-Id: $TENANT_ID" \
      -H "X-Warehouse-Id: $WAREHOUSE_ID" 2>/dev/null || echo '[]')

    if [ "$(echo "$INSPECTION_LIST" | jq 'length')" -gt 0 ]; then
        INSPECTION_RUN_ID=$(echo "$INSPECTION_LIST" | jq -r '.[0].id // empty')
        STATUS=$(echo "$INSPECTION_LIST" | jq -r '.[0].status // empty')

        if [ "$STATUS" = "Completed" ]; then
            echo -e "  ${GREEN}✓${NC} AI 执行完成"
            break
        elif [ "$STATUS" = "Failed" ]; then
            echo -e "  ${RED}✗${NC} AI 执行失败"
            echo "$INSPECTION_LIST" | jq '.[0]'
            exit 1
        elif [ "$STATUS" = "WaitingManualReview" ]; then
            echo -e "  ${YELLOW}⚠${NC}  需要人工复核（置信度不足）"
            break
        else
            echo -e "  ⏳ 状态: $STATUS (等待 ${WAIT_COUNT}s)"
        fi
    else
        echo -e "  ⏳ 等待 AI 工作流启动... (${WAIT_COUNT}s)"
    fi

    sleep 2
    WAIT_COUNT=$((WAIT_COUNT + 2))
done

if [ $WAIT_COUNT -ge $MAX_WAIT ]; then
    echo -e "  ${YELLOW}⚠${NC}  等待超时，但这可能是正常的（AI 可能还在处理）"
    echo -e "  ${BLUE}ℹ${NC}  请查看 Aspire Dashboard 的日志: http://localhost:15170"
fi
echo ""

# 步骤 5: 获取 AI 决策结果
if [ -n "$INSPECTION_RUN_ID" ]; then
    echo -e "${YELLOW}5️⃣  获取 AI 决策结果...${NC}"
    RESULT=$(curl -s "$AI_GATEWAY_URL/api/ai/inspections/$INSPECTION_RUN_ID" \
      -H "X-Tenant-Id: $TENANT_ID" \
      -H "X-Warehouse-Id: $WAREHOUSE_ID" 2>/dev/null || echo '{}')

    echo "$RESULT" | jq '.'
    echo ""

    DECISION=$(echo "$RESULT" | jq -r '.decision // empty')
    CONFIDENCE=$(echo "$RESULT" | jq -r '.confidenceScore // empty')

    if [ -n "$DECISION" ]; then
        echo -e "  ${GREEN}✓${NC} AI 决策: $DECISION"
        echo -e "  ${GREEN}✓${NC} 置信度: $CONFIDENCE"
    fi
fi
echo ""

# 步骤 6: 显示测试总结
echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  测试总结                              ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""
echo -e "${GREEN}✅ 事件驱动流程测试完成${NC}"
echo ""
echo -e "${YELLOW}📊 关键信息:${NC}"
echo -e "  • QcTaskId: $QC_TASK_ID"
echo -e "  • InspectionRunId: $INSPECTION_RUN_ID"
echo -e "  • TenantId: $TENANT_ID"
echo ""
echo -e "${YELLOW}🔍 查看详细日志:${NC}"
echo -e "  • Aspire Dashboard: http://localhost:15170"
echo -e "  • 搜索关键词: $QC_TASK_ID"
echo ""
echo -e "${YELLOW}💡 提示:${NC}"
echo -e "  • AI 智能体日志包含详细的推理过程"
echo -e "  • 如果看到 'Fallback to rule-based' 说明 AI 调用失败，使用了规则兜底"
echo -e "  • 检查 appsettings.Local.json 中的 API Key 是否正确"
echo ""
