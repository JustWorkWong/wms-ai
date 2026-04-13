using WmsAi.SharedKernel.Domain;

namespace WmsAi.Platform.Domain.Tenants;

public sealed class Warehouse : TenantScopedAggregateRoot
{
    private Warehouse()
    {
    }

    public Warehouse(string tenantId, string code, string name, bool isDefault)
        : base(tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Code = code.Trim();
        Name = name.Trim();
        IsDefault = isDefault;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsDefault { get; private set; }
}
