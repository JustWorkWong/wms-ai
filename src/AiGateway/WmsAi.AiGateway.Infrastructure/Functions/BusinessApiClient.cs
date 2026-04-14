using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace WmsAi.AiGateway.Infrastructure.Functions;

public sealed class BusinessApiClient : IBusinessApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _contextAccessor;

    public BusinessApiClient(HttpClient httpClient, IHttpContextAccessor contextAccessor)
    {
        _httpClient = httpClient;
        _contextAccessor = contextAccessor;
    }

    public async Task<T?> GetAsync<T>(
        string path,
        string tenantId,
        string? warehouseId,
        CancellationToken cancellationToken = default) where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddContextHeaders(request, tenantId, warehouseId);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        string tenantId,
        string? warehouseId,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(request)
        };

        AddContextHeaders(httpRequest, tenantId, warehouseId);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private void AddContextHeaders(HttpRequestMessage request, string tenantId, string? warehouseId)
    {
        var context = _contextAccessor.HttpContext;

        request.Headers.Add("X-Tenant-Id", tenantId);

        if (!string.IsNullOrWhiteSpace(warehouseId))
            request.Headers.Add("X-Warehouse-Id", warehouseId);

        // 如果有 HttpContext，则传递用户和关联 ID
        if (context != null)
        {
            if (context.Request.Headers.TryGetValue("X-User-Id", out var userId))
                request.Headers.Add("X-User-Id", userId.ToString());

            if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
                request.Headers.Add("X-Correlation-Id", correlationId.ToString());
            else
                request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString());
        }
        else
        {
            // 后台任务（如 CAP 事件处理器）没有 HttpContext
            // 使用系统用户和生成新的关联 ID
            request.Headers.Add("X-User-Id", "system");
            request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString());
        }
    }
}
