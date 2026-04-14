namespace WmsAi.Inbound.Domain.Qc;

public interface IQcTaskRepository
{
    Task<QcTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QcTask?> GetByTaskNoAsync(string tenantId, string warehouseId, string taskNo, CancellationToken cancellationToken = default);
    Task<List<QcTask>> GetByReceiptIdAsync(Guid receiptId, CancellationToken cancellationToken = default);
    Task AddAsync(QcTask task, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTaskNoAsync(string tenantId, string warehouseId, string taskNo, CancellationToken cancellationToken = default);
}
