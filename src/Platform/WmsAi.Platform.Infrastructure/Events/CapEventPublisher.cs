using DotNetCore.CAP;
using WmsAi.Contracts.Events;
using WmsAi.Platform.Application.Tenants;
using WmsAi.Platform.Domain.Tenants;
using WmsAi.SharedKernel.Domain;
using WmsAi.SharedKernel.Persistence;

namespace WmsAi.Platform.Infrastructure.Events;

public sealed class CapEventPublisher(ICapPublisher capPublisher, DomainEventDispatcher dispatcher) : IEventPublisher
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
            case TenantCreatedEvent e:
                await capPublisher.PublishAsync(
                    "tenant.created.v1",
                    new TenantCreatedV1(
                        e.EventId,
                        e.OccurredAt,
                        e.TenantId,
                        e.TenantCode,
                        e.TenantName),
                    cancellationToken: cancellationToken);
                break;

            case WarehouseCreatedEvent e:
                await capPublisher.PublishAsync(
                    "warehouse.created.v1",
                    new WarehouseCreatedV1(
                        e.EventId,
                        e.OccurredAt,
                        e.TenantId,
                        e.WarehouseId,
                        e.WarehouseCode,
                        e.WarehouseName,
                        e.IsDefault),
                    cancellationToken: cancellationToken);
                break;
        }
    }
}
