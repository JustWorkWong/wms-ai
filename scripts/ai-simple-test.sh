#!/bin/bash

# AI 质检流程简化测试脚本
# 用途：不依赖 psql，只测试 API 和日志

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  WMS AI 质检流程简化测试              ║${NC}"
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
GATEWAY_URL="http://localhost:5000"
PLATFORM_URL="http://localhost:5001"
INBOUND_URL="http://localhost:5002"
AI_GATEWAY_URL="http://localhost:5003"

# 步骤 1: 检查后端是否已启动
echo -e "${YELLOW}1️⃣  检查后端服务状态...${NC}"

check_service() {
    local name=$1
    local url=$2
    # 尝试访问根路径或健康检查端点
    if curl -s -f "$url/health" > /dev/null 2>&1 || curl -s -f "$url/" > /dev/null 2>&1; then
        echo -e "  ${GREEN}✓${NC} $name 服务正常"
        return 0
    else
        echo -e "  ${RED}✗${NC} $name 服务未启动"
        return 1
    fi
}

# 如果服务未启动，提示用户启动
if ! check_service "Platform" "$PLATFORM_URL" || \
   ! check_service "Inbound" "$INBOUND_URL" || \
   ! check_service "AiGateway" "$AI_GATEWAY_URL"; then
    echo ""
    echo -e "${YELLOW}⚠️  请先启动后端服务:${NC}"
    echo -e "  cd src/AppHost/WmsAi.AppHost"
    echo -e "  dotnet run"
    echo ""
    exit 1
fi
echo ""

# 步骤 2: 创建测试数据
echo -e "${YELLOW}2️⃣  创建测试数据...${NC}"

TENANT_ID="test-tenant-$(date +%s)"
WAREHOUSE_ID="test-warehouse-$(date +%s)"
USER_ID="test-user-$(date +%s)"
INBOUND_NOTICE_ID="test-notice-$(date +%s)"

echo -e "  TenantId: $TENANT_ID"
echo -e "  WarehouseId: $WAREHOUSE_ID"
echo ""

# 创建租户
echo -e "  📝 创建租户..."
TENANT_RESPONSE=$(curl -s -X POST "$PLATFORM_URL/api/tenants" \
  -H "Content-Type: application/json" \
  -d "{
    \"id\": \"$TENANT_ID\",
    \"name\": \"测试租户\",
    \"code\": \"TEST-$(date +%s)\"
  }" || echo '{"error": "failed"}')

if echo "$TENANT_RESPONSE" | jq -e '.error' > /dev/null 2>&1; then
    echo -e "  ${RED}✗${NC} 租户创建失败"
    echo "$TENANT_RESPONSE" | jq '.'
    exit 1
fi
echo -e "  ${GREEN}✓${NC} 租户创建成功"

# 创建仓库
echo -e "  📝 创建仓库..."
WAREHOUSE_RESPONSE=$(curl -s -X POST "$PLATFORM_URL/api/warehouses" \
  -H "Content-Type: application/json" \
  -d "{
    \"id\": \"$WAREHOUSE_ID\",
    \"tenantId\": \"$TENANT_ID\",
    \"name\": \"测试仓库\",
    \"code\": \"WH-$(date +%s)\"
  }" || echo '{"error": "failed"}')

if echo "$WAREHOUSE_RESPONSE" | jq -e '.error' > /dev/null 2>&1; then
    echo -e "  ${RED}✗${NC} 仓库创建失败"
    echo "$WAREHOUSE_RESPONSE" | jq '.'
    exit 1
fi
echo -e "  ${GREEN}✓${NC} 仓库创建成功"
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
  }" || echo '{"error": "failed"}')

if echo "$NOTICE_RESPONSE" | jq -e '.error' > /dev/null 2>&1; then
    echo -e "  ${RED}✗${NC} 到货通知创建失败"
    echo "$NOTICE_RESPONSE" | jq '.'
    exit 1
fi
echo -e "  ${GREEN}✓${NC} 到货通知创建成功: $INBOUND_NOTICE_ID"
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
  }" || echo '{"error": "failed"}')

if echo "$QC_TASK_RESPONSE" | jq -e '.error' > /dev/null 2>&1; then
    echo -e "  ${RED}✗${NC} 质检任务创建失败"
    echo "$QC_TASK_RESPONSE" | jq '.'
    exit 1
fi

QC_TASK_ID=$(echo $QC_TASK_RESPONSE | jq -r '.id')
echo -e "  ${GREEN}✓${NC} 质检任务创建成功: $QC_TASK_ID"
echo ""

# 步骤 5: 上传质检证据
echo -e "${YELLOW}5️⃣  上传质检证据...${NC}"

# 上传照片证据
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

# 上传测量证据
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

echo -e "  ${GREEN}✓${NC} 证据上传完成（照片 + 测量）"
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
  }" || echo '{"error": "failed"}')

if echo "$AI_RUN_RESPONSE" | jq -e '.error' > /dev/null 2>&1; then
    echo -e "  ${RED}✗${NC} AI 流程启动失败"
    echo "$AI_RUN_RESPONSE" | jq '.'
    exit 1
fi

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
      -H "X-Warehouse-Id: $WAREHOUSE_ID" || echo '{"status": "Unknown"}')

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

# 提取关键信息
DECISION=$(echo $RESULT_RESPONSE | jq -r '.decision')
CONFIDENCE=$(echo $RESULT_RESPONSE | jq -r '.confidenceScore')

echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  测试结果                              ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""
echo -e "  决策: ${GREEN}$DECISION${NC}"
echo -e "  置信度: ${GREEN}$CONFIDENCE${NC}"
echo ""

# 步骤 9: 清理测试数据（可选）
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
echo ""
echo -e "${YELLOW}📋 下一步:${NC}"
echo -e "  • 查看 Aspire Dashboard 中的日志"
echo -e "  • 检查 AiGateway 服务的详细日志"
echo -e "  • 验证数据库中的记录"
echo ""
