using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using WmsAi.AiGateway.Application.Agents;

namespace WmsAi.AiGateway.Infrastructure.Agents;

public sealed class InspectionDecisionAgent : IInspectionDecisionAgent
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<InspectionDecisionAgent> _logger;

    public InspectionDecisionAgent(
        IChatClient chatClient,
        ILogger<InspectionDecisionAgent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<InspectionDecisionResult> MakeDecisionAsync(
        InspectionDecisionContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Making inspection decision for QcTask {QcTaskId}, SKU {SkuCode}",
            context.QcTaskId, context.SkuCode);

        try
        {
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(context);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, userPrompt)
            };

            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions
                {
                    Temperature = 0.2f,
                    MaxOutputTokens = 1500
                },
                cancellationToken);

            var aiContent = response.Text ?? string.Empty;

            _logger.LogInformation("AI decision response received");

            return ParseAiResponse(aiContent, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to make AI decision, falling back to rule-based decision");
            return FallbackDecision(context);
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

    private string BuildUserPrompt(InspectionDecisionContext context)
    {
        var evidenceList = context.Evidence.Select(e =>
            $"- {e.AssetType} (ID: {e.AssetId}, URL: {e.Url})"
        );
        var evidenceStr = string.Join("\n", evidenceList);

        return $"""
            质检任务 ID: {context.QcTaskId}
            SKU 编码: {context.SkuCode}

            质量规则：
            {JsonSerializer.Serialize(context.QualityRules, new JsonSerializerOptions { WriteIndented = true })}

            提供的证据：
            {evidenceStr}

            操作记录：
            {JsonSerializer.Serialize(context.OperationRecords, new JsonSerializerOptions { WriteIndented = true })}

            请基于以上信息做出质检决策。
            """;
    }

    private InspectionDecisionResult ParseAiResponse(string aiContent, InspectionDecisionContext context)
    {
        try
        {
            // 尝试提取 JSON（AI 可能返回 markdown 代码块）
            var jsonContent = aiContent.Trim();
            if (jsonContent.StartsWith("```json"))
            {
                jsonContent = jsonContent.Replace("```json", "").Replace("```", "").Trim();
            }
            else if (jsonContent.StartsWith("```"))
            {
                jsonContent = jsonContent.Replace("```", "").Trim();
            }

            var result = JsonSerializer.Deserialize<InspectionDecisionResult>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result != null)
            {
                return result;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response as JSON: {Content}", aiContent);
        }

        // 如果解析失败，回退到规则决策
        return FallbackDecision(context);
    }

    private InspectionDecisionResult FallbackDecision(InspectionDecisionContext context)
    {
        _logger.LogInformation("Using fallback rule-based decision");

        var hasEvidence = context.Evidence.Count > 0;
        var decision = hasEvidence ? "Accept" : "Reject";
        var confidence = hasEvidence ? 0.92m : 0.65m;

        var issues = new List<QualityIssue>();
        if (!hasEvidence)
        {
            issues.Add(new QualityIssue
            {
                IssueType = "MissingEvidence",
                Description = "No evidence provided for quality inspection",
                Severity = "High",
                EvidenceRef = null
            });
        }

        return new InspectionDecisionResult
        {
            Decision = decision,
            Reasoning = hasEvidence
                ? "All quality checks passed based on provided evidence"
                : "Insufficient evidence to make acceptance decision",
            ConfidenceScore = confidence,
            Issues = issues,
            StructuredData = new Dictionary<string, object>
            {
                ["evidenceCount"] = context.Evidence.Count,
                ["rulesApplied"] = context.QualityRules.Count,
                ["fallbackUsed"] = true
            }
        };
    }
}
