using Microsoft.AspNetCore.Mvc;
using WmsAi.AiGateway.Application.AgUi;
using WmsAi.AiGateway.Domain.Inspections;
using WmsAi.AiGateway.Infrastructure.Functions;

namespace WmsAi.AiGateway.Host.Controllers;

[ApiController]
[Route("api/ai/inspections")]
public sealed class AiInspectionsController : ControllerBase
{
    private readonly IAiInspectionRunRepository _inspectionRepository;
    private readonly IBusinessApiClient _businessApiClient;
    private readonly ILogger<AiInspectionsController> _logger;

    public AiInspectionsController(
        IAiInspectionRunRepository inspectionRepository,
        IBusinessApiClient businessApiClient,
        ILogger<AiInspectionsController> logger)
    {
        _inspectionRepository = inspectionRepository;
        _businessApiClient = businessApiClient;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartInspection(
        [FromBody] StartInspectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 检查是否已存在该 QcTask 的检验记录
            var existing = await _inspectionRepository.GetByQcTaskIdAsync(request.QcTaskId, cancellationToken);
            if (existing != null)
            {
                return Conflict(new { error = "Inspection already exists for this QC task", inspectionRunId = existing.Id });
            }

            // 创建工作流运行记录
            var workflowRunId = Guid.NewGuid();
            var inspectionRun = new AiInspectionRun(
                request.TenantId,
                request.WarehouseId,
                request.QcTaskId,
                workflowRunId,
                sessionId: null,
                agentProfileCode: "QC_INSPECTOR",
                modelProfileCode: "DEFAULT",
                modelConfigSnapshotJson: null);

            await _inspectionRepository.AddAsync(inspectionRun, cancellationToken);

            // 启动工作流（标记为运行中）
            inspectionRun.Start();
            await _inspectionRepository.UpdateAsync(inspectionRun, cancellationToken);

            _logger.LogInformation(
                "Started AI inspection: InspectionRunId={InspectionRunId}, QcTaskId={QcTaskId}, WorkflowRunId={WorkflowRunId}",
                inspectionRun.Id,
                request.QcTaskId,
                workflowRunId);

            return Ok(new
            {
                inspectionRunId = inspectionRun.Id,
                workflowRunId,
                status = inspectionRun.Status.ToString(),
                qcTaskId = request.QcTaskId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start inspection for QcTaskId={QcTaskId}", request.QcTaskId);
            return StatusCode(500, new { error = "Failed to start inspection", details = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetInspectionStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var inspection = await _inspectionRepository.GetByIdAsync(id, cancellationToken);
        if (inspection == null)
            return NotFound(new { error = "Inspection run not found" });

        var suggestions = inspection.Suggestions.Select(s => new
        {
            id = s.Id,
            suggestionType = s.SuggestionType,
            reasoning = s.Reasoning,
            confidence = s.Confidence,
            createdAt = s.CreatedAt
        }).ToList();

        return Ok(new
        {
            id = inspection.Id,
            qcTaskId = inspection.QcTaskId,
            workflowRunId = inspection.WorkflowRunId,
            status = inspection.Status.ToString(),
            agentProfileCode = inspection.AgentProfileCode,
            modelProfileCode = inspection.ModelProfileCode,
            resultSummary = inspection.ResultSummary,
            suggestions,
            createdAt = inspection.CreatedAt,
            updatedAt = inspection.UpdatedAt,
            completedAt = inspection.CompletedAt
        });
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> ResumeInspection(
        Guid id,
        [FromBody] ResumeInspectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var inspection = await _inspectionRepository.GetByIdAsync(id, cancellationToken);
            if (inspection == null)
                return NotFound(new { error = "Inspection run not found" });

            if (inspection.Status != InspectionStatus.WaitingManualReview)
                return BadRequest(new { error = $"Cannot resume inspection with status {inspection.Status}" });

            // 调用 Inbound 服务完成质检决策
            var finalizeCommand = new
            {
                tenantId = inspection.TenantId,
                warehouseId = inspection.WarehouseId,
                qcTaskId = inspection.QcTaskId,
                decisionStatus = request.Decision,
                decisionSource = "AI_MANUAL_REVIEW",
                reasonSummary = request.Reasoning
            };

            var result = await _businessApiClient.PostAsync<object, FinalizeQcDecisionResult>(
                "/api/inbound/qc/decisions",
                finalizeCommand,
                inspection.TenantId,
                inspection.WarehouseId,
                cancellationToken);

            if (result == null)
            {
                _logger.LogError("Failed to finalize QC decision for InspectionRunId={InspectionRunId}", id);
                return StatusCode(500, new { error = "Failed to finalize QC decision" });
            }

            // 更新检验记录状态
            inspection.Complete($"Manual review completed: {request.Decision} - {request.Reasoning}");
            await _inspectionRepository.UpdateAsync(inspection, cancellationToken);

            _logger.LogInformation(
                "Resumed and completed inspection: InspectionRunId={InspectionRunId}, Decision={Decision}",
                id,
                request.Decision);

            return Ok(new
            {
                id = inspection.Id,
                status = inspection.Status.ToString(),
                decision = request.Decision,
                qcDecisionId = result.QcDecisionId,
                taskStatus = result.TaskStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume inspection: InspectionRunId={InspectionRunId}", id);
            return StatusCode(500, new { error = "Failed to resume inspection", details = ex.Message });
        }
    }

    [HttpPost("{inspectionRunId:guid}/manual-review")]
    public async Task<IActionResult> SubmitManualReview(
        Guid inspectionRunId,
        [FromBody] ManualReviewRequest request,
        CancellationToken cancellationToken)
    {
        var inspection = await _inspectionRepository.GetByIdAsync(inspectionRunId, cancellationToken);
        if (inspection == null)
            return NotFound(new { error = "Inspection run not found" });

        if (inspection.Status != InspectionStatus.WaitingManualReview)
            return BadRequest(new { error = "Inspection is not waiting for manual review" });

        inspection.CompleteManualReview(request.Decision, request.Reasoning, request.ReviewerId);
        await _inspectionRepository.UpdateAsync(inspection, cancellationToken);

        // TODO: Resume workflow from checkpoint

        return Ok(new
        {
            inspectionRunId,
            status = inspection.Status.ToString(),
            decision = request.Decision
        });
    }
}

public record StartInspectionRequest(Guid QcTaskId, string TenantId, string WarehouseId, string UserId);
public record ResumeInspectionRequest(string Decision, string Reasoning);
public record FinalizeQcDecisionResult(Guid QcDecisionId, string TaskStatus);
