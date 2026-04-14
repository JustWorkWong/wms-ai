using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using WmsAi.AiGateway.Application.Agents;
using WmsAi.AiGateway.Application.Workflows;

namespace WmsAi.AiGateway.Infrastructure.Workflows.Executors;

/// <summary>
/// 质检决策 Agent Executor - 调用 AI 做出质检决策
/// </summary>
public sealed partial class InspectionDecisionAgentExecutor : Executor
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<InspectionDecisionAgentExecutor> _logger;

    public InspectionDecisionAgentExecutor(
        IChatClient chatClient,
        ILogger<InspectionDecisionAgentExecutor> logger)
        : base("InspectionDecisionAgent")
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder builder)
    {
        return builder;
    }

    [MessageHandler]
    public async Task<QcInspectionState> HandleAsync(
        QcInspectionState state,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "开始质检决策: QcTaskId={QcTaskId}, SKU={SkuCode}",
            state.QcTaskId, state.QcTask?.SkuCode);

        try
        {
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(state);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, userPrompt)
            };

            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions
                {
                    Temperature = WorkflowConstants.InspectionDecisionTemperature,
                    MaxOutputTokens = WorkflowConstants.InspectionDecisionMaxTokens
                },
                cancellationToken);

            var aiContent = response.Text ?? string.Empty;

            _logger.LogInformation(
                "AI 质检决策完成: QcTaskId={QcTaskId}",
                state.QcTaskId);

            var decisionResult = ParseAiResponse(aiContent);

            return state.With(inspectionDecision: decisionResult, status: "DecisionMade");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "质检决策失败: QcTaskId={QcTaskId}",
                state.QcTaskId);

            return state.WithError($"质检决策失败: {ex.Message}");
        }
    }

    private string BuildSystemPrompt()
    {
        return """
            你是一个专业的质检决策专家。你的任务是基于提供的证据和质量规则，对商品质检做出决策。

            决策类型：
            - Accept: 接受，质量合格
            - Reject: 拒绝，质量不合格
            - Conditional: 有条件接受，需要人工复核

            请按照以下 JSON 格式返回决策结果：
            {
              "decision": "Accept/Reject/Conditional",
              "reasoning": "决策推理过程",
              "confidenceScore": 0.0-1.0,
              "issues": [
                {
                  "issueType": "问题类型",
                  "description": "问题描述",
                  "severity": "High/Medium/Low",
                  "evidenceRef": "相关证据ID（可选）"
                }
              ],
              "structuredData": {
                "key": "value"
              }
            }

            注意：
            1. 只返回 JSON，不要有其他文字
            2. confidenceScore < 0.7 时建议使用 Conditional 决策
            3. 如果发现质量问题，必须在 issues 中详细说明
            4. reasoning 要清晰说明决策依据
            """;
    }

    private string BuildUserPrompt(QcInspectionState state)
    {
        var evidenceList = state.Evidence.Select(e =>
            $"- {e.AssetType} (ID: {e.AssetId}, URL: {e.Url})"
        );
        var evidenceStr = string.Join("\n", evidenceList);

        var rulesJson = JsonSerializer.Serialize(
            state.QualityRules,
            new JsonSerializerOptions { WriteIndented = true });

        var gapAnalysis = state.EvidenceGapAnalysis != null
            ? JsonSerializer.Serialize(
                state.EvidenceGapAnalysis,
                new JsonSerializerOptions { WriteIndented = true })
            : "无证据缺口分析";

        return $"""
            质检任务 ID: {state.QcTaskId}
            SKU 编码: {state.QcTask?.SkuCode ?? "N/A"}
            SKU 名称: {state.QcTask?.SkuName ?? "N/A"}
            质检数量: {state.QcTask?.Quantity ?? 0}

            质量规则：
            {rulesJson}

            提供的证据：
            {evidenceStr}

            证据缺口分析：
            {gapAnalysis}

            请基于以上信息做出质检决策。
            """;
    }

    private InspectionDecisionResult ParseAiResponse(string aiContent)
    {
        return AiResponseParser.Parse<InspectionDecisionResult>(aiContent, _logger);
    }
}
