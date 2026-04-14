using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using WmsAi.AiGateway.Application.Workflows;
using WmsAi.AiGateway.Infrastructure.Workflows.Executors;

namespace WmsAi.AiGateway.Infrastructure.Workflows;

/// <summary>
/// 质检工作流工厂 - 构建完整的 MAF Workflow
/// </summary>
public sealed class QcInspectionWorkflowFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IChatClient _chatClient;
    private readonly ILogger<QcInspectionWorkflowFactory> _logger;

    // Executors
    private readonly LoadQcTaskExecutor _loadQcTaskExecutor;
    private readonly LoadEvidenceExecutor _loadEvidenceExecutor;
    private readonly LoadRulesExecutor _loadRulesExecutor;
    private readonly EvidenceGapAgentExecutor _evidenceGapAgentExecutor;
    private readonly InspectionDecisionAgentExecutor _inspectionDecisionAgentExecutor;
    private readonly ConfidenceCheckExecutor _confidenceCheckExecutor;
    private readonly SaveResultExecutor _saveResultExecutor;
    private readonly PublishEventExecutor _publishEventExecutor;

    public QcInspectionWorkflowFactory(
        IServiceProvider serviceProvider,
        IChatClient chatClient,
        ILogger<QcInspectionWorkflowFactory> logger,
        LoadQcTaskExecutor loadQcTaskExecutor,
        LoadEvidenceExecutor loadEvidenceExecutor,
        LoadRulesExecutor loadRulesExecutor,
        EvidenceGapAgentExecutor evidenceGapAgentExecutor,
        InspectionDecisionAgentExecutor inspectionDecisionAgentExecutor,
        ConfidenceCheckExecutor confidenceCheckExecutor,
        SaveResultExecutor saveResultExecutor,
        PublishEventExecutor publishEventExecutor)
    {
        _serviceProvider = serviceProvider;
        _chatClient = chatClient;
        _logger = logger;
        _loadQcTaskExecutor = loadQcTaskExecutor;
        _loadEvidenceExecutor = loadEvidenceExecutor;
        _loadRulesExecutor = loadRulesExecutor;
        _evidenceGapAgentExecutor = evidenceGapAgentExecutor;
        _inspectionDecisionAgentExecutor = inspectionDecisionAgentExecutor;
        _confidenceCheckExecutor = confidenceCheckExecutor;
        _saveResultExecutor = saveResultExecutor;
        _publishEventExecutor = publishEventExecutor;
    }

    /// <summary>
    /// 构建质检工作流
    /// </summary>
    public async Task<Workflow> BuildAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始构建质检工作流");

        // 创建人工审批 RequestPort
        var approvalPort = new RequestPort<ApprovalRequest, ApprovalResponse>(
            "HumanApproval",
            typeof(ApprovalRequest),
            typeof(ApprovalResponse));

        // 使用 WorkflowBuilder 构建工作流（从 LoadQcTask 开始）
        var builder = new WorkflowBuilder(_loadQcTaskExecutor);

        // 2. 数据加载链：LoadQcTask → LoadEvidence → LoadRules
        builder.AddEdge(_loadQcTaskExecutor, _loadEvidenceExecutor);
        builder.AddEdge(_loadEvidenceExecutor, _loadRulesExecutor);

        // 3. AI 分析链：LoadRules → EvidenceGapAgent → InspectionDecisionAgent
        builder.AddEdge(_loadRulesExecutor, _evidenceGapAgentExecutor);
        builder.AddEdge(_evidenceGapAgentExecutor, _inspectionDecisionAgentExecutor);

        // 4. 置信度检查：InspectionDecisionAgent → ConfidenceCheck
        builder.AddEdge(_inspectionDecisionAgentExecutor, _confidenceCheckExecutor);

        // 5. 条件分支：根据置信度决定路径
        // 高置信度路径：ConfidenceCheck → SaveResult
        builder.AddEdge<QcInspectionState>(
            _confidenceCheckExecutor,
            _saveResultExecutor,
            state => state != null && !state.RequiresHumanApproval);

        // 低置信度路径：ConfidenceCheck → RequestPort (暂停等待人工审批)
        builder.AddEdge<QcInspectionState>(
            _confidenceCheckExecutor,
            approvalPort,
            state => state != null && state.RequiresHumanApproval);

        // 人工审批后继续：RequestPort → SaveResult
        builder.AddEdge(approvalPort, _saveResultExecutor);

        // 6. 发布事件：SaveResult → PublishEvent
        builder.AddEdge(_saveResultExecutor, _publishEventExecutor);

        // 7. 设置输出
        builder.WithOutputFrom(_publishEventExecutor);

        // 8. 构建 Workflow
        var workflow = builder.Build();

        _logger.LogInformation("质检工作流构建完成");

        return await Task.FromResult(workflow);
    }
}

/// <summary>
/// 人工审批请求
/// </summary>
public sealed class ApprovalRequest
{
    /// <summary>
    /// 质检任务 ID
    /// </summary>
    public Guid QcTaskId { get; init; }

    /// <summary>
    /// AI 建议的决策
    /// </summary>
    public string AiDecision { get; init; } = string.Empty;

    /// <summary>
    /// AI 决策理由
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;

    /// <summary>
    /// 置信度分数
    /// </summary>
    public decimal ConfidenceScore { get; init; }

    /// <summary>
    /// 证据缺口分析
    /// </summary>
    public string? EvidenceGaps { get; init; }

    /// <summary>
    /// 请求时间
    /// </summary>
    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
}
