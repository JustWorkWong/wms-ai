namespace WmsAi.AiGateway.Domain.Inspections;

public sealed class MafToolCallLog
{
    private MafToolCallLog()
    {
    }

    public MafToolCallLog(
        Guid? sessionId,
        Guid? workflowRunId,
        Guid? workflowStepRunId,
        string tenantId,
        string warehouseId,
        string userId,
        string? membershipId,
        string callType,
        string toolName,
        string? inputJson,
        string? outputJson,
        string status,
        int durationMs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(warehouseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(callType);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        Id = Guid.NewGuid();
        SessionId = sessionId;
        WorkflowRunId = workflowRunId;
        WorkflowStepRunId = workflowStepRunId;
        TenantId = tenantId.Trim();
        WarehouseId = warehouseId.Trim();
        UserId = userId.Trim();
        MembershipId = membershipId?.Trim();
        CallType = callType.Trim();
        ToolName = toolName.Trim();
        InputJson = inputJson?.Trim();
        OutputJson = outputJson?.Trim();
        Status = status.Trim();
        DurationMs = durationMs;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid? SessionId { get; private set; }

    public Guid? WorkflowRunId { get; private set; }

    public Guid? WorkflowStepRunId { get; private set; }

    public string TenantId { get; private set; } = string.Empty;

    public string WarehouseId { get; private set; } = string.Empty;

    public string UserId { get; private set; } = string.Empty;

    public string? MembershipId { get; private set; }

    public string CallType { get; private set; } = string.Empty;

    public string ToolName { get; private set; } = string.Empty;

    public string? InputJson { get; private set; }

    public string? OutputJson { get; private set; }

    public string Status { get; private set; } = string.Empty;

    public int DurationMs { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
