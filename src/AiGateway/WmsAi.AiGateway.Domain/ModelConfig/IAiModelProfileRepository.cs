namespace WmsAi.AiGateway.Domain.ModelConfig;

public interface IAiModelProfileRepository
{
    Task<AiModelProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AiModelProfile?> GetByProfileCodeAsync(string profileCode, CancellationToken cancellationToken = default);

    Task<List<AiModelProfile>> GetBySceneCodeAsync(string sceneCode, CancellationToken cancellationToken = default);

    Task AddAsync(AiModelProfile profile, CancellationToken cancellationToken = default);

    Task UpdateAsync(AiModelProfile profile, CancellationToken cancellationToken = default);

    Task<List<AiModelProfile>> GetActiveProfilesAsync(CancellationToken cancellationToken = default);
}
