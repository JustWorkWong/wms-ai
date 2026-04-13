namespace WmsAi.SharedKernel.Execution;

public sealed record RequestExecutionContext
{
    public RequestExecutionContext(
        string tenantId,
        string? warehouseId,
        string userId,
        string membershipId,
        string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(membershipId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        if (warehouseId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(warehouseId);
        }

        TenantId = tenantId;
        WarehouseId = warehouseId;
        UserId = userId;
        MembershipId = membershipId;
        CorrelationId = correlationId;
    }

    public string TenantId { get; }

    public string? WarehouseId { get; }

    public string UserId { get; }

    public string MembershipId { get; }

    public string CorrelationId { get; }
}
