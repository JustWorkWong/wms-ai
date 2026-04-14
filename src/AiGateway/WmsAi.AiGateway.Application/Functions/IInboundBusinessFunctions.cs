namespace WmsAi.AiGateway.Application.Functions;

public interface IInboundBusinessFunctions
{
    Task<QcTaskDetails?> GetQcTaskDetailsAsync(
        Guid qcTaskId,
        string tenantId,
        string warehouseId,
        CancellationToken cancellationToken = default);

    Task<List<EvidenceAsset>> GetEvidenceAssetsAsync(
        Guid qcTaskId,
        string tenantId,
        string warehouseId,
        CancellationToken cancellationToken = default);

    Task<QualityProfile?> GetQualityRulesAsync(
        string skuId,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<Guid> SubmitAiSuggestionAsync(
        Guid qcTaskId,
        string tenantId,
        string warehouseId,
        string suggestionType,
        double confidence,
        string reasoning,
        CancellationToken cancellationToken = default);
}

public sealed record QcTaskDetails(
    Guid QcTaskId,
    string TaskNo,
    string SkuCode,
    decimal Quantity,
    string Status,
    Guid InboundNoticeId,
    Guid ReceiptId);

public sealed record EvidenceAsset(
    Guid AssetId,
    string Type,
    string Url,
    string? Metadata = null);

public sealed record QualityProfile(
    string SkuId,
    List<QualityRule> Rules);

public sealed record QualityRule(
    string RuleType,
    string Description,
    string? Threshold = null);
