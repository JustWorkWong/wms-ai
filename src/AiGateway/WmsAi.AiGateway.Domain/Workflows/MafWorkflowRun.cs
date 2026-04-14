using WmsAi.SharedKernel.Domain;

namespace WmsAi.AiGateway.Domain.Workflows;

public sealed class MafWorkflowRun : WarehouseScopedAggregateRoot
{
    private readonly List<MafWorkflowStepRun> _stepRuns = [];

    private MafWorkflowRun()
    {
    }

    public MafWorkflowRun(
        string tenantId,
        string warehouseId,
        string workflowName,
        string agentProfileCode,
        string requestedBy,
        string? membershipId,
        string? userInput,
        string? routingJson,
        string? executionContextJson)
        : base(tenantId, warehouseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowName);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentProfileCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBy);

        WorkflowName = workflowName.Trim();
        AgentProfileCode = agentProfileCode.Trim();
        Status = WorkflowStatus.Pending;
        RequestedBy = requestedBy.Trim();
        MembershipId = membershipId?.Trim();
        UserInput = userInput?.Trim();
        RoutingJson = routingJson?.Trim();
        ExecutionContextJson = executionContextJson?.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string WorkflowName { get; private set; } = string.Empty;

    public string AgentProfileCode { get; private set; } = string.Empty;

    public WorkflowStatus Status { get; private set; }

    public string RequestedBy { get; private set; } = string.Empty;

    public string? MembershipId { get; private set; }

    public string? UserInput { get; private set; }

    public string? RoutingJson { get; private set; }

    public string? ExecutionContextJson { get; private set; }

    public string? ResultJson { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? CurrentNode { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<MafWorkflowStepRun> StepRuns => _stepRuns.AsReadOnly();

    public void Start()
    {
        if (Status != WorkflowStatus.Pending)
        {
            throw new InvalidOperationException("Can only start a pending workflow");
        }

        Status = WorkflowStatus.Running;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateCurrentNode(string nodeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        CurrentNode = nodeName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddStepRun(
        string nodeName,
        string? agentProfileCode,
        StepKind stepKind,
        string? inputJson,
        string? payloadJson,
        string? evidenceJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);

        var sequence = _stepRuns.Count + 1;
        var stepRun = new MafWorkflowStepRun(
            Id,
            sequence,
            nodeName,
            agentProfileCode,
            stepKind,
            inputJson,
            payloadJson,
            evidenceJson);

        _stepRuns.Add(stepRun);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Pause()
    {
        Status = WorkflowStatus.Paused;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Resume()
    {
        if (Status != WorkflowStatus.Paused)
        {
            throw new InvalidOperationException("Can only resume a paused workflow");
        }

        Status = WorkflowStatus.Running;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(string? resultJson)
    {
        Status = WorkflowStatus.Completed;
        ResultJson = resultJson?.Trim();
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        Status = WorkflowStatus.Failed;
        ErrorMessage = errorMessage.Trim();
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        Status = WorkflowStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
