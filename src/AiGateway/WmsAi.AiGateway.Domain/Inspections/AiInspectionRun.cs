using WmsAi.SharedKernel.Domain;

namespace WmsAi.AiGateway.Domain.Inspections;

public sealed class AiInspectionRun : WarehouseScopedAggregateRoot
{
    private readonly List<AiSuggestion> _suggestions = [];

    private AiInspectionRun()
    {
    }

    public AiInspectionRun(
        string tenantId,
        string warehouseId,
        Guid qcTaskId,
        Guid workflowRunId,
        Guid? sessionId,
        string agentProfileCode,
        string modelProfileCode,
        string? modelConfigSnapshotJson)
        : base(tenantId, warehouseId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(qcTaskId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(workflowRunId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentProfileCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelProfileCode);

        QcTaskId = qcTaskId;
        WorkflowRunId = workflowRunId;
        SessionId = sessionId;
        AgentProfileCode = agentProfileCode.Trim();
        ModelProfileCode = modelProfileCode.Trim();
        ModelConfigSnapshotJson = modelConfigSnapshotJson?.Trim();
        Status = InspectionStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid QcTaskId { get; private set; }

    public Guid WorkflowRunId { get; private set; }

    public Guid? SessionId { get; private set; }

    public string AgentProfileCode { get; private set; } = string.Empty;

    public string ModelProfileCode { get; private set; } = string.Empty;

    public string? ModelConfigSnapshotJson { get; private set; }

    public InspectionStatus Status { get; private set; }

    public string? ResultSummary { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<AiSuggestion> Suggestions => _suggestions.AsReadOnly();

    public void Start()
    {
        if (Status != InspectionStatus.Pending)
        {
            throw new InvalidOperationException("Can only start a pending inspection");
        }

        Status = InspectionStatus.Running;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddSuggestion(
        SuggestionType suggestionType,
        string reasoning,
        decimal confidenceScore,
        string? structuredDataJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasoning);

        var suggestion = new AiSuggestion(
            Id,
            suggestionType,
            reasoning,
            confidenceScore,
            structuredDataJson);

        _suggestions.Add(suggestion);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(string? resultSummary)
    {
        Status = InspectionStatus.Completed;
        ResultSummary = resultSummary?.Trim();
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Fail()
    {
        Status = InspectionStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void EscalateToManualReview()
    {
        Status = InspectionStatus.ManualReview;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
