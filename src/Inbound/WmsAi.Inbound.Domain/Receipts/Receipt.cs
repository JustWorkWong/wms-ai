using WmsAi.SharedKernel.Domain;

namespace WmsAi.Inbound.Domain.Receipts;

public sealed class Receipt : WarehouseScopedAggregateRoot
{
    private readonly List<ReceiptLine> _lines = [];

    private Receipt()
    {
    }

    public Receipt(
        string tenantId,
        string warehouseId,
        Guid inboundNoticeId,
        string receiptNo,
        IEnumerable<ReceiptLineInput> lines)
        : base(tenantId, warehouseId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(inboundNoticeId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptNo);

        InboundNoticeId = inboundNoticeId;
        ReceiptNo = receiptNo.Trim();
        Status = ReceiptStatus.Created;
        ReceivedAt = DateTimeOffset.UtcNow;

        foreach (var line in lines)
        {
            _lines.Add(new ReceiptLine(line.SkuCode, line.ReceivedQuantity));
        }

        if (_lines.Count == 0)
        {
            throw new ArgumentException("At least one line is required.", nameof(lines));
        }

        RaiseDomainEvent(new ReceiptRecordedEvent(Id, TenantId, WarehouseId, InboundNoticeId, ReceiptNo, ReceivedAt));
    }

    public Guid InboundNoticeId { get; private set; }

    public string ReceiptNo { get; private set; } = string.Empty;

    public ReceiptStatus Status { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    public IReadOnlyCollection<ReceiptLine> Lines => _lines;

    public void MarkQcCompleted()
    {
        Status = ReceiptStatus.QcCompleted;
    }
}

public sealed class ReceiptLine
{
    private ReceiptLine()
    {
    }

    internal ReceiptLine(string skuCode, decimal receivedQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skuCode);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(receivedQuantity);

        Id = Guid.NewGuid();
        SkuCode = skuCode.Trim();
        ReceivedQuantity = receivedQuantity;
    }

    public Guid Id { get; private set; }

    public string SkuCode { get; private set; } = string.Empty;

    public decimal ReceivedQuantity { get; private set; }
}

public sealed record ReceiptLineInput(string SkuCode, decimal ReceivedQuantity);
