using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Domain.ModelConfig;
using WmsAi.AiGateway.Infrastructure.Persistence;

namespace WmsAi.AiGateway.Infrastructure.Repositories;

public sealed class AiModelProviderRepository(AiDbContext context) : IAiModelProviderRepository
{
    public async Task<AiModelProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.AiModelProviders.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<AiModelProvider?> GetByProviderCodeAsync(string providerCode, CancellationToken cancellationToken = default)
    {
        return await context.AiModelProviders.FirstOrDefaultAsync(p => p.ProviderCode == providerCode, cancellationToken);
    }

    public async Task AddAsync(AiModelProvider provider, CancellationToken cancellationToken = default)
    {
        await context.AiModelProviders.AddAsync(provider, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AiModelProvider provider, CancellationToken cancellationToken = default)
    {
        context.AiModelProviders.Update(provider);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AiModelProvider>> GetActiveProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await context.AiModelProviders
            .Where(p => p.Status == ProviderStatus.Active)
            .ToListAsync(cancellationToken);
    }
}
