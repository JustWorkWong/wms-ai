namespace WmsAi.AiGateway.Application.AgUi;

public sealed record SessionCreateResponse(
    Guid SessionId,
    string Status,
    DateTimeOffset CreatedAt);
