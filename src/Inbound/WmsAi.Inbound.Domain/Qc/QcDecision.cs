using WmsAi.SharedKernel.Domain;

namespace WmsAi.Inbound.Domain.Qc;

public sealed class QcDecision : WarehouseScopedAggregateRoot
{
    private QcDecision()
    {
    }

    public QcDecision(
        string tenantId,
        string warehouseId,
        Guid qcTaskId,
        string decisionStatus,
        string decisionSource,
        string reasonSummary)
        : base(tenantId, warehouseId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(qcTaskId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionStatus);
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonSummary);

        QcTaskId = qcTaskId;
        DecisionStatus = decisionStatus.Trim();
        DecisionSource = decisionSource.Trim();
        ReasonSummary = reasonSummary.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public Guid QcTaskId { get; private set; }

    public string DecisionStatus { get; private set; } = string.Empty;

    public string DecisionSource { get; private set; } = string.Empty;

    public DateTimeOffset ReviewedAt { get; private set; }

    public string ReasonSummary { get; private set; } = string.Empty;
}
