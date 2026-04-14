using DotNetCore.CAP;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WmsAi.AiGateway.Application.Workflows;
using WmsAi.Contracts.Events;

namespace WmsAi.AiGateway.Infrastructure.Workflows.Executors;

/// <summary>
/// 发布事件 Executor - 发布 AI 检验完成事件到 CAP 事件总线
/// </summary>
public sealed partial class PublishEventExecutor : Executor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PublishEventExecutor> _logger;

    public PublishEventExecutor(
        IServiceProvider serviceProvider,
        ILogger<PublishEventExecutor> logger)
        : base("PublishEvent")
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder builder)
    {
        return builder;
    }

    [MessageHandler]
    public async Task<QcInspectionState> HandleAsync(
        QcInspectionState state,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "开始发布事件: QcTaskId={QcTaskId}, Decision={Decision}",
            state.QcTaskId,
            state.InspectionDecision?.Decision);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var capPublisher = scope.ServiceProvider.GetRequiredService<ICapPublisher>();

            // 构建事件
            var @event = new AiInspectionCompletedV1(
                EventId: Guid.NewGuid(),
                Timestamp: DateTimeOffset.UtcNow,
                TenantId: state.TenantId,
                WarehouseId: state.WarehouseId,
                QcTaskId: state.QcTaskId,
                WorkflowRunId: state.WorkflowRunId,
                Decision: state.InspectionDecision?.Decision ?? "Unknown",
                Reasoning: state.InspectionDecision?.Reasoning ?? string.Empty,
                ConfidenceScore: (double)(state.InspectionDecision?.ConfidenceScore ?? 0),
                RequiresHumanApproval: state.RequiresHumanApproval,
                FinalDecision: state.FinalDecision);

            // 发布事件
            await capPublisher.PublishAsync(
                "ai.inspection.completed.v1",
                @event,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "成功发布事件: EventId={EventId}, QcTaskId={QcTaskId}, Decision={Decision}",
                @event.EventId,
                state.QcTaskId,
                @event.Decision);

            return new QcInspectionState
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
                HumanApproval = state.HumanApproval,
                FinalDecision = state.FinalDecision,
                Status = "EventPublished"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "发布事件失败: QcTaskId={QcTaskId}",
                state.QcTaskId);

            return new QcInspectionState
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
                HumanApproval = state.HumanApproval,
                FinalDecision = state.FinalDecision,
                Status = "Failed",
                ErrorMessage = $"发布事件失败: {ex.Message}"
            };
        }
    }
}
