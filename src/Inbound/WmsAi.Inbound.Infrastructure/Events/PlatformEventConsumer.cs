using DotNetCore.CAP;
using Microsoft.Extensions.Logging;
using WmsAi.Contracts.Events;

namespace WmsAi.Inbound.Infrastructure.Events;

public sealed class PlatformEventConsumer(ILogger<PlatformEventConsumer> logger)
{
    [CapSubscribe("tenant.created.v1")]
    public async Task HandleTenantCreated(TenantCreatedV1 @event)
    {
        logger.LogInformation(
            "Received TenantCreatedV1 event: EventId={EventId}, TenantId={TenantId}, TenantCode={TenantCode}",
            @event.EventId,
            @event.TenantId,
            @event.TenantCode);

        // Placeholder: Initialize business space for tenant
        // In a real implementation, this might create default business records
        await Task.CompletedTask;
    }

    [CapSubscribe("warehouse.created.v1")]
    public async Task HandleWarehouseCreated(WarehouseCreatedV1 @event)
    {
        logger.LogInformation(
            "Received WarehouseCreatedV1 event: EventId={EventId}, WarehouseId={WarehouseId}, WarehouseCode={WarehouseCode}",
            @event.EventId,
            @event.WarehouseId,
            @event.WarehouseCode);

        // Placeholder: Initialize warehouse business space
        // In a real implementation, this might create default warehouse configurations
        await Task.CompletedTask;
    }
}
