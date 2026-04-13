using Microsoft.EntityFrameworkCore;
using WmsAi.Inbound.Application.Abstractions;
using WmsAi.Inbound.Application.Support;
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
            throw new InboundNotFoundException("Inbound notice was not found.");
        }

        if (command.Lines.Count == 0)
        {
            throw new InboundValidationException("At least one receipt line is required.");
        }

        if (command.Lines.Any(line => line.ReceivedQuantity <= 0))
        {
            throw new InboundValidationException("Receipt lines must have positive quantity.");
        }

        Receipt receipt;
        try
        {
            receipt = new Receipt(
                command.TenantId,
                command.WarehouseId,
                command.InboundNoticeId,
                command.ReceiptNo,
                command.Lines);
        }
        catch (ArgumentException exception)
        {
            throw new InboundValidationException(exception.Message);
        }

        businessDbContext.Receipts.Add(receipt);

        var index = 0;
        foreach (var line in receipt.Lines)
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

        try
        {
            await businessDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (InboundPersistenceExceptionTranslator.Translate(exception, "Receipt already exists.") is InboundConflictException translated)
        {
            throw translated;
        }

        return new RecordReceiptResult(receipt.Id, index);
    }
}
