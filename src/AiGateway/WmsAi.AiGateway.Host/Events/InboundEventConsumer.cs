using DotNetCore.CAP;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Agents.AI.Workflows.InProc;
using WmsAi.AiGateway.Application.Workflows;
using WmsAi.AiGateway.Domain.Workflows;
using WmsAi.AiGateway.Infrastructure.Workflows;
using WmsAi.Contracts.Events;

namespace WmsAi.AiGateway.Host.Events;

public sealed class InboundEventConsumer : ICapSubscribe
{
    private readonly ILogger<InboundEventConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public InboundEventConsumer(
        ILogger<InboundEventConsumer> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    [CapSubscribe("qctask.created.v1")]
    public async Task HandleQcTaskCreated(QcTaskCreatedV1 @event)
    {
        _logger.LogInformation(
            "Received QcTaskCreatedV1 event: EventId={EventId}, QcTaskId={QcTaskId}, TaskNo={TaskNo}, SkuCode={SkuCode}",
            @event.EventId,
            @event.QcTaskId,
            @event.TaskNo,
            @event.SkuCode);

        try
        {
            using var scope = _serviceProvider.CreateScope();

            // 解析 MAF 服务
            var workflowFactory = scope.ServiceProvider.GetRequiredService<QcInspectionWorkflowFactory>();
            var checkpointStore = scope.ServiceProvider.GetRequiredService<ICheckpointStore<JsonElement>>();
            var workflowRunRepository = scope.ServiceProvider.GetRequiredService<IMafWorkflowRunRepository>();

            // 构建 Workflow
            var workflow = await workflowFactory.BuildAsync(CancellationToken.None);

            _logger.LogInformation(
                "Workflow built successfully for QcTaskId={QcTaskId}",
                @event.QcTaskId);

            // 准备初始 State
            var workflowRunId = Guid.NewGuid();
            var initialState = new QcInspectionState
            {
                QcTaskId = @event.QcTaskId,
                TenantId = @event.TenantId,
                WarehouseId = @event.WarehouseId,
                UserId = WorkflowConstants.SystemUserId,
                WorkflowRunId = workflowRunId,
                Status = "Running"
            };

            // 创建 Workflow Run 记录
            var workflowRun = new MafWorkflowRun(
                tenantId: @event.TenantId,
                warehouseId: @event.WarehouseId,
                workflowName: "QcInspectionWorkflow",
                agentProfileCode: "QcInspection",
                requestedBy: WorkflowConstants.SystemUserId,
                membershipId: null,
                userInput: JsonSerializer.Serialize(new { @event.QcTaskId, @event.TaskNo }),
                routingJson: null,
                executionContextJson: JsonSerializer.Serialize(initialState));
            await workflowRunRepository.AddAsync(workflowRun, CancellationToken.None);

            _logger.LogInformation(
                "Created MafWorkflowRun: WorkflowRunId={WorkflowRunId}, QcTaskId={QcTaskId}",
                workflowRunId,
                @event.QcTaskId);

            // 创建执行环境（带 Checkpointing）
            var sessionId = workflowRunId.ToString();
            var checkpointManager = CheckpointManager.CreateJson(checkpointStore, new JsonSerializerOptions());

            // 执行 Workflow（流式）
            var streamingRun = await InProcessExecution.RunStreamingAsync(
                workflow,
                initialState,
                checkpointManager,
                sessionId,
                CancellationToken.None);

            _logger.LogInformation(
                "Workflow execution started: WorkflowRunId={WorkflowRunId}, SessionId={SessionId}",
                workflowRunId,
                sessionId);

            // 处理事件流
            await foreach (var workflowEvent in streamingRun.WatchStreamAsync(CancellationToken.None))
            {
                await HandleWorkflowEvent(workflowEvent, workflowRun, workflowRunRepository, streamingRun);
            }

            _logger.LogInformation(
                "Workflow execution completed: WorkflowRunId={WorkflowRunId}, QcTaskId={QcTaskId}",
                workflowRunId,
                @event.QcTaskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to execute MAF workflow for QcTaskId={QcTaskId}, TaskNo={TaskNo}",
                @event.QcTaskId,
                @event.TaskNo);
        }
    }

    /// <summary>
    /// 处理 Workflow 事件流
    /// </summary>
    private async Task HandleWorkflowEvent(
        WorkflowEvent workflowEvent,
        MafWorkflowRun workflowRun,
        IMafWorkflowRunRepository workflowRunRepository,
        StreamingRun streamingRun)
    {
        switch (workflowEvent)
        {
            case SuperStepCompletedEvent completedEvent:
                _logger.LogInformation(
                    "SuperStep completed: WorkflowRunId={WorkflowRunId}, StepNumber={StepNumber}",
                    workflowRun.Id,
                    completedEvent.StepNumber);
                // Checkpoint 已自动保存，无需额外操作
                break;

            case RequestInfoEvent requestEvent:
                await HandleRequestInfoEvent(requestEvent, workflowRun, streamingRun);
                break;

            case ExecutorCompletedEvent executorCompleted:
                _logger.LogInformation(
                    "Executor completed: WorkflowRunId={WorkflowRunId}, ExecutorId={ExecutorId}",
                    workflowRun.Id,
                    executorCompleted.ExecutorId);
                break;

            case ExecutorFailedEvent executorFailed:
                _logger.LogError(
                    "Executor failed: WorkflowRunId={WorkflowRunId}, ExecutorId={ExecutorId}, Error={Error}",
                    workflowRun.Id,
                    executorFailed.ExecutorId,
                    executorFailed.Data?.ToString() ?? "Unknown error");
                workflowRun.Fail(executorFailed.Data?.ToString() ?? "Unknown error");
                await workflowRunRepository.UpdateAsync(workflowRun, CancellationToken.None);
                break;

            default:
                _logger.LogDebug(
                    "Received workflow event: Type={EventType}, WorkflowRunId={WorkflowRunId}",
                    workflowEvent.GetType().Name,
                    workflowRun.Id);
                break;
        }
    }

    /// <summary>
    /// 处理 RequestInfoEvent（人工审批请求）
    /// </summary>
    private async Task HandleRequestInfoEvent(
        RequestInfoEvent requestEvent,
        MafWorkflowRun workflowRun,
        StreamingRun streamingRun)
    {
        try
        {
            // 从 ExternalRequest.Data 中解析 ApprovalRequest
            var requestData = requestEvent.Request.Data;
            var approvalRequest = requestData.As<ApprovalRequest>();

            if (approvalRequest == null)
            {
                _logger.LogError(
                    "Failed to deserialize ApprovalRequest: WorkflowRunId={WorkflowRunId}",
                    workflowRun.Id);
                return;
            }

            _logger.LogInformation(
                "Received approval request: WorkflowRunId={WorkflowRunId}, QcTaskId={QcTaskId}, ConfidenceScore={ConfidenceScore}, AiDecision={AiDecision}",
                workflowRun.Id,
                approvalRequest.QcTaskId,
                approvalRequest.ConfidenceScore,
                approvalRequest.AiDecision);

            // 检查置信度
            if (approvalRequest.ConfidenceScore >= WorkflowConstants.HighConfidenceThreshold)
            {
                // 高置信度：自动批准
                var response = new ApprovalResponse
                {
                    Decision = approvalRequest.AiDecision,
                    Comments = $"Auto-approved by system (confidence: {approvalRequest.ConfidenceScore:P0})",
                    ReviewerId = WorkflowConstants.SystemUserId,
                    ApprovedAt = DateTimeOffset.UtcNow
                };

                // 发送响应
                var externalResponse = new ExternalResponse(
                    requestEvent.Request.PortInfo,
                    requestEvent.Request.RequestId,
                    new PortableValue(response));

                await streamingRun.SendResponseAsync(externalResponse);

                _logger.LogInformation(
                    "Auto-approved: WorkflowRunId={WorkflowRunId}, QcTaskId={QcTaskId}, Decision={Decision}",
                    workflowRun.Id,
                    approvalRequest.QcTaskId,
                    response.Decision);
            }
            else
            {
                // 低置信度：暂停等待人工审批
                _logger.LogInformation(
                    "Workflow paused for human approval: WorkflowRunId={WorkflowRunId}, QcTaskId={QcTaskId}, ConfidenceScore={ConfidenceScore}",
                    workflowRun.Id,
                    approvalRequest.QcTaskId,
                    approvalRequest.ConfidenceScore);

                workflowRun.Pause();
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IMafWorkflowRunRepository>();
                await repository.UpdateAsync(workflowRun, CancellationToken.None);

                // 不响应 Request，等待外部 API 调用 /api/ai/workflows/{workflowId}/resume
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to handle RequestInfoEvent: WorkflowRunId={WorkflowRunId}",
                workflowRun.Id);
        }
    }

    [CapSubscribe("receipt.recorded.v1")]
    public async Task HandleReceiptRecorded(ReceiptRecordedV1 @event)
    {
        _logger.LogInformation(
            "Received ReceiptRecordedV1 event: EventId={EventId}, ReceiptId={ReceiptId}, ReceiptNo={ReceiptNo}",
            @event.EventId,
            @event.ReceiptId,
            @event.ReceiptNo);

        // Placeholder: Log event for future AI analytics
        await Task.CompletedTask;
    }

    [CapSubscribe("qcdecision.finalized.v1")]
    public async Task HandleQcDecisionFinalized(QcDecisionFinalizedV1 @event)
    {
        _logger.LogInformation(
            "Received QcDecisionFinalizedV1 event: EventId={EventId}, QcTaskId={QcTaskId}, DecisionStatus={DecisionStatus}",
            @event.EventId,
            @event.QcTaskId,
            @event.DecisionStatus);

        // Placeholder: Log event for AI learning feedback loop
        await Task.CompletedTask;
    }
}
