using DotNetCore.CAP;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WmsAi.AiGateway.Application.Agents;
using WmsAi.AiGateway.Application.Functions;
using WmsAi.AiGateway.Domain.Inspections;
using WmsAi.AiGateway.Domain.Workflows;
using WmsAi.AiGateway.Infrastructure.Functions;
using WmsAi.Contracts.Events;

namespace WmsAi.AiGateway.Host.Events;

public sealed class InboundEventConsumer(
    ILogger<InboundEventConsumer> logger,
    IAiInspectionRunRepository inspectionRepository,
    IMafWorkflowRunRepository workflowRepository,
    IEvidenceGapAgent evidenceGapAgent,
    IInspectionDecisionAgent inspectionDecisionAgent,
    IInboundBusinessFunctions inboundFunctions,
    IBusinessApiClient businessApiClient)
{
    private const string SystemUserId = "system";
    private const decimal HighConfidenceThreshold = 0.8m;

    [CapSubscribe("qctask.created.v1")]
    public async Task HandleQcTaskCreated(QcTaskCreatedV1 @event)
    {
        logger.LogInformation(
            "Received QcTaskCreatedV1 event: EventId={EventId}, QcTaskId={QcTaskId}, TaskNo={TaskNo}, SkuCode={SkuCode}",
            @event.EventId,
            @event.QcTaskId,
            @event.TaskNo,
            @event.SkuCode);

        try
        {
            await ExecuteAiInspectionWorkflowAsync(@event, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to execute AI inspection workflow for QcTaskId={QcTaskId}, TaskNo={TaskNo}",
                @event.QcTaskId,
                @event.TaskNo);
        }
    }

    private async Task ExecuteAiInspectionWorkflowAsync(QcTaskCreatedV1 @event, CancellationToken cancellationToken)
    {
        // 1. 创建工作流运行记录
        var workflowRun = new MafWorkflowRun(
            tenantId: @event.TenantId,
            warehouseId: @event.WarehouseId,
            workflowName: "QC_INSPECTION",
            agentProfileCode: "QC_DUAL_AGENT",
            requestedBy: SystemUserId,
            membershipId: null,
            userInput: JsonSerializer.Serialize(new { qcTaskId = @event.QcTaskId, taskNo = @event.TaskNo }),
            routingJson: null,
            executionContextJson: null);

        await workflowRepository.AddAsync(workflowRun, cancellationToken);
        workflowRun.Start();
        await workflowRepository.UpdateAsync(workflowRun, cancellationToken);

        logger.LogInformation(
            "Created workflow run: WorkflowRunId={WorkflowRunId}, QcTaskId={QcTaskId}",
            workflowRun.Id,
            @event.QcTaskId);

        // 2. 创建 AI 检验运行记录
        var inspectionRun = new AiInspectionRun(
            tenantId: @event.TenantId,
            warehouseId: @event.WarehouseId,
            qcTaskId: @event.QcTaskId,
            workflowRunId: workflowRun.Id,
            sessionId: null,
            agentProfileCode: "QC_DUAL_AGENT",
            modelProfileCode: "DEFAULT",
            modelConfigSnapshotJson: null);

        await inspectionRepository.AddAsync(inspectionRun, cancellationToken);
        inspectionRun.Start();
        await inspectionRepository.UpdateAsync(inspectionRun, cancellationToken);

        logger.LogInformation(
            "Created inspection run: InspectionRunId={InspectionRunId}, QcTaskId={QcTaskId}",
            inspectionRun.Id,
            @event.QcTaskId);

        try
        {
            // 3. 获取质检任务详情
            var qcTaskDetails = await inboundFunctions.GetQcTaskDetailsAsync(
                @event.QcTaskId,
                @event.TenantId,
                @event.WarehouseId,
                cancellationToken);

            if (qcTaskDetails == null)
            {
                throw new InvalidOperationException($"QC task not found: {@event.QcTaskId}");
            }

            // 4. 获取证据资产
            var evidenceAssets = await inboundFunctions.GetEvidenceAssetsAsync(
                @event.QcTaskId,
                @event.TenantId,
                @event.WarehouseId,
                cancellationToken);

            logger.LogInformation(
                "Retrieved evidence assets: Count={Count}, QcTaskId={QcTaskId}",
                evidenceAssets.Count,
                @event.QcTaskId);

            // 5. 获取质量规则
            var qualityProfile = await inboundFunctions.GetQualityRulesAsync(
                @event.SkuCode,
                @event.TenantId,
                cancellationToken);

            // 6. 调用证据缺口智能体
            var evidenceGapContext = new EvidenceGapContext
            {
                QcTaskId = @event.QcTaskId,
                TenantId = @event.TenantId,
                WarehouseId = @event.WarehouseId,
                RequiredEvidenceTypes = new List<string> { "Photo", "Video", "Document" },
                CurrentEvidence = evidenceAssets.Select(e => new EvidenceItem
                {
                    EvidenceType = e.Type,
                    EvidenceId = e.AssetId.ToString(),
                    Status = "Available",
                    Metadata = new Dictionary<string, object> { ["url"] = e.Url }
                }).ToList(),
                QualityRules = qualityProfile?.Rules.ToDictionary(
                    r => r.RuleType,
                    r => (object)new { r.Description, r.Threshold }) ?? new Dictionary<string, object>()
            };

            var evidenceGapResult = await evidenceGapAgent.AnalyzeEvidenceAsync(evidenceGapContext, cancellationToken);

            logger.LogInformation(
                "Evidence gap analysis completed: IsComplete={IsComplete}, Confidence={Confidence}, QcTaskId={QcTaskId}",
                evidenceGapResult.IsComplete,
                evidenceGapResult.ConfidenceScore,
                @event.QcTaskId);

            // 记录证据缺口分析步骤
            workflowRun.AddStepRun(
                nodeName: "EvidenceGapAnalysis",
                agentProfileCode: "EVIDENCE_GAP_AGENT",
                stepKind: StepKind.AgentExecution,
                inputJson: JsonSerializer.Serialize(evidenceGapContext),
                payloadJson: JsonSerializer.Serialize(evidenceGapResult),
                evidenceJson: null);
            await workflowRepository.UpdateAsync(workflowRun, cancellationToken);

            // 如果证据不完整，创建证据缺口建议
            if (!evidenceGapResult.IsComplete)
            {
                await inboundFunctions.SubmitAiSuggestionAsync(
                    @event.QcTaskId,
                    @event.TenantId,
                    @event.WarehouseId,
                    SuggestionType.EvidenceGap.ToString(),
                    (double)evidenceGapResult.ConfidenceScore,
                    evidenceGapResult.Reasoning,
                    cancellationToken);

                logger.LogWarning(
                    "Evidence gaps detected: Gaps={GapCount}, QcTaskId={QcTaskId}",
                    evidenceGapResult.Gaps.Count,
                    @event.QcTaskId);
            }

            // 7. 调用检验决策智能体
            var inspectionContext = new InspectionDecisionContext
            {
                QcTaskId = @event.QcTaskId,
                TenantId = @event.TenantId,
                WarehouseId = @event.WarehouseId,
                SkuCode = @event.SkuCode,
                QualityRules = qualityProfile?.Rules.ToDictionary(
                    r => r.RuleType,
                    r => (object)new { r.Description, r.Threshold }) ?? new Dictionary<string, object>(),
                Evidence = evidenceAssets.Select(e => new Application.Agents.EvidenceAsset
                {
                    AssetType = e.Type,
                    AssetId = e.AssetId.ToString(),
                    Url = e.Url,
                    Metadata = string.IsNullOrEmpty(e.Metadata)
                        ? new Dictionary<string, object>()
                        : JsonSerializer.Deserialize<Dictionary<string, object>>(e.Metadata) ?? new Dictionary<string, object>()
                }).ToList(),
                OperationRecords = new Dictionary<string, object>
                {
                    ["taskNo"] = qcTaskDetails.TaskNo,
                    ["quantity"] = qcTaskDetails.Quantity,
                    ["status"] = qcTaskDetails.Status
                }
            };

            var decisionResult = await inspectionDecisionAgent.MakeDecisionAsync(inspectionContext, cancellationToken);

            logger.LogInformation(
                "Inspection decision completed: Decision={Decision}, Confidence={Confidence}, QcTaskId={QcTaskId}",
                decisionResult.Decision,
                decisionResult.ConfidenceScore,
                @event.QcTaskId);

            // 记录检验决策步骤
            workflowRun.AddStepRun(
                nodeName: "InspectionDecision",
                agentProfileCode: "INSPECTION_DECISION_AGENT",
                stepKind: StepKind.AgentExecution,
                inputJson: JsonSerializer.Serialize(inspectionContext),
                payloadJson: JsonSerializer.Serialize(decisionResult),
                evidenceJson: null);
            await workflowRepository.UpdateAsync(workflowRun, cancellationToken);

            // 8. 提交 AI 建议
            await inboundFunctions.SubmitAiSuggestionAsync(
                @event.QcTaskId,
                @event.TenantId,
                @event.WarehouseId,
                decisionResult.Decision,
                (double)decisionResult.ConfidenceScore,
                decisionResult.Reasoning,
                cancellationToken);

            // 9. 根据置信度决定是否自动完成质检
            if (decisionResult.ConfidenceScore >= HighConfidenceThreshold)
            {
                logger.LogInformation(
                    "High confidence decision, auto-finalizing: Confidence={Confidence}, QcTaskId={QcTaskId}",
                    decisionResult.ConfidenceScore,
                    @event.QcTaskId);

                // 自动调用 Inbound 服务完成质检
                var finalizeCommand = new
                {
                    tenantId = @event.TenantId,
                    warehouseId = @event.WarehouseId,
                    qcTaskId = @event.QcTaskId,
                    decisionStatus = decisionResult.Decision,
                    decisionSource = "AI_AUTO",
                    reasonSummary = decisionResult.Reasoning
                };

                var finalizeResult = await businessApiClient.PostAsync<object, FinalizeQcDecisionResult>(
                    "/api/inbound/qc/decisions",
                    finalizeCommand,
                    @event.TenantId,
                    @event.WarehouseId,
                    cancellationToken);

                if (finalizeResult != null)
                {
                    inspectionRun.Complete($"Auto-finalized with decision: {decisionResult.Decision}");
                    workflowRun.Complete(JsonSerializer.Serialize(new
                    {
                        decision = decisionResult.Decision,
                        confidence = decisionResult.ConfidenceScore,
                        qcDecisionId = finalizeResult.QcDecisionId,
                        autoFinalized = true
                    }));

                    logger.LogInformation(
                        "QC decision auto-finalized: QcDecisionId={QcDecisionId}, QcTaskId={QcTaskId}",
                        finalizeResult.QcDecisionId,
                        @event.QcTaskId);
                }
                else
                {
                    throw new InvalidOperationException("Failed to finalize QC decision via API");
                }
            }
            else
            {
                // 置信度低，标记为等待人工复核
                logger.LogInformation(
                    "Low confidence decision, escalating to manual review: Confidence={Confidence}, QcTaskId={QcTaskId}",
                    decisionResult.ConfidenceScore,
                    @event.QcTaskId);

                inspectionRun.EscalateToManualReview();
                workflowRun.Pause();

                logger.LogInformation(
                    "Inspection escalated to manual review: InspectionRunId={InspectionRunId}, QcTaskId={QcTaskId}",
                    inspectionRun.Id,
                    @event.QcTaskId);
            }

            await inspectionRepository.UpdateAsync(inspectionRun, cancellationToken);
            await workflowRepository.UpdateAsync(workflowRun, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "AI inspection workflow failed: QcTaskId={QcTaskId}, WorkflowRunId={WorkflowRunId}",
                @event.QcTaskId,
                workflowRun.Id);

            inspectionRun.Fail();
            workflowRun.Fail(ex.Message);

            await inspectionRepository.UpdateAsync(inspectionRun, cancellationToken);
            await workflowRepository.UpdateAsync(workflowRun, cancellationToken);

            throw;
        }
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

internal sealed record FinalizeQcDecisionResult(Guid QcDecisionId, string TaskStatus);
