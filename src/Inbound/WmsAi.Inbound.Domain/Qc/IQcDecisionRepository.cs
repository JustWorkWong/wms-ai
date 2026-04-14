namespace WmsAi.Inbound.Domain.Qc;

public interface IQcDecisionRepository
{
    Task<QcDecision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QcDecision?> GetByQcTaskIdAsync(Guid qcTaskId, CancellationToken cancellationToken = default);
    Task AddAsync(QcDecision decision, CancellationToken cancellationToken = default);
}
