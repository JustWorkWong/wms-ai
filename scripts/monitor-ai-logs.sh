#!/bin/bash

# AI 日志实时监控脚本
# 用途：实时查看 AI 相关的日志输出

echo "📊 开始监控 AI 日志..."
echo "按 Ctrl+C 停止监控"
echo "================================"
echo ""

# 如果使用 Docker Compose
if command -v docker-compose &> /dev/null; then
    docker-compose logs -f ai-gateway | grep -E "(EvidenceGapAgent|InspectionDecisionAgent|OpenAiCompatibleClient|AI)"
# 如果使用 Aspire
elif [ -d "/tmp/aspire-logs" ]; then
    tail -f /tmp/aspire-logs/ai-gateway.log | grep -E "(EvidenceGapAgent|InspectionDecisionAgent|OpenAiCompatibleClient|AI)"
# 否则提示手动查看
else
    echo "⚠️  请在 Aspire Dashboard 中查看 AiGateway 服务的日志"
    echo "或者运行: dotnet run --project src/AppHost/WmsAi.AppHost"
fi
