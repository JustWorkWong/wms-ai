namespace WmsAi.AiGateway.Application.AgUi;

public sealed record AgUiStatusEvent : AgUiEvent
{
    public AgUiStatusEvent(
        string Status,
        double? Confidence = null,
        string? CurrentNode = null,
        DateTimeOffset? Timestamp = null)
        : base("status", Timestamp ?? DateTimeOffset.UtcNow)
    {
        this.Status = Status;
        this.Confidence = Confidence;
        this.CurrentNode = CurrentNode;
    }

    public string Status { get; init; }
    public double? Confidence { get; init; }
    public string? CurrentNode { get; init; }
}
