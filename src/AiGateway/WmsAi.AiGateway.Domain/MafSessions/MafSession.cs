using WmsAi.SharedKernel.Domain;

namespace WmsAi.AiGateway.Domain.MafSessions;

public sealed class MafSession : WarehouseScopedAggregateRoot
{
    private readonly List<MafMessage> _messages = [];
    private readonly List<MafCheckpoint> _checkpoints = [];
    private readonly List<MafSummarySnapshot> _summarySnapshots = [];

    private MafSession()
    {
    }

    public MafSession(
        string tenantId,
        string warehouseId,
        string userId,
        string sessionType,
        string businessObjectType,
        string businessObjectId)
        : base(tenantId, warehouseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(businessObjectType);
        ArgumentException.ThrowIfNullOrWhiteSpace(businessObjectId);

        UserId = userId.Trim();
        SessionType = sessionType.Trim();
        BusinessObjectType = businessObjectType.Trim();
        BusinessObjectId = businessObjectId.Trim();
        Status = SessionStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string UserId { get; private set; } = string.Empty;

    public string SessionType { get; private set; } = string.Empty;

    public string BusinessObjectType { get; private set; } = string.Empty;

    public string BusinessObjectId { get; private set; } = string.Empty;

    public SessionStatus Status { get; private set; }

    public string? AgentSessionJson { get; private set; }

    public Guid? LastCheckpointId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<MafMessage> Messages => _messages.AsReadOnly();

    public IReadOnlyCollection<MafCheckpoint> Checkpoints => _checkpoints.AsReadOnly();

    public IReadOnlyCollection<MafSummarySnapshot> SummarySnapshots => _summarySnapshots.AsReadOnly();

    public void UpdateAgentSessionState(string agentSessionJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentSessionJson);
        AgentSessionJson = agentSessionJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddMessage(MessageRole role, string messageType, string? contentText, string? contentJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);

        var sequence = _messages.Count + 1;
        var message = new MafMessage(Id, sequence, role, messageType, contentText, contentJson);
        _messages.Add(message);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CreateCheckpoint(string checkpointName, Guid? workflowRunId, Guid? workflowStepRunId, Guid? summarySnapshotId, int cursor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointName);

        var checkpoint = new MafCheckpoint(Id, checkpointName, workflowRunId, workflowStepRunId, summarySnapshotId, cursor);
        _checkpoints.Add(checkpoint);
        LastCheckpointId = checkpoint.Id;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CreateSummarySnapshot(string summaryText, string? evidenceRefsJson, string? messageRangeJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryText);

        var snapshot = new MafSummarySnapshot(Id, summaryText, evidenceRefsJson, messageRangeJson);
        _summarySnapshots.Add(snapshot);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Pause()
    {
        Status = SessionStatus.Paused;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Resume()
    {
        if (Status != SessionStatus.Paused)
        {
            throw new InvalidOperationException("Can only resume a paused session");
        }

        Status = SessionStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        Status = SessionStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Fail()
    {
        Status = SessionStatus.Failed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
