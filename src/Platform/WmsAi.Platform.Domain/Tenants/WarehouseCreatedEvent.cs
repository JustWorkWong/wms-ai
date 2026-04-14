using WmsAi.SharedKernel.Domain;

namespace WmsAi.Platform.Domain.Tenants;

public sealed record WarehouseCreatedEvent(
    Guid WarehouseId,
    Guid TenantId,
    string WarehouseCode,
    string WarehouseName,
    bool IsDefault) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
