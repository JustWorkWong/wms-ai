using Microsoft.EntityFrameworkCore;
using WmsAi.Platform.Domain.Tenants;
using WmsAi.Platform.Infrastructure.Persistence;

namespace WmsAi.Platform.Infrastructure.Repositories;

public sealed class TenantRepository(UserDbContext context) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task<Tenant?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return context.Tenants.FirstOrDefaultAsync(t => t.Code == code, cancellationToken);
    }

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        context.Tenants.Add(tenant);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return context.Tenants.AnyAsync(t => t.Code == code, cancellationToken);
    }
}
