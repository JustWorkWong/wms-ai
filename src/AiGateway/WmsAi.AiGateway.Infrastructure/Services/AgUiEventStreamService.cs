using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using WmsAi.AiGateway.Application.AgUi;
using WmsAi.AiGateway.Application.Services;
using WmsAi.AiGateway.Domain.MafSessions;
using WmsAi.AiGateway.Domain.Workflows;

namespace WmsAi.AiGateway.Infrastructure.Services;

public sealed class AgUiEventStreamService : IAgUiEventStreamService
{
    private readonly IMafSessionRepository _sessionRepository;
    private readonly IMafWorkflowRunRepository _workflowRepository;
    private readonly ConcurrentDictionary<Guid, Channel<AgUiEvent>> _sessionChannels = new();

    public AgUiEventStreamService(
        IMafSessionRepository sessionRepository,
        IMafWorkflowRunRepository workflowRepository)
    {
        _sessionRepository = sessionRepository;
        _workflowRepository = workflowRepository;
    }

    public async IAsyncEnumerable<AgUiEvent> StreamEventsAsync(
        Guid sessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            yield return new AgUiErrorEvent("Session not found", "SESSION_NOT_FOUND");
            yield break;
        }

        var channel = _sessionChannels.GetOrAdd(sessionId, _ =>
            Channel.CreateUnbounded<AgUiEvent>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            }));

        yield return new AgUiStatusEvent("connected", CurrentNode: "Initializing");

        // Send heartbeat every 30 seconds
        using var heartbeatTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        var heartbeatTask = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested && await heartbeatTimer.WaitForNextTickAsync(cancellationToken))
            {
                await channel.Writer.WriteAsync(
                    new AgUiStatusEvent("heartbeat"),
                    cancellationToken);
            }
        }, cancellationToken);

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return evt;

                if (evt is AgUiStatusEvent statusEvent &&
                    (statusEvent.Status == "completed" || statusEvent.Status == "failed"))
                {
                    break;
                }
            }
        }
        finally
        {
            _sessionChannels.TryRemove(sessionId, out _);
            await heartbeatTask;
        }
    }

    public async Task PublishEventAsync(Guid sessionId, AgUiEvent evt, CancellationToken cancellationToken = default)
    {
        if (_sessionChannels.TryGetValue(sessionId, out var channel))
        {
            await channel.Writer.WriteAsync(evt, cancellationToken);
        }
    }
}
