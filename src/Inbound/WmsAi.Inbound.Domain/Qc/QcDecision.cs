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
        DecisionResult = ParseDecisionResult(decisionStatus);
        DecisionSource = decisionSource.Trim();
        ReasonSummary = reasonSummary.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public Guid QcTaskId { get; private set; }

    public QcDecisionResult DecisionResult { get; private set; }

    public string DecisionSource { get; private set; } = string.Empty;

    public DateTimeOffset ReviewedAt { get; private set; }

    public string ReasonSummary { get; private set; } = string.Empty;

    private static QcDecisionResult ParseDecisionResult(string decisionStatus)
    {
        return decisionStatus.Trim().ToLowerInvariant() switch
        {
            "accepted" => QcDecisionResult.Accepted,
            "rejected" => QcDecisionResult.Rejected,
            "conditional" => QcDecisionResult.Conditional,
            _ => throw new ArgumentException($"Invalid decision status: {decisionStatus}", nameof(decisionStatus))
        };
    }
}
