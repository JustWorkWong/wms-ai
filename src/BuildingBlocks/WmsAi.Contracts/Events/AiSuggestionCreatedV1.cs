namespace WmsAi.Contracts.Events;

public sealed record AiSuggestionCreatedV1(
    Guid EventId,
    DateTimeOffset Timestamp,
    string TenantId,
    string WarehouseId,
    Guid QcTaskId,
    Guid SuggestionId,
    string SuggestedDecision,
    string Reasoning);
