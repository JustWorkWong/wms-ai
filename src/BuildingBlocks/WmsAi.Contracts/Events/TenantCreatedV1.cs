namespace WmsAi.Contracts.Events;

public sealed record TenantCreatedV1(
    Guid EventId,
    DateTimeOffset Timestamp,
    Guid TenantId,
    string TenantCode,
    string TenantName);
