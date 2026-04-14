using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Domain.ModelConfig;
using WmsAi.AiGateway.Infrastructure.Persistence;

namespace WmsAi.AiGateway.Infrastructure.Repositories;

public sealed class AiModelProfileRepository(AiDbContext context) : IAiModelProfileRepository
{
    public async Task<AiModelProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.AiModelProfiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<AiModelProfile?> GetByProfileCodeAsync(string profileCode, CancellationToken cancellationToken = default)
    {
        return await context.AiModelProfiles.FirstOrDefaultAsync(p => p.ProfileCode == profileCode, cancellationToken);
    }

    public async Task<List<AiModelProfile>> GetBySceneCodeAsync(string sceneCode, CancellationToken cancellationToken = default)
    {
        return await context.AiModelProfiles
            .Where(p => p.SceneCode == sceneCode && p.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AiModelProfile profile, CancellationToken cancellationToken = default)
    {
        await context.AiModelProfiles.AddAsync(profile, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AiModelProfile profile, CancellationToken cancellationToken = default)
    {
        context.AiModelProfiles.Update(profile);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AiModelProfile>> GetActiveProfilesAsync(CancellationToken cancellationToken = default)
    {
        return await context.AiModelProfiles
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }
}
