using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Agents.AI.Workflows.InProc;
using Microsoft.AspNetCore.Mvc;
using WmsAi.AiGateway.Application.Workflows;
using WmsAi.AiGateway.Domain.Workflows;
using WmsAi.AiGateway.Infrastructure.Workflows;

namespace WmsAi.AiGateway.Host.Endpoints;

/// <summary>
/// Workflow 恢复 API - 处理人工审批后的 Workflow 恢复
/// </summary>
public static class WorkflowEndpoints
{
    public static void MapWorkflowEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ai/workflows")
            .WithTags("Workflows")
            .RequireAuthorization(); // 添加认证要求

        group.MapPost("/{workflowId:guid}/resume", ResumeWorkflow)
            .WithName("ResumeWorkflow")
            .WithSummary("恢复暂停的 Workflow（人工审批后）")
            .Produces<WorkflowResumeResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/{workflowId:guid}/status", GetWorkflowStatus)
            .WithName("GetWorkflowStatus")
            .WithSummary("查询 Workflow 当前状态")
            .Produces<WorkflowStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// 恢复暂停的 Workflow（人工审批后）
    /// </summary>
    private static async Task<IResult> ResumeWorkflow(
        [FromRoute] Guid workflowId,
        [FromBody] ApprovalResponse approvalResponse,
        [FromServices] IMafWorkflowRunRepository workflowRunRepository,
        [FromServices] ICheckpointStore<JsonElement> checkpointStore,
        [FromServices] QcInspectionWorkflowFactory workflowFactory,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Resuming workflow {WorkflowId} with approval decision: {Decision}",
                workflowId, approvalResponse.Decision);

            // 1. 查找 Workflow Run
            var workflowRun = await workflowRunRepository.GetByIdAsync(workflowId, cancellationToken);
            if (workflowRun == null)
            {
                logger.LogWarning("Workflow {WorkflowId} not found", workflowId);
                return Results.NotFound(new { Error = $"Workflow {workflowId} not found" });
            }

            // 2. 验证 Workflow 状态
            if (workflowRun.Status != WorkflowStatus.Paused)
            {
                logger.LogWarning(
                    "Workflow {WorkflowId} is not paused, current status: {Status}",
                    workflowId, workflowRun.Status);
                return Results.BadRequest(new
                {
                    Error = $"Workflow is not paused, current status: {workflowRun.Status}"
                });
            }

            // 3. 从 CheckpointStore 加载最新的 Checkpoint
            var sessionId = workflowId.ToString();
            var checkpointIndex = await checkpointStore.RetrieveIndexAsync(sessionId, null);
            var latestCheckpoint = checkpointIndex.OrderByDescending(c => c.CheckpointId).FirstOrDefault();

            if (latestCheckpoint == null)
            {
                logger.LogWarning("No checkpoint found for workflow {WorkflowId}", workflowId);
                return Results.BadRequest(new { Error = "No checkpoint found for this workflow" });
            }

            // 4. 构建 CheckpointInfo 和 CheckpointManager
            var checkpointInfo = new CheckpointInfo(
                sessionId: sessionId,
                checkpointId: latestCheckpoint.CheckpointId);

            var checkpointManager = CheckpointManager.CreateJson(checkpointStore, new JsonSerializerOptions());

            logger.LogInformation(
                "Loaded checkpoint {CheckpointId} for workflow {WorkflowId}",
                latestCheckpoint.CheckpointId, workflowId);

            // 5. 重新构建 Workflow
            var workflow = await workflowFactory.BuildAsync(cancellationToken);

            // 6. 更新 Workflow Run 状态为 Running
            workflowRun.Resume();
            await workflowRunRepository.UpdateAsync(workflowRun, cancellationToken);

            logger.LogInformation(
                "Resuming workflow from checkpoint {CheckpointId}",
                latestCheckpoint.CheckpointId);

            // 7. 使用 ResumeStreamingAsync 恢复 Workflow
            var streamingRun = await InProcessExecution.ResumeStreamingAsync(
                workflow,
                checkpointInfo,
                checkpointManager,
                cancellationToken);

            // 8. 发送人工审批响应
            // 需要从 RequestInfoEvent 中获取 ExternalRequest 信息
            // 这里暂时使用简化的方式，假设 RequestId 存储在数据库中
            var externalResponse = new ExternalResponse(
                PortInfo: new RequestPortInfo(
                    RequestType: new TypeId(typeof(ApprovalRequest)),
                    ResponseType: new TypeId(typeof(ApprovalResponse)),
                    PortId: "HumanApproval"),
                RequestId: "approval-request",
                Data: new PortableValue(approvalResponse));

            await streamingRun.SendResponseAsync(externalResponse);

            logger.LogInformation(
                "Sent approval response to workflow {WorkflowId}: {Decision}",
                workflowId, approvalResponse.Decision);

            // 9. 监听事件流直到完成或再次暂停
            await foreach (var evt in streamingRun.WatchStreamAsync(cancellationToken))
            {
                logger.LogInformation(
                    "Workflow {WorkflowId} event: {EventType}",
                    workflowId, evt.GetType().Name);

                switch (evt)
                {
                    case ExecutorCompletedEvent executorCompleted:
                        logger.LogInformation(
                            "Executor completed: {ExecutorId}",
                            executorCompleted.ExecutorId);
                        break;

                    case RequestInfoEvent requestInfo:
                        // Workflow 再次暂停，等待人工审批
                        logger.LogInformation(
                            "Workflow {WorkflowId} paused again, waiting for approval",
                            workflowId);

                        workflowRun.Pause();
                        await workflowRunRepository.UpdateAsync(workflowRun, cancellationToken);

                        return Results.Ok(new WorkflowResumeResponse
                        {
                            WorkflowId = workflowId,
                            Status = "Paused",
                            Message = "Workflow paused again, waiting for another approval",
                            ApprovalDecision = approvalResponse.Decision,
                            ResumedAt = DateTimeOffset.UtcNow
                        });

                    case WorkflowOutputEvent outputEvent:
                        // Workflow 完成
                        logger.LogInformation(
                            "Workflow {WorkflowId} completed successfully",
                            workflowId);

                        workflowRun.Complete(JsonSerializer.Serialize(outputEvent.Data));
                        await workflowRunRepository.UpdateAsync(workflowRun, cancellationToken);

                        return Results.Ok(new WorkflowResumeResponse
                        {
                            WorkflowId = workflowId,
                            Status = "Completed",
                            Message = "Workflow completed successfully",
                            ApprovalDecision = approvalResponse.Decision,
                            ResumedAt = DateTimeOffset.UtcNow
                        });

                    case ExecutorFailedEvent failedEvent:
                        // Workflow 失败
                        var exception = failedEvent.Data as Exception;
                        logger.LogError(
                            exception,
                            "Workflow {WorkflowId} failed",
                            workflowId);

                        workflowRun.Fail(exception?.Message ?? "Unknown error");
                        await workflowRunRepository.UpdateAsync(workflowRun, cancellationToken);

                        return Results.Ok(new WorkflowResumeResponse
                        {
                            WorkflowId = workflowId,
                            Status = "Failed",
                            Message = $"Workflow failed: {exception?.Message}",
                            ApprovalDecision = approvalResponse.Decision,
                            ResumedAt = DateTimeOffset.UtcNow
                        });
                }
            }

            // 如果事件流结束但没有明确的完成/失败事件
            logger.LogWarning(
                "Workflow {WorkflowId} event stream ended without completion event",
                workflowId);

            return Results.Ok(new WorkflowResumeResponse
            {
                WorkflowId = workflowId,
                Status = "Running",
                Message = "Workflow resumed and running",
                ApprovalDecision = approvalResponse.Decision,
                ResumedAt = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resume workflow {WorkflowId}", workflowId);
            return Results.Problem($"Failed to resume workflow: {ex.Message}");
        }
    }

    /// <summary>
    /// 查询 Workflow 当前状态
    /// </summary>
    private static async Task<IResult> GetWorkflowStatus(
        [FromRoute] Guid workflowId,
        [FromServices] IMafWorkflowRunRepository workflowRunRepository,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Querying status for workflow {WorkflowId}", workflowId);

            var workflowRun = await workflowRunRepository.GetByIdAsync(workflowId, cancellationToken);
            if (workflowRun == null)
            {
                logger.LogWarning("Workflow {WorkflowId} not found", workflowId);
                return Results.NotFound(new { Error = $"Workflow {workflowId} not found" });
            }

            var response = new WorkflowStatusResponse
            {
                WorkflowId = workflowId,
                WorkflowName = workflowRun.WorkflowName,
                Status = workflowRun.Status.ToString(),
                CurrentNode = workflowRun.CurrentNode,
                IsWaitingForApproval = workflowRun.Status == WorkflowStatus.Paused,
                CreatedAt = workflowRun.CreatedAt,
                UpdatedAt = workflowRun.UpdatedAt,
                CompletedAt = workflowRun.CompletedAt,
                ErrorMessage = workflowRun.ErrorMessage,
                StepCount = workflowRun.StepRuns.Count
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get workflow status for {WorkflowId}", workflowId);
            return Results.Problem($"Failed to get workflow status: {ex.Message}");
        }
    }
}

/// <summary>
/// Workflow 恢复响应
/// </summary>
public sealed class WorkflowResumeResponse
{
    public Guid WorkflowId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string ApprovalDecision { get; init; } = string.Empty;
    public DateTimeOffset ResumedAt { get; init; }
}

/// <summary>
/// Workflow 状态响应
/// </summary>
public sealed class WorkflowStatusResponse
{
    public Guid WorkflowId { get; init; }
    public string WorkflowName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? CurrentNode { get; init; }
    public bool IsWaitingForApproval { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public int StepCount { get; init; }
}
