namespace WmsAi.AiGateway.Application.Services;

public interface IModelRoutingService
{
    Task<ModelConfiguration> GetModelConfigurationAsync(
        string sceneCode,
        string tenantId,
        string? warehouseId,
        CancellationToken cancellationToken = default);

    Task<string> CreateConfigurationSnapshotAsync(
        ModelConfiguration configuration,
        CancellationToken cancellationToken = default);
}

public sealed class ModelConfiguration
{
    public string ProfileCode { get; init; } = string.Empty;
    public string ProviderCode { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public decimal Temperature { get; init; }
    public decimal? TopP { get; init; }
    public int MaxTokens { get; init; }
    public int TimeoutSeconds { get; init; }
    public string? PromptAssetVersion { get; init; }
    public Dictionary<string, object> AdditionalSettings { get; init; } = [];
}
