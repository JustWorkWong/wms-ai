using System.Text.Json;
using WmsAi.AiGateway.Application.Services;
using WmsAi.AiGateway.Domain.Inspections;
using WmsAi.AiGateway.Domain.MafSessions;
using WmsAi.AiGateway.Domain.Workflows;
using WmsAi.AiGateway.Infrastructure.Persistence;

namespace WmsAi.AiGateway.Infrastructure.Services;

public sealed class MafPersistenceService(
    IMafSessionRepository sessionRepository,
    IMafWorkflowRunRepository workflowRunRepository,
    AiDbContext context) : IMafPersistenceService
{
    public async Task<Guid> CreateSessionAsync(
        string tenantId,
        string warehouseId,
        string userId,
        string sessionType,
        string businessObjectType,
        string businessObjectId,
        CancellationToken cancellationToken = default)
    {
        var session = new MafSession(
            tenantId,
            warehouseId,
            userId,
            sessionType,
            businessObjectType,
            businessObjectId);

        await sessionRepository.AddAsync(session, cancellationToken);
        return session.Id;
    }

    public async Task SaveMessageAsync(
        Guid sessionId,
        string role,
        string messageType,
        string? contentText,
        string? contentJson,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        var messageRole = Enum.Parse<MessageRole>(role, ignoreCase: true);
        session.AddMessage(messageRole, messageType, contentText, contentJson);
        await sessionRepository.UpdateAsync(session, cancellationToken);
    }

    public async Task CreateCheckpointAsync(
        Guid sessionId,
        string checkpointName,
        Guid? workflowRunId,
        Guid? workflowStepRunId,
        int cursor,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        session.CreateCheckpoint(checkpointName, workflowRunId, workflowStepRunId, null, cursor);
        await sessionRepository.UpdateAsync(session, cancellationToken);
    }

    public async Task<Guid> RestoreSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        session.Resume();
        await sessionRepository.UpdateAsync(session, cancellationToken);
        return session.Id;
    }

    public async Task SaveWorkflowRunAsync(
        Guid workflowRunId,
        string status,
        string? currentNode,
        string? resultJson,
        string? errorMessage,
        CancellationToken cancellationToken = default)
    {
        var workflowRun = await workflowRunRepository.GetByIdAsync(workflowRunId, cancellationToken);
        if (workflowRun == null)
        {
            throw new InvalidOperationException($"Workflow run {workflowRunId} not found");
        }

        if (currentNode != null)
        {
            workflowRun.UpdateCurrentNode(currentNode);
        }

        var workflowStatus = Enum.Parse<WorkflowStatus>(status, ignoreCase: true);
        switch (workflowStatus)
        {
            case WorkflowStatus.Completed:
                workflowRun.Complete(resultJson);
                break;
            case WorkflowStatus.Failed:
                workflowRun.Fail(errorMessage ?? "Unknown error");
                break;
            case WorkflowStatus.Paused:
                workflowRun.Pause();
                break;
            case WorkflowStatus.Running:
                workflowRun.Start();
                break;
        }

        await workflowRunRepository.UpdateAsync(workflowRun, cancellationToken);
    }

    public async Task SaveStepRunAsync(
        Guid workflowRunId,
        string nodeName,
        string stepKind,
        string status,
        string? message,
        string? payloadJson,
        string? errorMessage,
        CancellationToken cancellationToken = default)
    {
        var workflowRun = await workflowRunRepository.GetByIdAsync(workflowRunId, cancellationToken);
        if (workflowRun == null)
        {
            throw new InvalidOperationException($"Workflow run {workflowRunId} not found");
        }

        var stepKindEnum = Enum.Parse<StepKind>(stepKind, ignoreCase: true);
        workflowRun.AddStepRun(nodeName, null, stepKindEnum, null, payloadJson, null);

        var stepRun = workflowRun.StepRuns.Last();
        stepRun.Start();

        var stepStatus = Enum.Parse<StepStatus>(status, ignoreCase: true);
        switch (stepStatus)
        {
            case StepStatus.Completed:
                stepRun.Complete(message, payloadJson);
                break;
            case StepStatus.Failed:
                stepRun.Fail(errorMessage ?? "Unknown error");
                break;
            case StepStatus.Skipped:
                stepRun.Skip(message);
                break;
        }

        await workflowRunRepository.UpdateAsync(workflowRun, cancellationToken);
    }

    public async Task LogToolCallAsync(
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
        CancellationToken cancellationToken = default)
    {
        var toolCallLog = new MafToolCallLog(
            sessionId,
            workflowRunId,
            workflowStepRunId,
            tenantId,
            warehouseId,
            userId,
            null,
            callType,
            toolName,
            inputJson,
            outputJson,
            status,
            durationMs);

        await context.MafToolCallLogs.AddAsync(toolCallLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task LogModelCallAsync(
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
        CancellationToken cancellationToken = default)
    {
        var modelCallLog = new MafModelCallLog(
            sessionId,
            workflowRunId,
            workflowStepRunId,
            agentProfileCode,
            tenantId,
            warehouseId,
            userId,
            providerCode,
            modelName,
            profileCode,
            requestTokens,
            responseTokens,
            requestTokens + responseTokens,
            latencyMs,
            finishReason,
            null,
            null,
            errorMessage);

        await context.MafModelCallLogs.AddAsync(modelCallLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
