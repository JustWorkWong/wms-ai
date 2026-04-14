using Microsoft.EntityFrameworkCore;
using WmsAi.Inbound.Domain.Inbound;
using WmsAi.Inbound.Infrastructure.Persistence;

namespace WmsAi.Inbound.Infrastructure.Repositories;

public sealed class InboundNoticeRepository(BusinessDbContext context) : IInboundNoticeRepository
{
    public Task<InboundNotice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.InboundNotices
            .Include(n => n.Lines)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public Task<InboundNotice?> GetByNoticeNoAsync(string tenantId, string warehouseId, string noticeNo, CancellationToken cancellationToken = default)
    {
        return context.InboundNotices
            .Include(n => n.Lines)
            .FirstOrDefaultAsync(n => n.TenantId == tenantId && n.WarehouseId == warehouseId && n.NoticeNo == noticeNo, cancellationToken);
    }

    public Task AddAsync(InboundNotice notice, CancellationToken cancellationToken = default)
    {
        context.InboundNotices.Add(notice);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByNoticeNoAsync(string tenantId, string warehouseId, string noticeNo, CancellationToken cancellationToken = default)
    {
        return context.InboundNotices.AnyAsync(n => n.TenantId == tenantId && n.WarehouseId == warehouseId && n.NoticeNo == noticeNo, cancellationToken);
    }
}
