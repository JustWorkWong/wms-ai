using WmsAi.SharedKernel.Domain;

namespace WmsAi.Inbound.Domain.Qc;

public sealed record QcTaskCreatedEvent(
    Guid QcTaskId,
    string TenantId,
    string WarehouseId,
    Guid InboundNoticeId,
    Guid ReceiptId,
    string TaskNo,
    string SkuCode) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
