using Microsoft.EntityFrameworkCore;
using WmsAi.Inbound.Domain.Inbound;
using WmsAi.Inbound.Domain.Qc;
using WmsAi.Inbound.Domain.Receipts;

namespace WmsAi.Inbound.Application.Abstractions;

public interface IBusinessDbContext
{
    DbSet<InboundNotice> InboundNotices { get; }

    DbSet<Receipt> Receipts { get; }

    DbSet<QcTask> QcTasks { get; }

    DbSet<QcDecision> QcDecisions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
