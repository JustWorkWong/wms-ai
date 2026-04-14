namespace WmsAi.AiGateway.Domain.MafSessions;

public sealed class MafCheckpoint
{
    private MafCheckpoint()
    {
    }

    public MafCheckpoint(
        Guid sessionId,
        string checkpointName,
        Guid? workflowRunId,
        Guid? workflowStepRunId,
        Guid? summarySnapshotId,
        int cursor)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointName);
        ArgumentOutOfRangeException.ThrowIfNegative(cursor);

        Id = Guid.NewGuid();
        SessionId = sessionId;
        CheckpointName = checkpointName.Trim();
        WorkflowRunId = workflowRunId;
        WorkflowStepRunId = workflowStepRunId;
        SummarySnapshotId = summarySnapshotId;
        Cursor = cursor;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    public string CheckpointName { get; private set; } = string.Empty;

    public Guid? WorkflowRunId { get; private set; }

    public Guid? WorkflowStepRunId { get; private set; }

    public Guid? SummarySnapshotId { get; private set; }

    public int Cursor { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
