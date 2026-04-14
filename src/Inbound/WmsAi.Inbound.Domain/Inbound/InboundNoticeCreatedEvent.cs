using WmsAi.SharedKernel.Domain;

namespace WmsAi.Inbound.Domain.Inbound;

public sealed record InboundNoticeCreatedEvent(
    Guid InboundNoticeId,
    string TenantId,
    string WarehouseId,
    string NoticeNo) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
