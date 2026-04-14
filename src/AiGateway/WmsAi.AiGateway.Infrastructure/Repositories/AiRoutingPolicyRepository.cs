using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Domain.ModelConfig;
using WmsAi.AiGateway.Infrastructure.Persistence;

namespace WmsAi.AiGateway.Infrastructure.Repositories;

public sealed class AiRoutingPolicyRepository(AiDbContext context) : IAiRoutingPolicyRepository
{
    public async Task<AiRoutingPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.AiRoutingPolicies.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<AiRoutingPolicy>> GetBySceneCodeAsync(
        string sceneCode,
        string tenantId,
        string? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var query = context.AiRoutingPolicies
            .Where(p => p.SceneCode == sceneCode && p.TenantId == tenantId && p.IsActive);

        if (!string.IsNullOrWhiteSpace(warehouseId))
        {
            query = query.Where(p => p.WarehouseId == warehouseId || p.WarehouseId == null);
        }
        else
        {
            query = query.Where(p => p.WarehouseId == null);
        }

        return await query
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.WarehouseId != null ? 0 : 1)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AiRoutingPolicy policy, CancellationToken cancellationToken = default)
    {
        await context.AiRoutingPolicies.AddAsync(policy, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AiRoutingPolicy policy, CancellationToken cancellationToken = default)
    {
        context.AiRoutingPolicies.Update(policy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AiRoutingPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default)
    {
        return await context.AiRoutingPolicies
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }
}
