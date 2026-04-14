using WmsAi.SharedKernel.Domain;

namespace WmsAi.AiGateway.Domain.ModelConfig;

public sealed class AiModelProfile : AggregateRoot
{
    private AiModelProfile()
    {
    }

    public AiModelProfile(
        Guid providerId,
        string profileCode,
        string sceneCode,
        string modelName,
        decimal temperature,
        decimal? topP,
        int maxTokens,
        int timeoutSeconds,
        string? retryPolicyJson,
        string? promptAssetVersion,
        bool isActive)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(providerId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);
        ArgumentOutOfRangeException.ThrowIfNegative(temperature);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(temperature, 2.0m);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTokens);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeoutSeconds);

        ProviderId = providerId;
        ProfileCode = profileCode.Trim();
        SceneCode = sceneCode.Trim();
        ModelName = modelName.Trim();
        Temperature = temperature;
        TopP = topP;
        MaxTokens = maxTokens;
        TimeoutSeconds = timeoutSeconds;
        RetryPolicyJson = retryPolicyJson?.Trim();
        PromptAssetVersion = promptAssetVersion?.Trim();
        IsActive = isActive;
    }

    public Guid ProviderId { get; private set; }

    public string ProfileCode { get; private set; } = string.Empty;

    public string SceneCode { get; private set; } = string.Empty;

    public string ModelName { get; private set; } = string.Empty;

    public decimal Temperature { get; private set; }

    public decimal? TopP { get; private set; }

    public int MaxTokens { get; private set; }

    public int TimeoutSeconds { get; private set; }

    public string? RetryPolicyJson { get; private set; }

    public string? PromptAssetVersion { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdateModelParameters(decimal temperature, decimal? topP, int maxTokens)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(temperature);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(temperature, 2.0m);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTokens);

        Temperature = temperature;
        TopP = topP;
        MaxTokens = maxTokens;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
