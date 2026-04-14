using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using WmsAi.AiGateway.Application.Agents;
using WmsAi.AiGateway.Application.Workflows;

namespace WmsAi.AiGateway.Infrastructure.Workflows.Executors;

/// <summary>
/// 证据缺口分析 Agent Executor - 调用 AI 分析证据完整性
/// </summary>
public sealed partial class EvidenceGapAgentExecutor : Executor
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<EvidenceGapAgentExecutor> _logger;

    public EvidenceGapAgentExecutor(
        IChatClient chatClient,
        ILogger<EvidenceGapAgentExecutor> logger)
        : base("EvidenceGapAgent")
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
            "开始证据缺口分析: QcTaskId={QcTaskId}, TenantId={TenantId}",
            state.QcTaskId, state.TenantId);

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
                    Temperature = 0.3f,
                    MaxOutputTokens = 1000
                },
                cancellationToken);

            var aiContent = response.Text ?? string.Empty;

            _logger.LogInformation(
                "AI 证据分析完成: QcTaskId={QcTaskId}",
                state.QcTaskId);

            var analysisResult = ParseAiResponse(aiContent);

            return new QcInspectionState
            {
                QcTaskId = state.QcTaskId,
                TenantId = state.TenantId,
                WarehouseId = state.WarehouseId,
                UserId = state.UserId,
                WorkflowRunId = state.WorkflowRunId,
                QcTask = state.QcTask,
                Evidence = state.Evidence,
                QualityRules = state.QualityRules,
                EvidenceGapAnalysis = analysisResult,
                Status = "EvidenceAnalyzed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "证据缺口分析失败: QcTaskId={QcTaskId}",
                state.QcTaskId);

            return new QcInspectionState
            {
                QcTaskId = state.QcTaskId,
                TenantId = state.TenantId,
                WarehouseId = state.WarehouseId,
                UserId = state.UserId,
                WorkflowRunId = state.WorkflowRunId,
                QcTask = state.QcTask,
                Evidence = state.Evidence,
                QualityRules = state.QualityRules,
                Status = "Failed",
                ErrorMessage = $"证据缺口分析失败: {ex.Message}"
            };
        }
    }

    private string BuildSystemPrompt()
    {
        return """
            你是一个质检证据分析专家。你的任务是分析当前提供的质检证据是否完整。

            请按照以下 JSON 格式返回分析结果：
            {
              "isComplete": true/false,
              "gaps": [
                {
                  "evidenceType": "证据类型",
                  "reason": "缺失原因",
                  "severity": "High/Medium/Low"
                }
              ],
              "reasoning": "分析推理过程",
              "confidenceScore": 0.0-1.0
            }

            注意：
            1. 只返回 JSON，不要有其他文字
            2. confidenceScore 表示你对分析结果的置信度
            3. 如果所有必需证据都已提供，isComplete 为 true，gaps 为空数组
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

        return $"""
            质检任务 ID: {state.QcTaskId}
            SKU 编码: {state.QcTask?.SkuCode ?? "N/A"}
            SKU 名称: {state.QcTask?.SkuName ?? "N/A"}

            质量规则：
            {rulesJson}

            当前已提供的证据：
            {evidenceStr}

            请分析当前证据是否完整，并指出缺失的证据类型。
            """;
    }

    private EvidenceGapResult ParseAiResponse(string aiContent)
    {
        try
        {
            // 提取 JSON（处理 markdown 代码块）
            var jsonContent = aiContent.Trim();
            if (jsonContent.StartsWith("```json"))
            {
                jsonContent = jsonContent.Replace("```json", "").Replace("```", "").Trim();
            }
            else if (jsonContent.StartsWith("```"))
            {
                jsonContent = jsonContent.Replace("```", "").Trim();
            }

            var result = JsonSerializer.Deserialize<EvidenceGapResult>(
                jsonContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result == null)
            {
                throw new InvalidOperationException("AI 返回的 JSON 解析为 null");
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "解析 AI 响应失败: {Content}",
                aiContent);

            throw new InvalidOperationException(
                $"无法解析 AI 响应为 EvidenceGapResult: {ex.Message}",
                ex);
        }
    }
}
