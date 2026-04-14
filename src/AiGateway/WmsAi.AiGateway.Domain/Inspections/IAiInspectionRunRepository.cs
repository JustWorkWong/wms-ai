namespace WmsAi.AiGateway.Domain.Inspections;

public interface IAiInspectionRunRepository
{
    Task<AiInspectionRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AiInspectionRun?> GetByQcTaskIdAsync(Guid qcTaskId, CancellationToken cancellationToken = default);

    Task AddAsync(AiInspectionRun inspectionRun, CancellationToken cancellationToken = default);

    Task UpdateAsync(AiInspectionRun inspectionRun, CancellationToken cancellationToken = default);

    Task<List<AiInspectionRun>> GetByStatusAsync(string tenantId, InspectionStatus status, CancellationToken cancellationToken = default);

    Task AddSuggestionAsync(AiSuggestion suggestion, CancellationToken cancellationToken = default);
}
