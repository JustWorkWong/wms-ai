namespace WmsAi.AiGateway.Domain.MafSessions;

public sealed class MafSummarySnapshot
{
    private MafSummarySnapshot()
    {
    }

    public MafSummarySnapshot(
        Guid sessionId,
        string summaryText,
        string? evidenceRefsJson,
        string? messageRangeJson)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryText);

        Id = Guid.NewGuid();
        SessionId = sessionId;
        SummaryText = summaryText.Trim();
        EvidenceRefsJson = evidenceRefsJson?.Trim();
        MessageRangeJson = messageRangeJson?.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    public string SummaryText { get; private set; } = string.Empty;

    public string? EvidenceRefsJson { get; private set; }

    public string? MessageRangeJson { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
