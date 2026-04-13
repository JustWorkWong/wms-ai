using Microsoft.EntityFrameworkCore;
using WmsAi.Inbound.Application.Abstractions;
using WmsAi.Inbound.Domain.Qc;
using WmsAi.Inbound.Domain.Receipts;

namespace WmsAi.Inbound.Application.Receipts;

public sealed record RecordReceiptCommand(
    string TenantId,
    string WarehouseId,
    Guid InboundNoticeId,
    string ReceiptNo,
    IReadOnlyCollection<ReceiptLineInput> Lines);

public sealed record RecordReceiptResult(Guid ReceiptId, int QcTaskCount);

public sealed class RecordReceiptHandler(IBusinessDbContext businessDbContext)
{
    public async Task<RecordReceiptResult> Handle(
        RecordReceiptCommand command,
        CancellationToken cancellationToken = default)
    {
        var inboundNotice = await businessDbContext.InboundNotices
            .SingleOrDefaultAsync(
                entity => entity.Id == command.InboundNoticeId
                    && entity.TenantId == command.TenantId
                    && entity.WarehouseId == command.WarehouseId,
                cancellationToken);

        if (inboundNotice is null)
        {
            throw new InvalidOperationException("Inbound notice was not found.");
        }

        var receipt = new Receipt(
            command.TenantId,
            command.WarehouseId,
            command.InboundNoticeId,
            command.ReceiptNo,
            command.Lines);

        businessDbContext.Receipts.Add(receipt);

        var index = 0;
        foreach (var line in receipt.Lines.Where(line => line.ReceivedQuantity > 0))
        {
            index++;
            businessDbContext.QcTasks.Add(new QcTask(
                command.TenantId,
                command.WarehouseId,
                inboundNotice.Id,
                receipt.Id,
                line.SkuCode,
                $"{receipt.ReceiptNo}-QC-{index:000}"));
        }

        inboundNotice.MarkReceived();

        await businessDbContext.SaveChangesAsync(cancellationToken);

        return new RecordReceiptResult(receipt.Id, index);
    }
}
