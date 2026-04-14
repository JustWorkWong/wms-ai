using WmsAi.SharedKernel.Domain;

namespace WmsAi.AiGateway.Domain.ModelConfig;

public sealed class AiModelProvider : AggregateRoot
{
    private AiModelProvider()
    {
    }

    public AiModelProvider(
        string providerCode,
        string providerName,
        string apiBaseUrl,
        string? apiVersion,
        string authMode,
        string credentialRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiBaseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(authMode);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialRef);

        ProviderCode = providerCode.Trim();
        ProviderName = providerName.Trim();
        ApiBaseUrl = apiBaseUrl.Trim();
        ApiVersion = apiVersion?.Trim();
        AuthMode = authMode.Trim();
        CredentialRef = credentialRef.Trim();
        Status = ProviderStatus.Active;
    }

    public string ProviderCode { get; private set; } = string.Empty;

    public string ProviderName { get; private set; } = string.Empty;

    public string ApiBaseUrl { get; private set; } = string.Empty;

    public string? ApiVersion { get; private set; }

    public string AuthMode { get; private set; } = string.Empty;

    public string CredentialRef { get; private set; } = string.Empty;

    public ProviderStatus Status { get; private set; }

    public void UpdateCredential(string credentialRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialRef);
        CredentialRef = credentialRef.Trim();
    }

    public void Activate()
    {
        Status = ProviderStatus.Active;
    }

    public void Deactivate()
    {
        Status = ProviderStatus.Inactive;
    }

    public void Deprecate()
    {
        Status = ProviderStatus.Deprecated;
    }
}
