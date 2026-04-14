namespace WmsAi.AiGateway.Application.AgUi;

public sealed record AgUiMessageEvent : AgUiEvent
{
    public AgUiMessageEvent(
        string Content,
        string? Role = null,
        DateTimeOffset? Timestamp = null)
        : base("message", Timestamp ?? DateTimeOffset.UtcNow)
    {
        this.Content = Content;
        this.Role = Role;
    }

    public string Content { get; init; }
    public string? Role { get; init; }
}
