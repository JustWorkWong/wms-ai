using WmsAi.Platform.Application.Tenants;

namespace WmsAi.Platform.Tests.Mocks;

public sealed class MockEventPublisher : IEventPublisher
{
    public Task PublishCollectedEventsAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
