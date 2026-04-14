namespace WmsAi.Platform.Domain.Tenants;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default);
    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default);
}
