namespace WmsAi.AiGateway.Application.AgUi;

public sealed record AgUiErrorEvent : AgUiEvent
{
    public AgUiErrorEvent(
        string ErrorMessage,
        string? ErrorCode = null,
        DateTimeOffset? Timestamp = null)
        : base("error", Timestamp ?? DateTimeOffset.UtcNow)
    {
        this.ErrorMessage = ErrorMessage;
        this.ErrorCode = ErrorCode;
    }

    public string ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
}
