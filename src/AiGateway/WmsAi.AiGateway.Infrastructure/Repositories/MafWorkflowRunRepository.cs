using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Domain.Workflows;
using WmsAi.AiGateway.Infrastructure.Persistence;

namespace WmsAi.AiGateway.Infrastructure.Repositories;

public sealed class MafWorkflowRunRepository(AiDbContext context) : IMafWorkflowRunRepository
{
    public async Task<MafWorkflowRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.MafWorkflowRuns
            .Include(w => w.StepRuns)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task AddAsync(MafWorkflowRun workflowRun, CancellationToken cancellationToken = default)
    {
        await context.MafWorkflowRuns.AddAsync(workflowRun, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MafWorkflowRun workflowRun, CancellationToken cancellationToken = default)
    {
        var persistedStepRunIds = await context.MafWorkflowStepRuns
            .AsNoTracking()
            .Where(stepRun => stepRun.WorkflowRunId == workflowRun.Id)
            .Select(stepRun => stepRun.Id)
            .ToListAsync(cancellationToken);

        var detachedStepRuns = workflowRun.StepRuns
            .Where(stepRun => !persistedStepRunIds.Contains(stepRun.Id))
            .ToList();
        var workflowEntry = context.Entry(workflowRun);

        if (detachedStepRuns.Count > 0)
        {
            context.ChangeTracker.Clear();
            await context.MafWorkflowStepRuns.AddRangeAsync(detachedStepRuns, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (workflowEntry.State == EntityState.Detached)
        {
            context.MafWorkflowRuns.Update(workflowRun);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<MafWorkflowRun>> GetByStatusAsync(
        string tenantId,
        WorkflowStatus status,
        CancellationToken cancellationToken = default)
    {
        return await context.MafWorkflowRuns
            .Where(w => w.TenantId == tenantId && w.Status == status)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MafWorkflowRun>> GetRecentByWarehouseAsync(
        string tenantId,
        string warehouseId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await context.MafWorkflowRuns
            .Where(w => w.TenantId == tenantId && w.WarehouseId == warehouseId)
            .OrderByDescending(w => w.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
