namespace WmsAi.AiGateway.Domain.Workflows;

public sealed class MafWorkflowStepRun
{
    private MafWorkflowStepRun()
    {
    }

    public MafWorkflowStepRun(
        Guid workflowRunId,
        int sequence,
        string nodeName,
        string? agentProfileCode,
        StepKind stepKind,
        string? inputJson,
        string? payloadJson,
        string? evidenceJson)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(workflowRunId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequence);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);

        Id = Guid.NewGuid();
        WorkflowRunId = workflowRunId;
        Sequence = sequence;
        NodeName = nodeName.Trim();
        AgentProfileCode = agentProfileCode?.Trim();
        StepKind = stepKind;
        Status = StepStatus.Pending;
        AttemptCount = 0;
        InputJson = inputJson?.Trim();
        PayloadJson = payloadJson?.Trim();
        EvidenceJson = evidenceJson?.Trim();
    }

    public Guid Id { get; private set; }

    public Guid WorkflowRunId { get; private set; }

    public int Sequence { get; private set; }

    public string NodeName { get; private set; } = string.Empty;

    public string? AgentProfileCode { get; private set; }

    public StepKind StepKind { get; private set; }

    public StepStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public string? Message { get; private set; }

    public string? InputJson { get; private set; }

    public string? PayloadJson { get; private set; }

    public string? EvidenceJson { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public void Start()
    {
        Status = StepStatus.Running;
        AttemptCount++;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(string? message, string? payloadJson)
    {
        Status = StepStatus.Completed;
        Message = message?.Trim();
        PayloadJson = payloadJson?.Trim();
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        Status = StepStatus.Failed;
        ErrorMessage = errorMessage.Trim();
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Skip(string? message)
    {
        Status = StepStatus.Skipped;
        Message = message?.Trim();
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
