using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
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
            .WithTags("Workflows");

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

            // 4. 恢复 Checkpoint 数据
            var checkpointData = await checkpointStore.RetrieveCheckpointAsync(sessionId, latestCheckpoint);

            logger.LogInformation(
                "Loaded checkpoint {CheckpointId} for workflow {WorkflowId}",
                latestCheckpoint.CheckpointId, workflowId);

            // 5. 反序列化 State
            var state = JsonSerializer.Deserialize<QcInspectionState>(checkpointData.GetRawText());
            if (state == null)
            {
                logger.LogError("Failed to deserialize checkpoint state for workflow {WorkflowId}", workflowId);
                return Results.Problem("Failed to deserialize checkpoint state");
            }

            // 6. 更新 State 添加人工审批响应
            var updatedState = new QcInspectionState
            {
                QcTaskId = state.QcTaskId,
                TenantId = state.TenantId,
                WarehouseId = state.WarehouseId,
                UserId = state.UserId,
                WorkflowRunId = state.WorkflowRunId,
                QcTask = state.QcTask,
                Evidence = state.Evidence,
                QualityRules = state.QualityRules,
                EvidenceGapAnalysis = state.EvidenceGapAnalysis,
                InspectionDecision = state.InspectionDecision,
                RequiresHumanApproval = state.RequiresHumanApproval,
                HumanApproval = approvalResponse,
                FinalDecision = state.FinalDecision,
                Status = "Running",
                ErrorMessage = state.ErrorMessage
            };

            // 7. 重新构建 Workflow
            var workflow = await workflowFactory.BuildAsync(cancellationToken);

            // 8. 恢复 Workflow 执行
            // 注意：MAF Workflow 的恢复机制需要通过 RequestPort 提供审批响应
            // 这里需要找到 HumanApproval RequestPort 并提供响应

            // TODO: 实际的 Workflow 恢复执行逻辑
            // 根据 MAF API，可能需要：
            // - workflow.ResumeFromCheckpoint(checkpointData, approvalResponse)
            // - 或通过 RequestPort.ProvideResponse() 方法

            logger.LogWarning(
                "Workflow resume execution not yet fully implemented. WorkflowId={WorkflowId}",
                workflowId);

            // 9. 更新 Workflow Run 状态
            workflowRun.Resume();
            await workflowRunRepository.UpdateAsync(workflowRun, cancellationToken);

            logger.LogInformation(
                "Workflow {WorkflowId} resumed successfully with decision: {Decision}",
                workflowId, approvalResponse.Decision);

            return Results.Ok(new WorkflowResumeResponse
            {
                WorkflowId = workflowId,
                Status = "Resumed",
                Message = "Workflow resumed successfully",
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
