namespace WmsAi.AiGateway.Application.Services;

public interface IMafPersistenceService
{
    Task<Guid> CreateSessionAsync(
        string tenantId,
        string warehouseId,
        string userId,
        string sessionType,
        string businessObjectType,
        string businessObjectId,
        CancellationToken cancellationToken = default);

    Task SaveMessageAsync(
        Guid sessionId,
        string role,
        string messageType,
        string? contentText,
        string? contentJson,
        CancellationToken cancellationToken = default);

    Task CreateCheckpointAsync(
        Guid sessionId,
        string checkpointName,
        Guid? workflowRunId,
        Guid? workflowStepRunId,
        int cursor,
        CancellationToken cancellationToken = default);

    Task<Guid> RestoreSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task SaveWorkflowRunAsync(
        Guid workflowRunId,
        string status,
        string? currentNode,
        string? resultJson,
        string? errorMessage,
        CancellationToken cancellationToken = default);

    Task SaveStepRunAsync(
        Guid workflowRunId,
        string nodeName,
        string stepKind,
        string status,
        string? message,
        string? payloadJson,
        string? errorMessage,
        CancellationToken cancellationToken = default);

    Task LogToolCallAsync(
        Guid? sessionId,
        Guid? workflowRunId,
        Guid? workflowStepRunId,
        string tenantId,
        string warehouseId,
        string userId,
        string callType,
        string toolName,
        string? inputJson,
        string? outputJson,
        string status,
        int durationMs,
        CancellationToken cancellationToken = default);

    Task LogModelCallAsync(
        Guid? sessionId,
        Guid? workflowRunId,
        Guid? workflowStepRunId,
        string agentProfileCode,
        string tenantId,
        string warehouseId,
        string userId,
        string providerCode,
        string modelName,
        string profileCode,
        int requestTokens,
        int responseTokens,
        int latencyMs,
        string finishReason,
        string? errorMessage,
        CancellationToken cancellationToken = default);
}
