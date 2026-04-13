using WmsAi.SharedKernel.Domain;

namespace WmsAi.Platform.Domain.Users;

public sealed class Membership : AggregateRoot
{
    private Membership()
    {
    }

    public Membership(Guid tenantId, Guid warehouseId, Guid userId, string role)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(warehouseId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        TenantId = tenantId;
        WarehouseId = warehouseId;
        UserId = userId;
        Role = role.Trim();
        Status = "active";
    }

    public Guid TenantId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid UserId { get; private set; }

    public string Role { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;
}
