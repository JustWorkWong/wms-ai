namespace WmsAi.AiGateway.Domain.ModelConfig;

public interface IAiModelProviderRepository
{
    Task<AiModelProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AiModelProvider?> GetByProviderCodeAsync(string providerCode, CancellationToken cancellationToken = default);

    Task AddAsync(AiModelProvider provider, CancellationToken cancellationToken = default);

    Task UpdateAsync(AiModelProvider provider, CancellationToken cancellationToken = default);

    Task<List<AiModelProvider>> GetActiveProvidersAsync(CancellationToken cancellationToken = default);
}
