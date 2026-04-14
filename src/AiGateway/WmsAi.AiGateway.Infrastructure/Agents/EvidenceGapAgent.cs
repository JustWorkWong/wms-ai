using WmsAi.AiGateway.Application.Agents;

namespace WmsAi.AiGateway.Infrastructure.Agents;

public sealed class EvidenceGapAgent : IEvidenceGapAgent
{
    public Task<EvidenceGapResult> AnalyzeEvidenceAsync(
        EvidenceGapContext context,
        CancellationToken cancellationToken = default)
    {
        // Stub implementation - returns mock evidence gap analysis
        var requiredTypes = context.RequiredEvidenceTypes.ToHashSet();
        var currentTypes = context.CurrentEvidence.Select(e => e.EvidenceType).ToHashSet();
        var missingTypes = requiredTypes.Except(currentTypes).ToList();

        var gaps = missingTypes.Select(type => new EvidenceGap
        {
            EvidenceType = type,
            Reason = $"Required evidence type '{type}' is missing",
            Severity = "High"
        }).ToList();

        return Task.FromResult(new EvidenceGapResult
        {
            IsComplete = gaps.Count == 0,
            Gaps = gaps,
            Reasoning = gaps.Count == 0
                ? "All required evidence is present"
                : $"Missing {gaps.Count} required evidence type(s)",
            ConfidenceScore = gaps.Count == 0 ? 1.0m : 0.5m
        });
    }
}
