namespace WmsAi.AiGateway.Domain.MafSessions;

public sealed class MafMessage
{
    private MafMessage()
    {
    }

    public MafMessage(
        Guid sessionId,
        int sequence,
        MessageRole role,
        string messageType,
        string? contentText,
        string? contentJson)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequence);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);

        Id = Guid.NewGuid();
        SessionId = sessionId;
        Sequence = sequence;
        Role = role;
        MessageType = messageType.Trim();
        ContentText = contentText?.Trim();
        ContentJson = contentJson?.Trim();
        IsSummary = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    public int Sequence { get; private set; }

    public MessageRole Role { get; private set; }

    public string MessageType { get; private set; } = string.Empty;

    public string? ContentText { get; private set; }

    public string? ContentJson { get; private set; }

    public bool IsSummary { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void MarkAsSummary()
    {
        IsSummary = true;
    }
}
