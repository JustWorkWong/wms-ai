using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using WmsAi.AiGateway.Application.Agents;

namespace WmsAi.AiGateway.Infrastructure.Agents;

public sealed class EvidenceGapAgent : IEvidenceGapAgent
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<EvidenceGapAgent> _logger;

    public EvidenceGapAgent(
        IChatClient chatClient,
        ILogger<EvidenceGapAgent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<EvidenceGapResult> AnalyzeEvidenceAsync(
        EvidenceGapContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Analyzing evidence gaps for QcTask {QcTaskId}, tenant {TenantId}",
            context.QcTaskId, context.TenantId);

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
                    Temperature = 0.3f,
                    MaxOutputTokens = 1000
                },
                cancellationToken);

            var aiContent = response.Text ?? string.Empty;

            _logger.LogInformation("AI response received");

            return ParseAiResponse(aiContent, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze evidence gaps, falling back to rule-based analysis");
            return FallbackAnalysis(context);
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

    private string BuildUserPrompt(EvidenceGapContext context)
    {
        var requiredTypes = string.Join(", ", context.RequiredEvidenceTypes);
        var currentEvidence = context.CurrentEvidence.Select(e =>
            $"- {e.EvidenceType} (ID: {e.EvidenceId}, Status: {e.Status})"
        );
        var currentEvidenceStr = string.Join("\n", currentEvidence);

        return $"""
            质检任务 ID: {context.QcTaskId}

            必需的证据类型：
            {requiredTypes}

            当前已提供的证据：
            {currentEvidenceStr}

            质量规则：
            {JsonSerializer.Serialize(context.QualityRules)}

            请分析当前证据是否完整，并指出缺失的证据类型。
            """;
    }

    private EvidenceGapResult ParseAiResponse(string aiContent, EvidenceGapContext context)
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

            var result = JsonSerializer.Deserialize<EvidenceGapResult>(jsonContent, new JsonSerializerOptions
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

        // 如果解析失败，回退到规则分析
        return FallbackAnalysis(context);
    }

    private EvidenceGapResult FallbackAnalysis(EvidenceGapContext context)
    {
        _logger.LogInformation("Using fallback rule-based analysis");

        var requiredTypes = context.RequiredEvidenceTypes.ToHashSet();
        var currentTypes = context.CurrentEvidence.Select(e => e.EvidenceType).ToHashSet();
        var missingTypes = requiredTypes.Except(currentTypes).ToList();

        var gaps = missingTypes.Select(type => new EvidenceGap
        {
            EvidenceType = type,
            Reason = $"Required evidence type '{type}' is missing",
            Severity = "High"
        }).ToList();

        return new EvidenceGapResult
        {
            IsComplete = gaps.Count == 0,
            Gaps = gaps,
            Reasoning = gaps.Count == 0
                ? "All required evidence is present"
                : $"Missing {gaps.Count} required evidence type(s)",
            ConfidenceScore = gaps.Count == 0 ? 1.0m : 0.5m
        };
    }
}
