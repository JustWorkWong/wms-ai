namespace WmsAi.AiGateway.Application.Agents;

public interface IEvidenceGapAgent
{
    Task<EvidenceGapResult> AnalyzeEvidenceAsync(
        EvidenceGapContext context,
        CancellationToken cancellationToken = default);
}

public sealed class EvidenceGapContext
{
    public Guid QcTaskId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string WarehouseId { get; init; } = string.Empty;
    public List<string> RequiredEvidenceTypes { get; init; } = [];
    public List<EvidenceItem> CurrentEvidence { get; init; } = [];
    public Dictionary<string, object> QualityRules { get; init; } = [];
}

public sealed class EvidenceItem
{
    public string EvidenceType { get; init; } = string.Empty;
    public string EvidenceId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = [];
}

public sealed class EvidenceGapResult
{
    public bool IsComplete { get; init; }
    public List<EvidenceGap> Gaps { get; init; } = [];
    public string Reasoning { get; init; } = string.Empty;
    public decimal ConfidenceScore { get; init; }
}

public sealed class EvidenceGap
{
    public string EvidenceType { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
}
