using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using WmsAi.AiGateway.Application.Workflows;

namespace WmsAi.AiGateway.Infrastructure.Workflows.Executors;

/// <summary>
/// 置信度检查 Executor - 检查 AI 决策的置信度，决定是否需要人工审批
/// </summary>
public sealed partial class ConfidenceCheckExecutor : Executor
{
    private readonly ILogger<ConfidenceCheckExecutor> _logger;
    private const decimal ConfidenceThreshold = 0.8m;

    public ConfidenceCheckExecutor(ILogger<ConfidenceCheckExecutor> logger)
        : base("ConfidenceCheck")
    {
        _logger = logger;
    }

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder builder)
    {
        return builder;
    }

    [MessageHandler]
    public Task<QcInspectionState> HandleAsync(
        QcInspectionState state,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "开始置信度检查: QcTaskId={QcTaskId}, ConfidenceScore={ConfidenceScore}",
            state.QcTaskId,
            state.InspectionDecision?.ConfidenceScore);

        if (state.InspectionDecision == null)
        {
            _logger.LogWarning(
                "质检决策结果为空: QcTaskId={QcTaskId}",
                state.QcTaskId);

            return Task.FromResult(new QcInspectionState
            {
                QcTaskId = state.QcTaskId,
                TenantId = state.TenantId,
                WarehouseId = state.WarehouseId,
                UserId = state.UserId,
                WorkflowRunId = state.WorkflowRunId,
                QcTask = state.QcTask,
                Evidence = state.Evidence,
                QualityRules = state.QualityRules,
                EvidenceGapAnalysis = state.EvidenceGapAnalysis,
                InspectionDecision = state.InspectionDecision,
                RequiresHumanApproval = true,
                Status = "ConfidenceCheckFailed",
                ErrorMessage = "质检决策结果为空"
            });
        }

        var confidenceScore = state.InspectionDecision.ConfidenceScore;
        var requiresHumanApproval = confidenceScore < ConfidenceThreshold;

        _logger.LogInformation(
            "置信度检查完成: QcTaskId={QcTaskId}, ConfidenceScore={ConfidenceScore}, Threshold={Threshold}, RequiresHumanApproval={RequiresHumanApproval}",
            state.QcTaskId,
            confidenceScore,
            ConfidenceThreshold,
            requiresHumanApproval);

        return Task.FromResult(new QcInspectionState
        {
            QcTaskId = state.QcTaskId,
            TenantId = state.TenantId,
            WarehouseId = state.WarehouseId,
            UserId = state.UserId,
            WorkflowRunId = state.WorkflowRunId,
            QcTask = state.QcTask,
            Evidence = state.Evidence,
            QualityRules = state.QualityRules,
            EvidenceGapAnalysis = state.EvidenceGapAnalysis,
            InspectionDecision = state.InspectionDecision,
            RequiresHumanApproval = requiresHumanApproval,
            Status = "ConfidenceChecked"
        });
    }
}
