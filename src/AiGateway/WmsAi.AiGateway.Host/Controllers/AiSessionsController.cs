using Microsoft.AspNetCore.Mvc;
using WmsAi.AiGateway.Application.AgUi;
using WmsAi.AiGateway.Application.Services;
using WmsAi.AiGateway.Domain.MafSessions;

namespace WmsAi.AiGateway.Host.Controllers;

[ApiController]
[Route("api/ai/sessions")]
public sealed class AiSessionsController : ControllerBase
{
    private readonly IMafSessionRepository _sessionRepository;
    private readonly IMafPersistenceService _persistenceService;
    private readonly IAgUiEventStreamService _eventStreamService;

    public AiSessionsController(
        IMafSessionRepository sessionRepository,
        IMafPersistenceService persistenceService,
        IAgUiEventStreamService eventStreamService)
    {
        _sessionRepository = sessionRepository;
        _persistenceService = persistenceService;
        _eventStreamService = eventStreamService;
    }

    [HttpPost]
    public async Task<ActionResult<SessionCreateResponse>> CreateSession(
        [FromBody] SessionCreateRequest request,
        CancellationToken cancellationToken)
    {
        var sessionId = await _persistenceService.CreateSessionAsync(
            request.TenantId,
            request.WarehouseId,
            request.UserId,
            request.SceneCode ?? "default",
            request.BusinessObjectType,
            request.BusinessObjectId,
            cancellationToken);

        return Ok(new SessionCreateResponse(
            sessionId,
            "Active",
            DateTimeOffset.UtcNow));
    }

    [HttpGet("{sessionId:guid}/stream")]
    public async Task StreamEvents(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        await foreach (var evt in _eventStreamService.StreamEventsAsync(sessionId, cancellationToken))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(evt);
            await Response.WriteAsync($"event: {evt.Type}\n", cancellationToken);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    [HttpPost("{sessionId:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid sessionId,
        [FromBody] MessageSendRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
            return NotFound(new { error = "Session not found" });

        await _persistenceService.SaveMessageAsync(
            sessionId,
            "user",
            "text",
            request.Content,
            null,
            cancellationToken);

        // TODO: Trigger workflow continuation

        return Accepted();
    }

    [HttpPost("{sessionId:guid}/resume")]
    public async Task<IActionResult> ResumeSession(
        Guid sessionId,
        [FromBody] SessionResumeRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
            return NotFound(new { error = "Session not found" });

        if (session.Status != SessionStatus.Paused)
            return BadRequest(new { error = "Session is not paused" });

        session.Resume();
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        // TODO: Restore workflow state and continue execution

        return Ok(new { sessionId, status = "Active" });
    }
}
