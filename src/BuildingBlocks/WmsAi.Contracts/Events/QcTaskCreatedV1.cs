namespace WmsAi.Contracts.Events;

public sealed record QcTaskCreatedV1(
    Guid EventId,
    DateTimeOffset Timestamp,
    string TenantId,
    string WarehouseId,
    Guid QcTaskId,
    Guid InboundNoticeId,
    Guid ReceiptId,
    string TaskNo,
    string SkuCode);
