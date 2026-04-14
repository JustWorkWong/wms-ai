using Microsoft.AspNetCore.Mvc;
using WmsAi.AiGateway.Application.AgUi;
using WmsAi.AiGateway.Domain.Inspections;

namespace WmsAi.AiGateway.Host.Controllers;

[ApiController]
[Route("api/ai/inspections")]
public sealed class AiInspectionsController : ControllerBase
{
    private readonly IAiInspectionRunRepository _inspectionRepository;

    public AiInspectionsController(IAiInspectionRunRepository inspectionRepository)
    {
        _inspectionRepository = inspectionRepository;
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
