using Microsoft.EntityFrameworkCore;
using WmsAi.Inbound.Application.Support;
using WmsAi.Inbound.Application.Abstractions;
using WmsAi.Inbound.Domain.Inbound;

namespace WmsAi.Inbound.Application.Inbound;

public sealed record CreateInboundNoticeCommand(
    string TenantId,
    string WarehouseId,
    string NoticeNo,
    IReadOnlyCollection<InboundNoticeLineInput> Lines);

public sealed record CreateInboundNoticeResult(Guid InboundNoticeId, string NoticeNo);

public sealed class CreateInboundNoticeHandler(IBusinessDbContext businessDbContext)
{
    public async Task<CreateInboundNoticeResult> Handle(
        CreateInboundNoticeCommand command,
        CancellationToken cancellationToken = default)
    {
        var inboundNotice = new InboundNotice(
            command.TenantId,
            command.WarehouseId,
            command.NoticeNo,
            command.Lines);

        businessDbContext.InboundNotices.Add(inboundNotice);
        try
        {
            await businessDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (InboundPersistenceExceptionTranslator.Translate(exception, "Inbound notice already exists.") is InboundConflictException translated)
        {
            throw translated;
        }

        return new CreateInboundNoticeResult(inboundNotice.Id, inboundNotice.NoticeNo);
    }
}
