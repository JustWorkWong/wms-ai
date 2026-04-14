using System.Text.Json;
using WmsAi.AiGateway.Application.Agents;
using WmsAi.AiGateway.Application.Services;
using WmsAi.AiGateway.Domain.Workflows;

namespace WmsAi.AiGateway.Application.Workflows;

public sealed class InboundInspectionWorkflow(
    IMafPersistenceService persistenceService,
    IEvidenceGapAgent evidenceGapAgent,
    IInspectionDecisionAgent inspectionDecisionAgent,
    IModelRoutingService modelRoutingService)
{
    private const decimal ConfidenceThreshold = 0.85m;

    public async Task<WorkflowResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var workflowRunId = context.WorkflowRunId;

        try
        {
            // Node 1: PrepareInspectionContext
            var inspectionContext = await PrepareInspectionContextAsync(workflowRunId, context, cancellationToken);
            await SaveStepAsync(workflowRunId, "PrepareInspectionContext", StepKind.Preparation, "Completed", null, cancellationToken);

            // Node 2: LoadQualitySkill
            var qualitySkill = await LoadQualitySkillAsync(workflowRunId, context, cancellationToken);
            await SaveStepAsync(workflowRunId, "LoadQualitySkill", StepKind.DataLoad, "Completed", null, cancellationToken);

            // Node 3: LoadRulesAndEvidence
            var rulesAndEvidence = await LoadRulesAndEvidenceAsync(workflowRunId, context, cancellationToken);
            await SaveStepAsync(workflowRunId, "LoadRulesAndEvidence", StepKind.DataLoad, "Completed", null, cancellationToken);

            // Node 4: CheckEvidenceCompleteness
            var evidenceGapResult = await CheckEvidenceCompletenessAsync(
                workflowRunId,
                context,
                rulesAndEvidence,
                cancellationToken);
            await SaveStepAsync(workflowRunId, "CheckEvidenceCompleteness", StepKind.Validation, "Completed", null, cancellationToken);

            // Node 5: RunInspectionAgent
            InspectionDecisionResult? decisionResult = null;
            if (evidenceGapResult.IsComplete)
            {
                decisionResult = await RunInspectionAgentAsync(
                    workflowRunId,
                    context,
                    rulesAndEvidence,
                    cancellationToken);
                await SaveStepAsync(workflowRunId, "RunInspectionAgent", StepKind.AgentExecution, "Completed", null, cancellationToken);
            }
            else
            {
                await SaveStepAsync(workflowRunId, "RunInspectionAgent", StepKind.AgentExecution, "Skipped", "Evidence incomplete", cancellationToken);
            }

            // Node 6: NormalizeSuggestion
            var normalizedSuggestion = NormalizeSuggestion(evidenceGapResult, decisionResult);
            await SaveStepAsync(workflowRunId, "NormalizeSuggestion", StepKind.Normalization, "Completed", null, cancellationToken);

            // Node 7: EvaluateConfidenceGate
            var shouldAutoPass = EvaluateConfidenceGate(normalizedSuggestion);
            await SaveStepAsync(workflowRunId, "EvaluateConfidenceGate", StepKind.Gate, "Completed", $"AutoPass: {shouldAutoPass}", cancellationToken);

            // Node 8: PersistSuggestion
            await PersistSuggestionAsync(workflowRunId, context, normalizedSuggestion, cancellationToken);
            await SaveStepAsync(workflowRunId, "PersistSuggestion", StepKind.Persistence, "Completed", null, cancellationToken);

            // Node 9: AutoPassOrEscalate
            if (shouldAutoPass)
            {
                await AutoPassAsync(workflowRunId, context, normalizedSuggestion, cancellationToken);
                await SaveStepAsync(workflowRunId, "AutoPassOrEscalate", StepKind.Decision, "Completed", "Auto-passed", cancellationToken);

                // Node 11: FinalizeDecision
                await FinalizeDecisionAsync(workflowRunId, context, normalizedSuggestion, cancellationToken);
                await SaveStepAsync(workflowRunId, "FinalizeDecision", StepKind.Finalization, "Completed", null, cancellationToken);

                return new WorkflowResult
                {
                    Status = "Completed",
                    Decision = normalizedSuggestion.Decision,
                    RequiresManualReview = false
                };
            }
            else
            {
                await EscalateToManualReviewAsync(workflowRunId, context, normalizedSuggestion, cancellationToken);
                await SaveStepAsync(workflowRunId, "AutoPassOrEscalate", StepKind.Decision, "Completed", "Escalated to manual review", cancellationToken);

                // Node 10: WaitManualReview
                await SaveStepAsync(workflowRunId, "WaitManualReview", StepKind.Wait, "Pending", "Waiting for manual review", cancellationToken);

                return new WorkflowResult
                {
                    Status = "Paused",
                    Decision = normalizedSuggestion.Decision,
                    RequiresManualReview = true
                };
            }
        }
        catch (Exception ex)
        {
            await persistenceService.SaveWorkflowRunAsync(
                workflowRunId,
                "Failed",
                null,
                null,
                ex.Message,
                cancellationToken);

            throw;
        }
    }

    private async Task<Dictionary<string, object>> PrepareInspectionContextAsync(
        Guid workflowRunId,
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        // Load QC task details from Inbound service
        // This would call the Inbound service via HTTP
        return new Dictionary<string, object>
        {
            ["qcTaskId"] = context.QcTaskId,
            ["tenantId"] = context.TenantId,
            ["warehouseId"] = context.WarehouseId,
            ["skuCode"] = context.SkuCode ?? "UNKNOWN"
        };
    }

    private async Task<Dictionary<string, object>> LoadQualitySkillAsync(
        Guid workflowRunId,
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        // Load inspection SOP/prompt from prompt assets
        return new Dictionary<string, object>
        {
            ["promptVersion"] = "v1.0",
            ["instructions"] = "Perform quality inspection based on evidence and rules"
        };
    }

    private async Task<Dictionary<string, object>> LoadRulesAndEvidenceAsync(
        Guid workflowRunId,
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        // Load SKU quality rules and evidence metadata
        return new Dictionary<string, object>
        {
            ["qualityRules"] = new Dictionary<string, object>(),
            ["evidence"] = new List<object>()
        };
    }

    private async Task<EvidenceGapResult> CheckEvidenceCompletenessAsync(
        Guid workflowRunId,
        WorkflowContext context,
        Dictionary<string, object> rulesAndEvidence,
        CancellationToken cancellationToken)
    {
        var evidenceContext = new EvidenceGapContext
        {
            QcTaskId = context.QcTaskId,
            TenantId = context.TenantId,
            WarehouseId = context.WarehouseId,
            RequiredEvidenceTypes = ["image", "label"],
            CurrentEvidence = [],
            QualityRules = []
        };

        return await evidenceGapAgent.AnalyzeEvidenceAsync(evidenceContext, cancellationToken);
    }

    private async Task<InspectionDecisionResult> RunInspectionAgentAsync(
        Guid workflowRunId,
        WorkflowContext context,
        Dictionary<string, object> rulesAndEvidence,
        CancellationToken cancellationToken)
    {
        var decisionContext = new InspectionDecisionContext
        {
            QcTaskId = context.QcTaskId,
            TenantId = context.TenantId,
            WarehouseId = context.WarehouseId,
            SkuCode = context.SkuCode ?? "UNKNOWN",
            QualityRules = [],
            Evidence = [],
            OperationRecords = []
        };

        return await inspectionDecisionAgent.MakeDecisionAsync(decisionContext, cancellationToken);
    }

    private NormalizedSuggestion NormalizeSuggestion(
        EvidenceGapResult evidenceGapResult,
        InspectionDecisionResult? decisionResult)
    {
        if (!evidenceGapResult.IsComplete)
        {
            return new NormalizedSuggestion
            {
                Decision = "EvidenceGap",
                Reasoning = evidenceGapResult.Reasoning,
                ConfidenceScore = evidenceGapResult.ConfidenceScore,
                StructuredData = new Dictionary<string, object>
                {
                    ["gaps"] = evidenceGapResult.Gaps
                }
            };
        }

        if (decisionResult == null)
        {
            throw new InvalidOperationException("Decision result is null when evidence is complete");
        }

        return new NormalizedSuggestion
        {
            Decision = decisionResult.Decision,
            Reasoning = decisionResult.Reasoning,
            ConfidenceScore = decisionResult.ConfidenceScore,
            StructuredData = decisionResult.StructuredData
        };
    }

    private bool EvaluateConfidenceGate(NormalizedSuggestion suggestion)
    {
        return suggestion.ConfidenceScore >= ConfidenceThreshold
            && suggestion.Decision != "EvidenceGap";
    }

    private async Task PersistSuggestionAsync(
        Guid workflowRunId,
        WorkflowContext context,
        NormalizedSuggestion suggestion,
        CancellationToken cancellationToken)
    {
        // Persist suggestion to AiInspectionRun
        var suggestionJson = JsonSerializer.Serialize(suggestion);
        await persistenceService.SaveStepRunAsync(
            workflowRunId,
            "PersistSuggestion",
            "Persistence",
            "Completed",
            null,
            suggestionJson,
            null,
            cancellationToken);
    }

    private async Task AutoPassAsync(
        Guid workflowRunId,
        WorkflowContext context,
        NormalizedSuggestion suggestion,
        CancellationToken cancellationToken)
    {
        // Publish CAP event to Inbound service
        // This would trigger QcDecision creation in Inbound
        await Task.CompletedTask;
    }

    private async Task EscalateToManualReviewAsync(
        Guid workflowRunId,
        WorkflowContext context,
        NormalizedSuggestion suggestion,
        CancellationToken cancellationToken)
    {
        // Mark inspection run for manual review
        await Task.CompletedTask;
    }

    private async Task FinalizeDecisionAsync(
        Guid workflowRunId,
        WorkflowContext context,
        NormalizedSuggestion suggestion,
        CancellationToken cancellationToken)
    {
        await persistenceService.SaveWorkflowRunAsync(
            workflowRunId,
            "Completed",
            "FinalizeDecision",
            JsonSerializer.Serialize(suggestion),
            null,
            cancellationToken);
    }

    private async Task SaveStepAsync(
        Guid workflowRunId,
        string nodeName,
        StepKind stepKind,
        string status,
        string? message,
        CancellationToken cancellationToken)
    {
        await persistenceService.SaveStepRunAsync(
            workflowRunId,
            nodeName,
            stepKind.ToString(),
            status,
            message,
            null,
            null,
            cancellationToken);
    }
}

public sealed class WorkflowContext
{
    public Guid WorkflowRunId { get; init; }
    public Guid QcTaskId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string WarehouseId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string? SkuCode { get; init; }
}

public sealed class WorkflowResult
{
    public string Status { get; init; } = string.Empty;
    public string Decision { get; init; } = string.Empty;
    public bool RequiresManualReview { get; init; }
}

public sealed class NormalizedSuggestion
{
    public string Decision { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
    public decimal ConfidenceScore { get; init; }
    public Dictionary<string, object> StructuredData { get; init; } = [];
}
