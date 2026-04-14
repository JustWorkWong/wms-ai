namespace WmsAi.AiGateway.Application.AgUi;

public sealed record AgUiToolCallEvent : AgUiEvent
{
    public AgUiToolCallEvent(
        string ToolName,
        string Status,
        string? Result = null,
        DateTimeOffset? Timestamp = null)
        : base("tool_call", Timestamp ?? DateTimeOffset.UtcNow)
    {
        this.ToolName = ToolName;
        this.Status = Status;
        this.Result = Result;
    }

    public string ToolName { get; init; }
    public string Status { get; init; }
    public string? Result { get; init; }
}
