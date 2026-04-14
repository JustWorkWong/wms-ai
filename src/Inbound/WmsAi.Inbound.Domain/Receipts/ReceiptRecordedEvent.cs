using WmsAi.SharedKernel.Domain;

namespace WmsAi.Inbound.Domain.Receipts;

public sealed record ReceiptRecordedEvent(
    Guid ReceiptId,
    string TenantId,
    string WarehouseId,
    Guid InboundNoticeId,
    string ReceiptNo,
    DateTimeOffset ReceivedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
