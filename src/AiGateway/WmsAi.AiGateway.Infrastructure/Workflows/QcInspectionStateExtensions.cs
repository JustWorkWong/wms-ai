using System.Text.Json;
using WmsAi.AiGateway.Application.Agents;
using WmsAi.AiGateway.Application.Workflows;

namespace WmsAi.AiGateway.Infrastructure.Workflows;

/// <summary>
/// QcInspectionState 扩展方法 - 简化状态复制操作
/// </summary>
public static class QcInspectionStateExtensions
{
    /// <summary>
    /// 复制状态并更新指定字段
    /// </summary>
    public static QcInspectionState With(
        this QcInspectionState state,
        string? status = null,
        QcTaskDetail? qcTask = null,
        List<EvidenceAsset>? evidence = null,
        Dictionary<string, object>? qualityRules = null,
        EvidenceGapResult? evidenceGapAnalysis = null,
        InspectionDecisionResult? inspectionDecision = null,
        bool? requiresHumanApproval = null,
        ApprovalResponse? humanApproval = null,
        string? finalDecision = null,
        string? errorMessage = null)
    {
        return new QcInspectionState
        {
            QcTaskId = state.QcTaskId,
            TenantId = state.TenantId,
            WarehouseId = state.WarehouseId,
            UserId = state.UserId,
            WorkflowRunId = state.WorkflowRunId,
            QcTask = qcTask ?? state.QcTask,
            Evidence = evidence ?? state.Evidence,
            QualityRules = qualityRules ?? state.QualityRules,
            EvidenceGapAnalysis = evidenceGapAnalysis ?? state.EvidenceGapAnalysis,
            InspectionDecision = inspectionDecision ?? state.InspectionDecision,
            RequiresHumanApproval = requiresHumanApproval ?? state.RequiresHumanApproval,
            HumanApproval = humanApproval ?? state.HumanApproval,
            FinalDecision = finalDecision ?? state.FinalDecision,
            Status = status ?? state.Status,
            ErrorMessage = errorMessage ?? state.ErrorMessage
        };
    }

    /// <summary>
    /// 创建失败状态
    /// </summary>
    public static QcInspectionState WithError(this QcInspectionState state, string errorMessage)
    {
        return state.With(status: "Failed", errorMessage: errorMessage);
    }
}
