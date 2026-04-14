namespace WmsAi.AiGateway.Application.Agents;

public interface IInspectionDecisionAgent
{
    Task<InspectionDecisionResult> MakeDecisionAsync(
        InspectionDecisionContext context,
        CancellationToken cancellationToken = default);
}

public sealed class InspectionDecisionContext
{
    public Guid QcTaskId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string WarehouseId { get; init; } = string.Empty;
    public string SkuCode { get; init; } = string.Empty;
    public Dictionary<string, object> QualityRules { get; init; } = [];
    public List<EvidenceAsset> Evidence { get; init; } = [];
    public Dictionary<string, object> OperationRecords { get; init; } = [];
}

public sealed class EvidenceAsset
{
    public string AssetType { get; init; } = string.Empty;
    public string AssetId { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = [];
}

public sealed class InspectionDecisionResult
{
    public string Decision { get; init; } = string.Empty; // Accept, Reject, Conditional
    public string Reasoning { get; init; } = string.Empty;
    public decimal ConfidenceScore { get; init; }
    public List<QualityIssue> Issues { get; init; } = [];
    public Dictionary<string, object> StructuredData { get; init; } = [];
}

public sealed class QualityIssue
{
    public string IssueType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string? EvidenceRef { get; init; }
}
