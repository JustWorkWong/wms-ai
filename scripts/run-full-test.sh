#!/bin/bash

# AI 质检流程完整测试主控脚本
# 用途：一键启动所有服务、执行测试、监控日志、检查数据库

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  WMS AI 质检流程完整测试套件          ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""

# 检查依赖
echo -e "${YELLOW}🔍 检查依赖...${NC}"
command -v dotnet >/dev/null 2>&1 || { echo -e "${RED}✗ dotnet 未安装${NC}"; exit 1; }
command -v psql >/dev/null 2>&1 || { echo -e "${RED}✗ psql 未安装${NC}"; exit 1; }
command -v jq >/dev/null 2>&1 || { echo -e "${RED}✗ jq 未安装${NC}"; exit 1; }
echo -e "${GREEN}✓ 依赖检查通过${NC}"
echo ""

# 步骤 1: 启动后端服务
echo -e "${YELLOW}1️⃣  启动后端服务 (Aspire)...${NC}"
cd src/AppHost/WmsAi.AppHost

# 后台启动 Aspire
dotnet run > /tmp/aspire-output.log 2>&1 &
ASPIRE_PID=$!
echo -e "  ${GREEN}✓${NC} Aspire 已启动 (PID: $ASPIRE_PID)"

# 等待服务就绪
echo -e "  ⏳ 等待服务启动..."
sleep 15

# 从日志中提取 Dashboard URL
DASHBOARD_URL=$(grep -oP 'https?://[^:]+:\d+' /tmp/aspire-output.log | head -1)
if [ -n "$DASHBOARD_URL" ]; then
    echo -e "  ${GREEN}✓${NC} Aspire Dashboard: $DASHBOARD_URL"
else
    echo -e "  ${YELLOW}⚠${NC}  无法获取 Dashboard URL，请手动检查"
fi

cd ../../..
echo ""

# 步骤 2: 启动前端服务
echo -e "${YELLOW}2️⃣  启动前端服务...${NC}"
cd web/wms-ai-web

if [ ! -d "node_modules" ]; then
    echo -e "  ⏳ 安装依赖..."
    npm install > /dev/null 2>&1
fi

npm run dev > /tmp/frontend-output.log 2>&1 &
FRONTEND_PID=$!
echo -e "  ${GREEN}✓${NC} 前端已启动 (PID: $FRONTEND_PID)"
echo -e "  ${GREEN}✓${NC} 前端地址: http://localhost:5173"

cd ../..
echo ""

# 步骤 3: 等待所有服务就绪
echo -e "${YELLOW}3️⃣  等待所有服务就绪...${NC}"
sleep 10

# 健康检查
check_health() {
    local name=$1
    local url=$2
    local max_retry=10
    local retry=0

    while [ $retry -lt $max_retry ]; do
        if curl -s -f "$url/health" > /dev/null 2>&1; then
            echo -e "  ${GREEN}✓${NC} $name 服务就绪"
            return 0
        fi
        retry=$((retry + 1))
        sleep 2
    done

    echo -e "  ${RED}✗${NC} $name 服务未就绪"
    return 1
}

check_health "Platform" "http://localhost:5001"
check_health "Inbound" "http://localhost:5002"
check_health "AiGateway" "http://localhost:5003"
echo ""

# 步骤 4: 启动日志监控（后台）
echo -e "${YELLOW}4️⃣  启动日志监控...${NC}"
./scripts/monitor-ai-logs.sh > /tmp/ai-logs.log 2>&1 &
MONITOR_PID=$!
echo -e "  ${GREEN}✓${NC} 日志监控已启动 (PID: $MONITOR_PID)"
echo -e "  ${BLUE}ℹ${NC}  查看日志: tail -f /tmp/ai-logs.log"
echo ""

# 步骤 5: 执行集成测试
echo -e "${YELLOW}5️⃣  执行 AI 质检流程集成测试...${NC}"
./scripts/ai-integration-test.sh

# 步骤 6: 检查数据库
echo -e "${YELLOW}6️⃣  检查数据库状态...${NC}"
./scripts/check-ai-db.sh

# 步骤 7: 显示测试报告
echo ""
echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  测试报告                              ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""
echo -e "${GREEN}✅ 所有测试通过！${NC}"
echo ""
echo -e "${YELLOW}📊 服务状态:${NC}"
echo -e "  • Aspire Dashboard: $DASHBOARD_URL"
echo -e "  • 前端: http://localhost:5173"
echo -e "  • Platform API: http://localhost:5001"
echo -e "  • Inbound API: http://localhost:5002"
echo -e "  • AiGateway API: http://localhost:5003"
echo ""
echo -e "${YELLOW}📋 日志文件:${NC}"
echo -e "  • Aspire: /tmp/aspire-output.log"
echo -e "  • 前端: /tmp/frontend-output.log"
echo -e "  • AI 日志: /tmp/ai-logs.log"
echo ""
echo -e "${YELLOW}🛠️  后续操作:${NC}"
echo -e "  • 查看 AI 日志: tail -f /tmp/ai-logs.log"
echo -e "  • 查看数据库: ./scripts/check-ai-db.sh"
echo -e "  • 停止所有服务: kill $ASPIRE_PID $FRONTEND_PID $MONITOR_PID"
echo ""

# 询问是否保持运行
read -p "是否保持服务运行? (Y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Nn]$ ]]; then
    echo -e "${GREEN}✓${NC} 服务保持运行，按 Ctrl+C 停止"
    wait
else
    echo -e "${YELLOW}🛑 停止所有服务...${NC}"
    kill $ASPIRE_PID $FRONTEND_PID $MONITOR_PID 2>/dev/null || true
    echo -e "${GREEN}✓${NC} 所有服务已停止"
fi
