using WmsAi.SharedKernel.Domain;

namespace WmsAi.Inbound.Domain.Qc;

public sealed record QcDecisionFinalizedEvent(
    Guid QcTaskId,
    Guid QcDecisionId,
    string TenantId,
    string WarehouseId,
    string DecisionStatus,
    string DecisionSource) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
