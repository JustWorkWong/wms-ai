using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WmsAi.AiGateway.Application.Workflows;
using WmsAi.AiGateway.Domain.Inspections;
using WmsAi.AiGateway.Domain.Workflows;
using WmsAi.AiGateway.Infrastructure.Functions;

namespace WmsAi.AiGateway.Infrastructure.Workflows.Executors;

/// <summary>
/// 保存结果 Executor - 保存 AI 检验结果到数据库并提交建议到 Inbound 服务
/// </summary>
public sealed partial class SaveResultExecutor : Executor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SaveResultExecutor> _logger;

    public SaveResultExecutor(
        IServiceProvider serviceProvider,
        ILogger<SaveResultExecutor> logger)
        : base("SaveResult")
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
            "开始保存结果: QcTaskId={QcTaskId}, Decision={Decision}, RequiresHumanApproval={RequiresHumanApproval}",
            state.QcTaskId,
            state.InspectionDecision?.Decision,
            state.RequiresHumanApproval);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var inspectionRepository = scope.ServiceProvider.GetRequiredService<IAiInspectionRunRepository>();
            var workflowRepository = scope.ServiceProvider.GetRequiredService<IMafWorkflowRunRepository>();
            var businessApiClient = scope.ServiceProvider.GetRequiredService<IBusinessApiClient>();

            // 1. 创建 AiInspectionRun 实体
            var inspectionRun = new AiInspectionRun(
                tenantId: state.TenantId,
                warehouseId: state.WarehouseId,
                qcTaskId: state.QcTaskId,
                workflowRunId: state.WorkflowRunId,
                sessionId: null,
                agentProfileCode: "InspectionDecisionAgent",
                modelProfileCode: "default",
                modelConfigSnapshotJson: null);

            inspectionRun.Start();

            // 2. 创建 AiSuggestion 实体
            var suggestion = new AiSuggestion(
                tenantId: state.TenantId,
                warehouseId: state.WarehouseId,
                qcTaskId: state.QcTaskId,
                suggestionType: state.InspectionDecision?.Decision ?? "Unknown",
                confidence: (double)(state.InspectionDecision?.ConfidenceScore ?? 0),
                reasoning: state.InspectionDecision?.Reasoning ?? "No reasoning provided");

            // 3. 保存到数据库
            await inspectionRepository.AddAsync(inspectionRun, cancellationToken);
            await inspectionRepository.AddSuggestionAsync(suggestion, cancellationToken);

            _logger.LogInformation(
                "成功保存 AiInspectionRun: InspectionRunId={InspectionRunId}, SuggestionId={SuggestionId}",
                inspectionRun.Id,
                suggestion.Id);

            // 4. 提交 AI 建议到 Inbound 服务
            try
            {
                var issues = state.InspectionDecision?.Issues != null
                    ? state.InspectionDecision.Issues.Select(i => (object)new
                    {
                        issueType = i.IssueType,
                        description = i.Description,
                        severity = i.Severity,
                        evidenceRef = i.EvidenceRef
                    }).ToList()
                    : new List<object>();

                var submitRequest = new
                {
                    qcTaskId = state.QcTaskId,
                    suggestionType = state.InspectionDecision?.Decision ?? "Unknown",
                    confidence = (double)(state.InspectionDecision?.ConfidenceScore ?? 0),
                    reasoning = state.InspectionDecision?.Reasoning ?? "No reasoning provided",
                    issues = issues,
                    structuredData = state.InspectionDecision?.StructuredData ?? new Dictionary<string, object>()
                };

                await businessApiClient.PostAsync<object, object>(
                    "/api/qc/ai-suggestions",
                    submitRequest,
                    state.TenantId,
                    state.WarehouseId,
                    cancellationToken);

                _logger.LogInformation(
                    "成功提交 AI 建议到 Inbound 服务: QcTaskId={QcTaskId}",
                    state.QcTaskId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "提交 AI 建议到 Inbound 服务失败: QcTaskId={QcTaskId}",
                    state.QcTaskId);
                // 不抛出异常，继续执行
            }

            // 5. 根据 RequiresHumanApproval 决定是否需要人工审批
            if (state.RequiresHumanApproval)
            {
                inspectionRun.EscalateToManualReview();
                await inspectionRepository.UpdateAsync(inspectionRun, cancellationToken);

                _logger.LogInformation(
                    "置信度不足，升级到人工审批: QcTaskId={QcTaskId}, ConfidenceScore={ConfidenceScore}",
                    state.QcTaskId,
                    state.InspectionDecision?.ConfidenceScore);

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
                    Status = "WaitingHumanApproval"
                };
            }

            // 6. 自动完成
            var resultSummary = JsonSerializer.Serialize(new
            {
                decision = state.InspectionDecision?.Decision,
                confidenceScore = state.InspectionDecision?.ConfidenceScore,
                reasoning = state.InspectionDecision?.Reasoning
            });

            inspectionRun.Complete(resultSummary);
            await inspectionRepository.UpdateAsync(inspectionRun, cancellationToken);

            _logger.LogInformation(
                "结果保存完成: QcTaskId={QcTaskId}, Status={Status}",
                state.QcTaskId,
                "ResultSaved");

            return state.With(finalDecision: state.InspectionDecision?.Decision, status: "ResultSaved");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "保存结果失败: QcTaskId={QcTaskId}",
                state.QcTaskId);

            return state.WithError($"保存结果失败: {ex.Message}");
        }
    }
}
