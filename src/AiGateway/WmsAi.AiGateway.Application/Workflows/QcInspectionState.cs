using WmsAi.AiGateway.Application.Agents;

namespace WmsAi.AiGateway.Application.Workflows;

/// <summary>
/// MAF Workflow 共享状态,包含质检工作流的所有输入、中间数据和输出结果
/// </summary>
public sealed class QcInspectionState
{
    /// <summary>
    /// 质检任务 ID
    /// </summary>
    public Guid QcTaskId { get; init; }

    /// <summary>
    /// 租户 ID
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// 仓库 ID
    /// </summary>
    public string WarehouseId { get; init; } = string.Empty;

    /// <summary>
    /// 用户 ID
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Workflow Run ID
    /// </summary>
    public Guid WorkflowRunId { get; init; }

    /// <summary>
    /// 质检任务详情(由 LoadQcTaskExecutor 加载)
    /// </summary>
    public QcTaskDetail? QcTask { get; init; }

    /// <summary>
    /// 证据资产列表(由 LoadEvidenceExecutor 加载)
    /// </summary>
    public List<EvidenceAsset> Evidence { get; init; } = [];

    /// <summary>
    /// 质量规则(由 LoadRulesExecutor 加载)
    /// </summary>
    public Dictionary<string, object> QualityRules { get; init; } = [];

    /// <summary>
    /// 证据缺口分析结果(由 EvidenceGapAgentExecutor 生成)
    /// </summary>
    public EvidenceGapResult? EvidenceGapAnalysis { get; init; }

    /// <summary>
    /// 质检决策结果(由 InspectionDecisionAgentExecutor 生成)
    /// </summary>
    public InspectionDecisionResult? InspectionDecision { get; init; }

    /// <summary>
    /// 是否需要人工审批(由 ConfidenceCheckExecutor 判断)
    /// </summary>
    public bool RequiresHumanApproval { get; init; }

    /// <summary>
    /// 人工审批响应(由 ApprovalRequestPort 接收)
    /// </summary>
    public ApprovalResponse? HumanApproval { get; init; }

    /// <summary>
    /// 最终决策(Approve/Reject/Conditional)
    /// </summary>
    public string? FinalDecision { get; init; }

    /// <summary>
    /// 工作流执行状态
    /// </summary>
    public string Status { get; init; } = "Pending";

    /// <summary>
    /// 错误信息(如果执行失败)
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 质检任务详情
/// </summary>
public sealed class QcTaskDetail
{
    /// <summary>
    /// 质检任务 ID
    /// </summary>
    public Guid QcTaskId { get; init; }

    /// <summary>
    /// SKU 编码
    /// </summary>
    public string SkuCode { get; init; } = string.Empty;

    /// <summary>
    /// SKU 名称
    /// </summary>
    public string SkuName { get; init; } = string.Empty;

    /// <summary>
    /// 质检数量
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// 质检类型(Inbound/Outbound/Inventory)
    /// </summary>
    public string QcType { get; init; } = string.Empty;

    /// <summary>
    /// 任务状态
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// 扩展元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = [];
}

/// <summary>
/// 人工审批响应
/// </summary>
public sealed class ApprovalResponse
{
    /// <summary>
    /// 审批决策(Approve/Reject/RequestMoreInfo)
    /// </summary>
    public string Decision { get; init; } = string.Empty;

    /// <summary>
    /// 审批意见
    /// </summary>
    public string Comments { get; init; } = string.Empty;

    /// <summary>
    /// 审批人 ID
    /// </summary>
    public string ReviewerId { get; init; } = string.Empty;

    /// <summary>
    /// 审批时间
    /// </summary>
    public DateTimeOffset ApprovedAt { get; init; }

    /// <summary>
    /// 扩展元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = [];
}
