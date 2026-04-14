using Microsoft.EntityFrameworkCore;
using WmsAi.Inbound.Domain.Qc;
using WmsAi.Inbound.Infrastructure.Persistence;

namespace WmsAi.Inbound.Infrastructure.Repositories;

public sealed class QcTaskRepository(BusinessDbContext context) : IQcTaskRepository
{
    public Task<QcTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.QcTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task<QcTask?> GetByTaskNoAsync(string tenantId, string warehouseId, string taskNo, CancellationToken cancellationToken = default)
    {
        return context.QcTasks.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.WarehouseId == warehouseId && t.TaskNo == taskNo, cancellationToken);
    }

    public Task<List<QcTask>> GetByReceiptIdAsync(Guid receiptId, CancellationToken cancellationToken = default)
    {
        return context.QcTasks.Where(t => t.ReceiptId == receiptId).ToListAsync(cancellationToken);
    }

    public Task AddAsync(QcTask task, CancellationToken cancellationToken = default)
    {
        context.QcTasks.Add(task);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByTaskNoAsync(string tenantId, string warehouseId, string taskNo, CancellationToken cancellationToken = default)
    {
        return context.QcTasks.AnyAsync(t => t.TenantId == tenantId && t.WarehouseId == warehouseId && t.TaskNo == taskNo, cancellationToken);
    }
}
