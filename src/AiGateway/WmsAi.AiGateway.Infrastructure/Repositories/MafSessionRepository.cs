using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Domain.MafSessions;
using WmsAi.AiGateway.Infrastructure.Persistence;

namespace WmsAi.AiGateway.Infrastructure.Repositories;

public sealed class MafSessionRepository(AiDbContext context) : IMafSessionRepository
{
    public async Task<MafSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.MafSessions
            .Include(s => s.Messages)
            .Include(s => s.Checkpoints)
            .Include(s => s.SummarySnapshots)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<MafSession?> GetByBusinessObjectAsync(
        string tenantId,
        string businessObjectType,
        string businessObjectId,
        CancellationToken cancellationToken = default)
    {
        return await context.MafSessions
            .Include(s => s.Messages)
            .Include(s => s.Checkpoints)
            .Include(s => s.SummarySnapshots)
            .FirstOrDefaultAsync(
                s => s.TenantId == tenantId
                    && s.BusinessObjectType == businessObjectType
                    && s.BusinessObjectId == businessObjectId,
                cancellationToken);
    }

    public async Task AddAsync(MafSession session, CancellationToken cancellationToken = default)
    {
        await context.MafSessions.AddAsync(session, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MafSession session, CancellationToken cancellationToken = default)
    {
        context.MafSessions.Update(session);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<MafSession>> GetActiveSessionsByUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await context.MafSessions
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);
    }
}
