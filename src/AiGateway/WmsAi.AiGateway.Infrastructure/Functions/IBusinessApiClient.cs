namespace WmsAi.AiGateway.Infrastructure.Functions;

public interface IBusinessApiClient
{
    Task<T?> GetAsync<T>(
        string path,
        string tenantId,
        string? warehouseId,
        CancellationToken cancellationToken = default) where T : class;

    Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        string tenantId,
        string? warehouseId,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;
}
