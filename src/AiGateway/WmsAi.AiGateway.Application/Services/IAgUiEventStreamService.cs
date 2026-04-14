using System.Runtime.CompilerServices;
using System.Text.Json;
using WmsAi.AiGateway.Application.AgUi;

namespace WmsAi.AiGateway.Application.Services;

public interface IAgUiEventStreamService
{
    IAsyncEnumerable<AgUiEvent> StreamEventsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
