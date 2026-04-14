using DotNetCore.CAP;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using WmsAi.AiGateway.Application.Workflows;
using WmsAi.AiGateway.Infrastructure.Workflows;
using WmsAi.Contracts.Events;

namespace WmsAi.AiGateway.Host.Events;

public sealed class InboundEventConsumer : ICapSubscribe
{
    private readonly ILogger<InboundEventConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;

    private const string SystemUserId = "system";
    private const decimal HighConfidenceThreshold = 0.8m;

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

            // 构建 Workflow
            var workflow = await workflowFactory.BuildAsync(CancellationToken.None);

            _logger.LogInformation(
                "Workflow built successfully for QcTaskId={QcTaskId}",
                @event.QcTaskId);

            // 准备初始 State
            var initialState = new QcInspectionState
            {
                QcTaskId = @event.QcTaskId,
                TenantId = @event.TenantId,
                WarehouseId = @event.WarehouseId,
                UserId = SystemUserId,
                WorkflowRunId = Guid.NewGuid(),
                Status = "Running"
            };

            // TODO: 执行 Workflow
            // MAF Workflow 的实际执行方法需要根据 Microsoft.Agents.AI.Workflows 的 API 文档确定
            // 可能的方法：workflow.Run(state), workflow.Execute(state), 或通过 WorkflowRunner
            // 当前暂时记录日志，等待 MAF API 确认后补充实现

            _logger.LogWarning(
                "Workflow execution not yet implemented. QcTaskId={QcTaskId}, WorkflowRunId={WorkflowRunId}",
                @event.QcTaskId,
                initialState.WorkflowRunId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to execute MAF workflow for QcTaskId={QcTaskId}, TaskNo={TaskNo}",
                @event.QcTaskId,
                @event.TaskNo);
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
