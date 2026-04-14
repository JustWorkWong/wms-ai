namespace WmsAi.Contracts.Events;

public sealed record ReceiptRecordedV1(
    Guid EventId,
    DateTimeOffset Timestamp,
    string TenantId,
    string WarehouseId,
    Guid ReceiptId,
    Guid InboundNoticeId,
    string ReceiptNo,
    DateTimeOffset ReceivedAt);
