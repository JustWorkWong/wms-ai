using Microsoft.EntityFrameworkCore;
using WmsAi.Inbound.Application.Abstractions;

namespace WmsAi.Inbound.Application.Qc;

public sealed class GetQcTaskByIdHandler(IBusinessDbContext businessDbContext)
{
    public async Task<QcTaskDetailsDto?> Handle(
        Guid qcTaskId,
        string tenantId,
        string warehouseId,
        CancellationToken cancellationToken = default)
    {
        var task = await businessDbContext.QcTasks
            .Where(t => t.Id == qcTaskId
                && t.TenantId == tenantId
                && t.WarehouseId == warehouseId)
            .Select(t => new QcTaskDetailsDto(
                t.Id,
                t.TaskNo,
                t.SkuCode,
                100m, // TODO: 从 Receipt 获取实际数量
                t.Status.ToString(),
                t.InboundNoticeId,
                t.ReceiptId))
            .FirstOrDefaultAsync(cancellationToken);

        return task;
    }
}

public sealed record QcTaskDetailsDto(
    Guid QcTaskId,
    string TaskNo,
    string SkuCode,
    decimal Quantity,
    string Status,
    Guid InboundNoticeId,
    Guid ReceiptId);
