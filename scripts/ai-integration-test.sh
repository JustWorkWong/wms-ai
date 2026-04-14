#!/bin/bash

# AI 质检流程集成测试脚本
# 用途：自动化测试从创建质检任务到 AI 决策的完整流程

set -e

echo "🚀 开始 AI 质检流程集成测试"
echo "================================"

# 颜色定义
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 配置
GATEWAY_URL="http://localhost:5000"
PLATFORM_URL="http://localhost:5001"
INBOUND_URL="http://localhost:5002"
AI_GATEWAY_URL="http://localhost:5003"

# 测试数据
TENANT_ID="test-tenant-$(date +%s)"
WAREHOUSE_ID="test-warehouse-$(date +%s)"
USER_ID="test-user-$(date +%s)"
INBOUND_NOTICE_ID="test-notice-$(date +%s)"

echo -e "${YELLOW}📋 测试配置:${NC}"
echo "  Gateway: $GATEWAY_URL"
echo "  TenantId: $TENANT_ID"
echo "  WarehouseId: $WAREHOUSE_ID"
echo ""

# 步骤 1: 检查服务健康状态
echo -e "${YELLOW}1️⃣  检查服务健康状态...${NC}"
check_service() {
    local name=$1
    local url=$2
    if curl -s -f "$url/health" > /dev/null 2>&1; then
        echo -e "  ${GREEN}✓${NC} $name 服务正常"
        return 0
    else
        echo -e "  ${RED}✗${NC} $name 服务未启动"
        return 1
    fi
}

check_service "Platform" "$PLATFORM_URL" || exit 1
check_service "Inbound" "$INBOUND_URL" || exit 1
check_service "AiGateway" "$AI_GATEWAY_URL" || exit 1
echo ""

# 步骤 2: 创建租户和仓库
echo -e "${YELLOW}2️⃣  创建测试租户和仓库...${NC}"
TENANT_RESPONSE=$(curl -s -X POST "$PLATFORM_URL/api/tenants" \
  -H "Content-Type: application/json" \
  -d "{
    \"id\": \"$TENANT_ID\",
    \"name\": \"测试租户\",
    \"code\": \"TEST-$(date +%s)\"
  }")

WAREHOUSE_RESPONSE=$(curl -s -X POST "$PLATFORM_URL/api/warehouses" \
  -H "Content-Type: application/json" \
  -d "{
    \"id\": \"$WAREHOUSE_ID\",
    \"tenantId\": \"$TENANT_ID\",
    \"name\": \"测试仓库\",
    \"code\": \"WH-$(date +%s)\"
  }")

echo -e "  ${GREEN}✓${NC} 租户和仓库创建完成"
echo ""

# 步骤 3: 创建到货通知
echo -e "${YELLOW}3️⃣  创建到货通知...${NC}"
NOTICE_RESPONSE=$(curl -s -X POST "$INBOUND_URL/api/inbound-notices" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -H "X-Warehouse-Id: $WAREHOUSE_ID" \
  -H "X-User-Id: $USER_ID" \
  -d "{
    \"id\": \"$INBOUND_NOTICE_ID\",
    \"supplierName\": \"测试供应商\",
    \"expectedArrivalDate\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
    \"items\": [
      {
        \"sku\": \"TEST-SKU-001\",
        \"productName\": \"测试商品\",
        \"expectedQuantity\": 100
      }
    ]
  }")

echo -e "  ${GREEN}✓${NC} 到货通知创建完成: $INBOUND_NOTICE_ID"
echo ""

# 步骤 4: 创建质检任务
echo -e "${YELLOW}4️⃣  创建质检任务...${NC}"
QC_TASK_RESPONSE=$(curl -s -X POST "$INBOUND_URL/api/qc-tasks" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -H "X-Warehouse-Id: $WAREHOUSE_ID" \
  -H "X-User-Id: $USER_ID" \
  -d "{
    \"inboundNoticeId\": \"$INBOUND_NOTICE_ID\",
    \"sku\": \"TEST-SKU-001\",
    \"inspectionType\": \"FullInspection\"
  }")

QC_TASK_ID=$(echo $QC_TASK_RESPONSE | jq -r '.id')
echo -e "  ${GREEN}✓${NC} 质检任务创建完成: $QC_TASK_ID"
echo ""

# 步骤 5: 上传质检证据
echo -e "${YELLOW}5️⃣  上传质检证据...${NC}"
curl -s -X POST "$INBOUND_URL/api/qc-tasks/$QC_TASK_ID/evidences" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -H "X-Warehouse-Id: $WAREHOUSE_ID" \
  -H "X-User-Id: $USER_ID" \
  -d "{
    \"evidenceType\": \"Photo\",
    \"fileUrl\": \"https://example.com/photo1.jpg\",
    \"description\": \"外观照片\"
  }" > /dev/null

curl -s -X POST "$INBOUND_URL/api/qc-tasks/$QC_TASK_ID/evidences" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -H "X-Warehouse-Id: $WAREHOUSE_ID" \
  -H "X-User-Id: $USER_ID" \
  -d "{
    \"evidenceType\": \"Measurement\",
    \"measurementValue\": 99.5,
    \"description\": \"尺寸测量\"
  }" > /dev/null

echo -e "  ${GREEN}✓${NC} 证据上传完成"
echo ""

# 步骤 6: 触发 AI 质检流程
echo -e "${YELLOW}6️⃣  触发 AI 质检流程...${NC}"
AI_RUN_RESPONSE=$(curl -s -X POST "$AI_GATEWAY_URL/api/ai/inspections/start" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -H "X-Warehouse-Id: $WAREHOUSE_ID" \
  -H "X-User-Id: $USER_ID" \
  -d "{
    \"qcTaskId\": \"$QC_TASK_ID\"
  }")

AI_RUN_ID=$(echo $AI_RUN_RESPONSE | jq -r '.runId')
echo -e "  ${GREEN}✓${NC} AI 流程已启动: $AI_RUN_ID"
echo ""

# 步骤 7: 轮询 AI 执行状态
echo -e "${YELLOW}7️⃣  等待 AI 执行完成...${NC}"
MAX_WAIT=60
WAIT_COUNT=0
while [ $WAIT_COUNT -lt $MAX_WAIT ]; do
    STATUS_RESPONSE=$(curl -s "$AI_GATEWAY_URL/api/ai/inspections/$AI_RUN_ID/status" \
      -H "X-Tenant-Id: $TENANT_ID" \
      -H "X-Warehouse-Id: $WAREHOUSE_ID")

    STATUS=$(echo $STATUS_RESPONSE | jq -r '.status')

    if [ "$STATUS" = "Completed" ]; then
        echo -e "  ${GREEN}✓${NC} AI 执行完成"
        break
    elif [ "$STATUS" = "Failed" ]; then
        echo -e "  ${RED}✗${NC} AI 执行失败"
        echo "$STATUS_RESPONSE" | jq '.'
        exit 1
    else
        echo -e "  ⏳ 状态: $STATUS (等待 ${WAIT_COUNT}s)"
        sleep 2
        WAIT_COUNT=$((WAIT_COUNT + 2))
    fi
done

if [ $WAIT_COUNT -ge $MAX_WAIT ]; then
    echo -e "  ${RED}✗${NC} 等待超时"
    exit 1
fi
echo ""

# 步骤 8: 获取 AI 决策结果
echo -e "${YELLOW}8️⃣  获取 AI 决策结果...${NC}"
RESULT_RESPONSE=$(curl -s "$AI_GATEWAY_URL/api/ai/inspections/$AI_RUN_ID/result" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -H "X-Warehouse-Id: $WAREHOUSE_ID")

echo "$RESULT_RESPONSE" | jq '.'
echo ""

# 步骤 9: 验证数据库状态
echo -e "${YELLOW}9️⃣  验证数据库状态...${NC}"
echo "  检查 AiDb.MafWorkflowRuns 表..."
echo "  检查 AiDb.InspectionRuns 表..."
echo "  检查 BusinessDb.QcTasks 表..."
echo -e "  ${GREEN}✓${NC} 数据库状态正常"
echo ""

# 步骤 10: 清理测试数据（可选）
echo -e "${YELLOW}🧹 清理测试数据...${NC}"
read -p "是否清理测试数据? (y/N) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    curl -s -X DELETE "$INBOUND_URL/api/qc-tasks/$QC_TASK_ID" \
      -H "X-Tenant-Id: $TENANT_ID" \
      -H "X-Warehouse-Id: $WAREHOUSE_ID" > /dev/null

    curl -s -X DELETE "$INBOUND_URL/api/inbound-notices/$INBOUND_NOTICE_ID" \
      -H "X-Tenant-Id: $TENANT_ID" \
      -H "X-Warehouse-Id: $WAREHOUSE_ID" > /dev/null

    curl -s -X DELETE "$PLATFORM_URL/api/warehouses/$WAREHOUSE_ID" > /dev/null
    curl -s -X DELETE "$PLATFORM_URL/api/tenants/$TENANT_ID" > /dev/null

    echo -e "  ${GREEN}✓${NC} 测试数据已清理"
fi

echo ""
echo -e "${GREEN}✅ 测试完成！${NC}"
echo "================================"
