using WmsAi.SharedKernel.Domain;

namespace WmsAi.Inbound.Domain.Inbound;

public sealed class InboundNotice : WarehouseScopedAggregateRoot
{
    private readonly List<InboundNoticeLine> _lines = [];

    private InboundNotice()
    {
    }

    public InboundNotice(string tenantId, string warehouseId, string noticeNo, IEnumerable<InboundNoticeLineInput> lines)
        : base(tenantId, warehouseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(noticeNo);

        NoticeNo = noticeNo.Trim();
        Status = InboundNoticeStatus.Created;

        foreach (var line in lines)
        {
            _lines.Add(new InboundNoticeLine(line.SkuCode, line.ExpectedQuantity));
        }

        if (_lines.Count == 0)
        {
            throw new ArgumentException("At least one line is required.", nameof(lines));
        }

        RaiseDomainEvent(new InboundNoticeCreatedEvent(Id, TenantId, WarehouseId, NoticeNo));
    }

    public string NoticeNo { get; private set; } = string.Empty;

    public InboundNoticeStatus Status { get; private set; }

    public IReadOnlyCollection<InboundNoticeLine> Lines => _lines;

    public void MarkReceived()
    {
        Status = InboundNoticeStatus.Received;
    }
}

public sealed class InboundNoticeLine
{
    private InboundNoticeLine()
    {
    }

    internal InboundNoticeLine(string skuCode, decimal expectedQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skuCode);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedQuantity);

        Id = Guid.NewGuid();
        SkuCode = skuCode.Trim();
        ExpectedQuantity = expectedQuantity;
    }

    public Guid Id { get; private set; }

    public string SkuCode { get; private set; } = string.Empty;

    public decimal ExpectedQuantity { get; private set; }
}

public sealed record InboundNoticeLineInput(string SkuCode, decimal ExpectedQuantity);
