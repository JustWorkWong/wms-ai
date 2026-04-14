namespace WmsAi.Contracts.Events;

/// <summary>
/// AI 检验完成事件 V1
/// </summary>
public sealed record AiInspectionCompletedV1(
    Guid EventId,
    DateTimeOffset Timestamp,
    string TenantId,
    string WarehouseId,
    Guid QcTaskId,
    Guid WorkflowRunId,
    string Decision,
    string Reasoning,
    double ConfidenceScore,
    bool RequiresHumanApproval,
    string? FinalDecision);
