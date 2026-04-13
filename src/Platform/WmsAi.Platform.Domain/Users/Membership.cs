using WmsAi.SharedKernel.Domain;

namespace WmsAi.Platform.Domain.Users;

public sealed class Membership : WarehouseScopedAggregateRoot
{
    private Membership()
    {
    }

    public Membership(string tenantId, string warehouseId, string userId, string role)
        : base(tenantId, warehouseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        UserId = userId.Trim();
        Role = role.Trim();
        Status = "active";
    }

    public string UserId { get; private set; } = string.Empty;

    public string Role { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;
}
