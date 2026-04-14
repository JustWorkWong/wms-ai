namespace WmsAi.Inbound.Domain.Inbound;

public interface IInboundNoticeRepository
{
    Task<InboundNotice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InboundNotice?> GetByNoticeNoAsync(string tenantId, string warehouseId, string noticeNo, CancellationToken cancellationToken = default);
    Task AddAsync(InboundNotice notice, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNoticeNoAsync(string tenantId, string warehouseId, string noticeNo, CancellationToken cancellationToken = default);
}
