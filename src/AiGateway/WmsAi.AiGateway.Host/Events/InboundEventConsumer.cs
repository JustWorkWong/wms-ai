using DotNetCore.CAP;
using Microsoft.Extensions.Logging;
using WmsAi.Contracts.Events;

namespace WmsAi.AiGateway.Host.Events;

public sealed class InboundEventConsumer(ILogger<InboundEventConsumer> logger)
{
    [CapSubscribe("qctask.created.v1")]
    public async Task HandleQcTaskCreated(QcTaskCreatedV1 @event)
    {
        logger.LogInformation(
            "Received QcTaskCreatedV1 event: EventId={EventId}, QcTaskId={QcTaskId}, TaskNo={TaskNo}, SkuCode={SkuCode}",
            @event.EventId,
            @event.QcTaskId,
            @event.TaskNo,
            @event.SkuCode);

        // Placeholder: Actual AI workflow will be implemented in Task 6
        // This will trigger the dual-agent MAF workflow for QC decision
        await Task.CompletedTask;
    }

    [CapSubscribe("receipt.recorded.v1")]
    public async Task HandleReceiptRecorded(ReceiptRecordedV1 @event)
    {
        logger.LogInformation(
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
        logger.LogInformation(
            "Received QcDecisionFinalizedV1 event: EventId={EventId}, QcTaskId={QcTaskId}, DecisionStatus={DecisionStatus}",
            @event.EventId,
            @event.QcTaskId,
            @event.DecisionStatus);

        // Placeholder: Log event for AI learning feedback loop
        await Task.CompletedTask;
    }
}
