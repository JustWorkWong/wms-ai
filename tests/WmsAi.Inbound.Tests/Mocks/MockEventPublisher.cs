using WmsAi.Inbound.Application.Qc;
using WmsAi.Inbound.Application.Receipts;

namespace WmsAi.Inbound.Tests.Mocks;

public sealed class MockEventPublisher : WmsAi.Inbound.Application.Receipts.IEventPublisher, WmsAi.Inbound.Application.Qc.IEventPublisher
{
    public Task PublishCollectedEventsAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
