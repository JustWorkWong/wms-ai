using DotNetCore.CAP;
using WmsAi.Contracts.Events;
using WmsAi.Inbound.Application.Qc;
using WmsAi.Inbound.Application.Receipts;
using WmsAi.Inbound.Domain.Qc;
using WmsAi.Inbound.Domain.Receipts;
using WmsAi.SharedKernel.Domain;
using WmsAi.SharedKernel.Persistence;

namespace WmsAi.Inbound.Infrastructure.Events;

public sealed class CapEventPublisher(ICapPublisher capPublisher, DomainEventDispatcher dispatcher)
    : WmsAi.Inbound.Application.Receipts.IEventPublisher, WmsAi.Inbound.Application.Qc.IEventPublisher
{
    public async Task PublishCollectedEventsAsync(CancellationToken cancellationToken = default)
    {
        var events = dispatcher.GetCollectedEvents();

        foreach (var domainEvent in events)
        {
            await PublishEventAsync(domainEvent, cancellationToken);
        }

        dispatcher.Clear();
    }

    private async Task PublishEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case ReceiptRecordedEvent e:
                await capPublisher.PublishAsync(
                    "receipt.recorded.v1",
                    new ReceiptRecordedV1(
                        e.EventId,
                        e.OccurredAt,
                        e.TenantId,
                        e.WarehouseId,
                        e.ReceiptId,
                        e.InboundNoticeId,
                        e.ReceiptNo,
                        e.ReceivedAt),
                    cancellationToken: cancellationToken);
                break;

            case QcTaskCreatedEvent e:
                await capPublisher.PublishAsync(
                    "qctask.created.v1",
                    new QcTaskCreatedV1(
                        e.EventId,
                        e.OccurredAt,
                        e.TenantId,
                        e.WarehouseId,
                        e.QcTaskId,
                        e.InboundNoticeId,
                        e.ReceiptId,
                        e.TaskNo,
                        e.SkuCode),
                    cancellationToken: cancellationToken);
                break;

            case QcDecisionFinalizedEvent e:
                await capPublisher.PublishAsync(
                    "qcdecision.finalized.v1",
                    new QcDecisionFinalizedV1(
                        e.EventId,
                        e.OccurredAt,
                        e.TenantId,
                        e.WarehouseId,
                        e.QcTaskId,
                        e.QcDecisionId,
                        e.DecisionStatus,
                        e.DecisionSource),
                    cancellationToken: cancellationToken);
                break;
        }
    }
}
