namespace WmsAi.AiGateway.Application.Maf;

public interface IMafWorkflowEngine
{
    Task<Guid> StartWorkflowAsync(
        string workflowName,
        string tenantId,
        string warehouseId,
        string userId,
        string agentProfileCode,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default);

    Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        Guid workflowRunId,
        CancellationToken cancellationToken = default);

    Task<WorkflowExecutionResult> ResumeWorkflowAsync(
        Guid workflowRunId,
        Dictionary<string, object> resumeContext,
        CancellationToken cancellationToken = default);

    Task PauseWorkflowAsync(
        Guid workflowRunId,
        string checkpointName,
        CancellationToken cancellationToken = default);
}

public sealed class WorkflowExecutionResult
{
    public Guid WorkflowRunId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? CurrentNode { get; init; }
    public Dictionary<string, object>? Result { get; init; }
    public string? ErrorMessage { get; init; }
}
