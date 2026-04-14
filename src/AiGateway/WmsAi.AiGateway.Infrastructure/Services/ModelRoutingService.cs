using System.Text.Json;
using Microsoft.Extensions.Logging;
using WmsAi.AiGateway.Application.Services;
using WmsAi.AiGateway.Domain.ModelConfig;

namespace WmsAi.AiGateway.Infrastructure.Services;

public sealed class ModelRoutingService(
    IAiModelProfileRepository profileRepository,
    IAiModelProviderRepository providerRepository,
    IAiRoutingPolicyRepository routingPolicyRepository,
    ILogger<ModelRoutingService> logger) : IModelRoutingService
{
    public async Task<ModelConfiguration> GetModelConfigurationAsync(
        string sceneCode,
        string tenantId,
        string? warehouseId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Routing model for scene={SceneCode}, tenant={TenantId}, warehouse={WarehouseId}",
            sceneCode, tenantId, warehouseId);

        // Step 1: Check if there's a routing policy
        var policies = await routingPolicyRepository.GetBySceneCodeAsync(
            sceneCode, tenantId, warehouseId, cancellationToken);

        AiModelProfile? selectedProfile = null;
        string? routingReason = null;

        if (policies.Count > 0)
        {
            // Use the highest priority policy (already sorted by repository)
            var policy = policies.First();
            logger.LogInformation(
                "Found routing policy: {PolicyName} (priority={Priority}, warehouse={WarehouseId})",
                policy.PolicyName, policy.Priority, policy.WarehouseId);

            selectedProfile = await ApplyRoutingPolicyAsync(
                policy, sceneCode, cancellationToken);
            routingReason = $"Policy: {policy.PolicyName}";
        }

        // Step 2: Fallback to default profile selection
        if (selectedProfile == null)
        {
            var profiles = await profileRepository.GetBySceneCodeAsync(sceneCode, cancellationToken);

            if (profiles.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No active model profiles found for scene '{sceneCode}' in tenant '{tenantId}'");
            }

            selectedProfile = profiles.First();
            routingReason = "Default (first active profile)";
            logger.LogInformation(
                "No routing policy found, using default profile: {ProfileCode}",
                selectedProfile.ProfileCode);
        }

        // Step 3: Get provider information
        var provider = await providerRepository.GetByIdAsync(
            selectedProfile.ProviderId, cancellationToken);

        if (provider == null)
        {
            throw new InvalidOperationException(
                $"Provider not found for profile '{selectedProfile.ProfileCode}'");
        }

        // Step 4: Check provider status and apply fallback if needed
        if (provider.Status != ProviderStatus.Active)
        {
            logger.LogWarning(
                "Provider {ProviderCode} is {Status}, attempting fallback",
                provider.ProviderCode, provider.Status);

            var fallbackResult = await FindFallbackProfileAsync(
                sceneCode, selectedProfile.Id, cancellationToken);

            if (fallbackResult.HasValue)
            {
                selectedProfile = fallbackResult.Value.Profile;
                provider = fallbackResult.Value.Provider;
                routingReason = $"{routingReason} → Fallback (primary provider unavailable)";
                logger.LogInformation(
                    "Fallback to profile: {ProfileCode}, provider: {ProviderCode}",
                    selectedProfile.ProfileCode, provider.ProviderCode);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Provider '{provider.ProviderCode}' is {provider.Status} and no fallback available for scene '{sceneCode}' in tenant '{tenantId}'");
            }
        }

        logger.LogInformation(
            "Selected model: profile={ProfileCode}, provider={ProviderCode}, model={ModelName}, reason={Reason}",
            selectedProfile.ProfileCode, provider.ProviderCode, selectedProfile.ModelName, routingReason);

        return new ModelConfiguration
        {
            ProfileCode = selectedProfile.ProfileCode,
            ProviderCode = provider.ProviderCode,
            ModelName = selectedProfile.ModelName,
            Temperature = selectedProfile.Temperature,
            TopP = selectedProfile.TopP,
            MaxTokens = selectedProfile.MaxTokens,
            TimeoutSeconds = selectedProfile.TimeoutSeconds,
            PromptAssetVersion = selectedProfile.PromptAssetVersion,
            AdditionalSettings = new Dictionary<string, object>
            {
                ["RoutingReason"] = routingReason ?? "Unknown"
            }
        };
    }

    public Task<string> CreateConfigurationSnapshotAsync(
        ModelConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var snapshot = JsonSerializer.Serialize(configuration);
        return Task.FromResult(snapshot);
    }

    private async Task<AiModelProfile?> ApplyRoutingPolicyAsync(
        AiRoutingPolicy policy,
        string sceneCode,
        CancellationToken cancellationToken)
    {
        // Parse routing rules JSON
        // For now, we'll implement a simple strategy-based routing
        // The JSON structure could be: { "strategy": "cost|latency|load", "profileCode": "xxx" }

        try
        {
            var rules = JsonSerializer.Deserialize<RoutingRules>(policy.RoutingRulesJson);

            if (rules?.ProfileCode != null)
            {
                var profile = await profileRepository.GetByProfileCodeAsync(
                    rules.ProfileCode, cancellationToken);

                if (profile != null && profile.IsActive)
                {
                    return profile;
                }

                logger.LogWarning(
                    "Profile {ProfileCode} specified in policy {PolicyName} not found or inactive",
                    rules.ProfileCode, policy.PolicyName);
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex,
                "Failed to parse routing rules for policy {PolicyName}: {Rules}",
                policy.PolicyName, policy.RoutingRulesJson);
        }

        return null;
    }

    private async Task<(AiModelProfile Profile, AiModelProvider Provider)?> FindFallbackProfileAsync(
        string sceneCode,
        Guid excludeProfileId,
        CancellationToken cancellationToken)
    {
        var profiles = await profileRepository.GetBySceneCodeAsync(sceneCode, cancellationToken);

        foreach (var profile in profiles.Where(p => p.Id != excludeProfileId))
        {
            var provider = await providerRepository.GetByIdAsync(
                profile.ProviderId, cancellationToken);

            if (provider?.Status == ProviderStatus.Active)
            {
                return (profile, provider);
            }
        }

        return null;
    }

    private sealed class RoutingRules
    {
        public string? Strategy { get; set; }
        public string? ProfileCode { get; set; }
    }
}
