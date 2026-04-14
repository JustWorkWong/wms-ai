namespace WmsAi.AiGateway.Application.Workflows;

/// <summary>
/// Workflow 相关常量定义
/// </summary>
public static class WorkflowConstants
{
    /// <summary>
    /// 高置信度阈值 - 超过此值自动批准，低于此值需要人工审批
    /// </summary>
    public const decimal HighConfidenceThreshold = 0.8m;

    /// <summary>
    /// 系统用户 ID - 用于自动化流程
    /// </summary>
    public const string SystemUserId = "system";

    /// <summary>
    /// AI 温度参数 - 证据缺口分析（较高创造性）
    /// </summary>
    public const float EvidenceGapTemperature = 0.3f;

    /// <summary>
    /// AI 温度参数 - 质检决策（较低创造性，更确定性）
    /// </summary>
    public const float InspectionDecisionTemperature = 0.2f;

    /// <summary>
    /// AI 最大输出 Token - 证据缺口分析
    /// </summary>
    public const int EvidenceGapMaxTokens = 1000;

    /// <summary>
    /// AI 最大输出 Token - 质检决策
    /// </summary>
    public const int InspectionDecisionMaxTokens = 1500;
}
