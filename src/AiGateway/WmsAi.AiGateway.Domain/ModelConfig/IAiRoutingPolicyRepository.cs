namespace WmsAi.AiGateway.Domain.ModelConfig;

public interface IAiRoutingPolicyRepository
{
    Task<AiRoutingPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<AiRoutingPolicy>> GetBySceneCodeAsync(
        string sceneCode,
        string tenantId,
        string? warehouseId,
        CancellationToken cancellationToken = default);

    Task AddAsync(AiRoutingPolicy policy, CancellationToken cancellationToken = default);

    Task UpdateAsync(AiRoutingPolicy policy, CancellationToken cancellationToken = default);

    Task<List<AiRoutingPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);
}
