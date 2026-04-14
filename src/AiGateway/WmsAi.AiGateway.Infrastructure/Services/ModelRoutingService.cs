using System.Text.Json;
using WmsAi.AiGateway.Application.Services;
using WmsAi.AiGateway.Domain.ModelConfig;

namespace WmsAi.AiGateway.Infrastructure.Services;

public sealed class ModelRoutingService(IAiModelProfileRepository profileRepository) : IModelRoutingService
{
    public async Task<ModelConfiguration> GetModelConfigurationAsync(
        string sceneCode,
        string tenantId,
        string? warehouseId,
        CancellationToken cancellationToken = default)
    {
        // Get profiles for the scene
        var profiles = await profileRepository.GetBySceneCodeAsync(sceneCode, cancellationToken);

        if (profiles.Count == 0)
        {
            throw new InvalidOperationException($"No active model profiles found for scene: {sceneCode}");
        }

        // For now, just return the first active profile
        // TODO: Implement routing policy logic based on tenant/warehouse
        var profile = profiles.First();

        return new ModelConfiguration
        {
            ProfileCode = profile.ProfileCode,
            ProviderCode = "openai", // TODO: Get from provider
            ModelName = profile.ModelName,
            Temperature = profile.Temperature,
            TopP = profile.TopP,
            MaxTokens = profile.MaxTokens,
            TimeoutSeconds = profile.TimeoutSeconds,
            PromptAssetVersion = profile.PromptAssetVersion,
            AdditionalSettings = new Dictionary<string, object>()
        };
    }

    public Task<string> CreateConfigurationSnapshotAsync(
        ModelConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var snapshot = JsonSerializer.Serialize(configuration);
        return Task.FromResult(snapshot);
    }
}
