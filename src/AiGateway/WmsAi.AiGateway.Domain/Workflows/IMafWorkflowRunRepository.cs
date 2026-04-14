namespace WmsAi.AiGateway.Domain.Workflows;

public interface IMafWorkflowRunRepository
{
    Task<MafWorkflowRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(MafWorkflowRun workflowRun, CancellationToken cancellationToken = default);

    Task UpdateAsync(MafWorkflowRun workflowRun, CancellationToken cancellationToken = default);

    Task<List<MafWorkflowRun>> GetByStatusAsync(string tenantId, WorkflowStatus status, CancellationToken cancellationToken = default);

    Task<List<MafWorkflowRun>> GetRecentByWarehouseAsync(string tenantId, string warehouseId, int limit, CancellationToken cancellationToken = default);
}
