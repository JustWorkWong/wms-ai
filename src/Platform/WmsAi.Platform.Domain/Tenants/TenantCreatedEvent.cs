using WmsAi.SharedKernel.Domain;

namespace WmsAi.Platform.Domain.Tenants;

public sealed record TenantCreatedEvent(
    Guid TenantId,
    string TenantCode,
    string TenantName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
