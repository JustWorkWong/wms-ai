using WmsAi.SharedKernel.Domain;

namespace WmsAi.Platform.Domain.Tenants;

public sealed class Warehouse : AggregateRoot
{
    private Warehouse()
    {
    }

    public Warehouse(Guid tenantId, string code, string name, bool isDefault)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        TenantId = tenantId;
        Code = code.Trim();
        Name = name.Trim();
        IsDefault = isDefault;
        Status = WarehouseStatus.Active;

        RaiseDomainEvent(new WarehouseCreatedEvent(Id, TenantId, Code, Name, IsDefault));
    }

    public Guid TenantId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsDefault { get; private set; }

    public WarehouseStatus Status { get; private set; }
}
