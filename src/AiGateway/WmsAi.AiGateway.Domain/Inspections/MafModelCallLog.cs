namespace WmsAi.AiGateway.Domain.Inspections;

public sealed class MafModelCallLog
{
    private MafModelCallLog()
    {
    }

    public MafModelCallLog(
        Guid? sessionId,
        Guid? workflowRunId,
        Guid? workflowStepRunId,
        string? agentProfileCode,
        string tenantId,
        string warehouseId,
        string userId,
        string providerCode,
        string modelName,
        string profileCode,
        int requestTokens,
        int responseTokens,
        int totalTokens,
        int latencyMs,
        string finishReason,
        string? requestMetaJson,
        string? responseMetaJson,
        string? errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(warehouseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(finishReason);

        Id = Guid.NewGuid();
        SessionId = sessionId;
        WorkflowRunId = workflowRunId;
        WorkflowStepRunId = workflowStepRunId;
        AgentProfileCode = agentProfileCode?.Trim();
        TenantId = tenantId.Trim();
        WarehouseId = warehouseId.Trim();
        UserId = userId.Trim();
        ProviderCode = providerCode.Trim();
        ModelName = modelName.Trim();
        ProfileCode = profileCode.Trim();
        RequestTokens = requestTokens;
        ResponseTokens = responseTokens;
        TotalTokens = totalTokens;
        LatencyMs = latencyMs;
        FinishReason = finishReason.Trim();
        RequestMetaJson = requestMetaJson?.Trim();
        ResponseMetaJson = responseMetaJson?.Trim();
        ErrorMessage = errorMessage?.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid? SessionId { get; private set; }

    public Guid? WorkflowRunId { get; private set; }

    public Guid? WorkflowStepRunId { get; private set; }

    public string? AgentProfileCode { get; private set; }

    public string TenantId { get; private set; } = string.Empty;

    public string WarehouseId { get; private set; } = string.Empty;

    public string UserId { get; private set; } = string.Empty;

    public string ProviderCode { get; private set; } = string.Empty;

    public string ModelName { get; private set; } = string.Empty;

    public string ProfileCode { get; private set; } = string.Empty;

    public int RequestTokens { get; private set; }

    public int ResponseTokens { get; private set; }

    public int TotalTokens { get; private set; }

    public int LatencyMs { get; private set; }

    public string FinishReason { get; private set; } = string.Empty;

    public string? RequestMetaJson { get; private set; }

    public string? ResponseMetaJson { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
