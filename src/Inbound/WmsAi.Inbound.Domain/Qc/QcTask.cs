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
        Status = "pending_inspection";
    }

    public Guid InboundNoticeId { get; private set; }

    public Guid ReceiptId { get; private set; }

    public string SkuCode { get; private set; } = string.Empty;

    public string TaskNo { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

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
            "accepted" => "accepted",
            "rejected" => "rejected",
            _ => "reviewed"
        };
    }
}
