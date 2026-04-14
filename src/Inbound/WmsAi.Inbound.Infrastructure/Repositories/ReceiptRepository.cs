using Microsoft.EntityFrameworkCore;
using WmsAi.Inbound.Domain.Receipts;
using WmsAi.Inbound.Infrastructure.Persistence;

namespace WmsAi.Inbound.Infrastructure.Repositories;

public sealed class ReceiptRepository(BusinessDbContext context) : IReceiptRepository
{
    public Task<Receipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Receipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<Receipt?> GetByReceiptNoAsync(string tenantId, string warehouseId, string receiptNo, CancellationToken cancellationToken = default)
    {
        return context.Receipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.WarehouseId == warehouseId && r.ReceiptNo == receiptNo, cancellationToken);
    }

    public Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        context.Receipts.Add(receipt);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByReceiptNoAsync(string tenantId, string warehouseId, string receiptNo, CancellationToken cancellationToken = default)
    {
        return context.Receipts.AnyAsync(r => r.TenantId == tenantId && r.WarehouseId == warehouseId && r.ReceiptNo == receiptNo, cancellationToken);
    }
}
