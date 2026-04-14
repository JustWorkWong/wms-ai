namespace WmsAi.Inbound.Domain.Receipts;

public interface IReceiptRepository
{
    Task<Receipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Receipt?> GetByReceiptNoAsync(string tenantId, string warehouseId, string receiptNo, CancellationToken cancellationToken = default);
    Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default);
    Task<bool> ExistsByReceiptNoAsync(string tenantId, string warehouseId, string receiptNo, CancellationToken cancellationToken = default);
}
