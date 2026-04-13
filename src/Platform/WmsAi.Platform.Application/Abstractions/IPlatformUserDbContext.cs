using WmsAi.Platform.Domain.Tenants;
using WmsAi.Platform.Domain.Users;

namespace WmsAi.Platform.Application.Abstractions;

public interface IPlatformUserDbContext
{
    Task AddTenantAsync(Tenant tenant, CancellationToken cancellationToken);

    Task AddWarehouseAsync(Warehouse warehouse, CancellationToken cancellationToken);

    Task AddUserAsync(User user, CancellationToken cancellationToken);

    Task AddMembershipAsync(Membership membership, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
