using Microsoft.EntityFrameworkCore;
using WmsAi.Platform.Domain.Tenants;
using WmsAi.Platform.Infrastructure.Persistence;

namespace WmsAi.Platform.Infrastructure.Repositories;

public sealed class WarehouseRepository(UserDbContext context) : IWarehouseRepository
{
    public Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public Task<Warehouse?> GetByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default)
    {
        return context.Warehouses.FirstOrDefaultAsync(w => w.TenantId == tenantId && w.Code == code, cancellationToken);
    }

    public Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        context.Warehouses.Add(warehouse);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default)
    {
        return context.Warehouses.AnyAsync(w => w.TenantId == tenantId && w.Code == code, cancellationToken);
    }
}
