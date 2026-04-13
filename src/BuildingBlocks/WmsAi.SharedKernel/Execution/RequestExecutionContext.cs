namespace WmsAi.SharedKernel.Execution;

public sealed record RequestExecutionContext(
    string TenantId,
    string? WarehouseId,
    string UserId,
    string MembershipId,
    string CorrelationId);
