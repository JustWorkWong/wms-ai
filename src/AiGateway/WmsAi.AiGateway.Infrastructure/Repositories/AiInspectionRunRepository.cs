using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Domain.Inspections;
using WmsAi.AiGateway.Infrastructure.Persistence;

namespace WmsAi.AiGateway.Infrastructure.Repositories;

public sealed class AiInspectionRunRepository(AiDbContext context) : IAiInspectionRunRepository
{
    public async Task<AiInspectionRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.AiInspectionRuns
            .Include(i => i.Suggestions)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<AiInspectionRun?> GetByQcTaskIdAsync(Guid qcTaskId, CancellationToken cancellationToken = default)
    {
        return await context.AiInspectionRuns
            .Include(i => i.Suggestions)
            .FirstOrDefaultAsync(i => i.QcTaskId == qcTaskId, cancellationToken);
    }

    public async Task AddAsync(AiInspectionRun inspectionRun, CancellationToken cancellationToken = default)
    {
        await context.AiInspectionRuns.AddAsync(inspectionRun, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AiInspectionRun inspectionRun, CancellationToken cancellationToken = default)
    {
        context.AiInspectionRuns.Update(inspectionRun);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AiInspectionRun>> GetByStatusAsync(
        string tenantId,
        InspectionStatus status,
        CancellationToken cancellationToken = default)
    {
        return await context.AiInspectionRuns
            .Where(i => i.TenantId == tenantId && i.Status == status)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddSuggestionAsync(AiSuggestion suggestion, CancellationToken cancellationToken = default)
    {
        await context.AiSuggestions.AddAsync(suggestion, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
