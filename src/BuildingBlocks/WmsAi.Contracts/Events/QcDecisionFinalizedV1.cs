namespace WmsAi.Contracts.Events;

public sealed record QcDecisionFinalizedV1(
    Guid EventId,
    DateTimeOffset Timestamp,
    string TenantId,
    string WarehouseId,
    Guid QcTaskId,
    Guid QcDecisionId,
    string DecisionStatus,
    string DecisionSource);
