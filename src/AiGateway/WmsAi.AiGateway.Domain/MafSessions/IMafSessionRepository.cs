namespace WmsAi.AiGateway.Domain.MafSessions;

public interface IMafSessionRepository
{
    Task<MafSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MafSession?> GetByBusinessObjectAsync(string tenantId, string businessObjectType, string businessObjectId, CancellationToken cancellationToken = default);

    Task AddAsync(MafSession session, CancellationToken cancellationToken = default);

    Task UpdateAsync(MafSession session, CancellationToken cancellationToken = default);

    Task<List<MafSession>> GetActiveSessionsByUserAsync(string userId, CancellationToken cancellationToken = default);
}
