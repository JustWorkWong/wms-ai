namespace WmsAi.SharedKernel.Domain;

public abstract class TenantScopedAggregateRoot : AggregateRoot
{
    protected TenantScopedAggregateRoot()
    {
    }

    protected TenantScopedAggregateRoot(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        TenantId = tenantId;
    }

    public string TenantId { get; protected set; } = string.Empty;
}
