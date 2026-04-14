using Microsoft.EntityFrameworkCore;
using WmsAi.Inbound.Domain.Qc;
using WmsAi.Inbound.Infrastructure.Persistence;

namespace WmsAi.Inbound.Infrastructure.Repositories;

public sealed class QcDecisionRepository(BusinessDbContext context) : IQcDecisionRepository
{
    public Task<QcDecision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.QcDecisions.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public Task<QcDecision?> GetByQcTaskIdAsync(Guid qcTaskId, CancellationToken cancellationToken = default)
    {
        return context.QcDecisions.FirstOrDefaultAsync(d => d.QcTaskId == qcTaskId, cancellationToken);
    }

    public Task AddAsync(QcDecision decision, CancellationToken cancellationToken = default)
    {
        context.QcDecisions.Add(decision);
        return Task.CompletedTask;
    }
}
