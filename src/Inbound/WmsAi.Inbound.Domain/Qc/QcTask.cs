using WmsAi.SharedKernel.Domain;

namespace WmsAi.Inbound.Domain.Qc;

public sealed class QcTask : WarehouseScopedAggregateRoot
{
    private QcTask()
    {
    }

    public QcTask(
        string tenantId,
        string warehouseId,
        Guid inboundNoticeId,
        Guid receiptId,
        string skuCode,
        string taskNo)
        : base(tenantId, warehouseId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(inboundNoticeId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(receiptId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(skuCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskNo);

        InboundNoticeId = inboundNoticeId;
        ReceiptId = receiptId;
        SkuCode = skuCode.Trim();
        TaskNo = taskNo.Trim();
        Status = QcTaskStatus.PendingInspection;

        RaiseDomainEvent(new QcTaskCreatedEvent(Id, TenantId, WarehouseId, InboundNoticeId, ReceiptId, TaskNo, SkuCode));
    }

    public Guid InboundNoticeId { get; private set; }

    public Guid ReceiptId { get; private set; }

    public string SkuCode { get; private set; } = string.Empty;

    public string TaskNo { get; private set; } = string.Empty;

    public QcTaskStatus Status { get; private set; }

    public Guid? QcDecisionId { get; private set; }

    public void Finalize(Guid qcDecisionId, string decisionStatus)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(qcDecisionId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionStatus);

        if (QcDecisionId.HasValue)
        {
            throw new InvalidOperationException("Formal qc decision already exists.");
        }

        QcDecisionId = qcDecisionId;
        Status = decisionStatus.Trim().ToLowerInvariant() switch
        {
            "accepted" => QcTaskStatus.Completed,
            "rejected" => QcTaskStatus.Completed,
            _ => QcTaskStatus.Completed
        };

        RaiseDomainEvent(new QcDecisionFinalizedEvent(Id, qcDecisionId, decisionStatus));
    }
}
