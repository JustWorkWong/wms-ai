namespace WmsAi.SharedKernel.Domain;

public abstract class WarehouseScopedAggregateRoot : TenantScopedAggregateRoot
{
    protected WarehouseScopedAggregateRoot()
    {
    }

    protected WarehouseScopedAggregateRoot(string tenantId, string warehouseId)
        : base(tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(warehouseId);
        WarehouseId = warehouseId;
    }

    public string WarehouseId { get; protected set; } = string.Empty;
}
