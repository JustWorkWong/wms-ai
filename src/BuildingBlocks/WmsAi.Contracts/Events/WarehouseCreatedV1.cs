namespace WmsAi.Contracts.Events;

public sealed record WarehouseCreatedV1(
    Guid EventId,
    DateTimeOffset Timestamp,
    Guid TenantId,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    bool IsDefault);
