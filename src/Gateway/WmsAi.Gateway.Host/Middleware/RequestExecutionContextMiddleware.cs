using System.Security.Claims;
using WmsAi.SharedKernel.Execution;

namespace WmsAi.Gateway.Host.Middleware;

public sealed class RequestExecutionContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestExecutionContextMiddleware> _logger;

    public RequestExecutionContextMiddleware(RequestDelegate next, ILogger<RequestExecutionContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown-user";
        var tenantId = context.User.FindFirstValue("tenant_id") ?? "UNKNOWN_TENANT";
        var warehouseId = context.User.FindFirstValue("warehouse_id");
        var membershipId = context.User.FindFirstValue("membership_id") ?? "unknown-membership";
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        var executionContext = new RequestExecutionContext(
            tenantId,
            warehouseId,
            userId,
            membershipId,
            correlationId);

        context.Request.Headers["X-Tenant-Id"] = executionContext.TenantId;
        context.Request.Headers["X-User-Id"] = executionContext.UserId;
        context.Request.Headers["X-Membership-Id"] = executionContext.MembershipId;
        context.Request.Headers["X-Correlation-Id"] = executionContext.CorrelationId;

        if (!string.IsNullOrWhiteSpace(executionContext.WarehouseId))
        {
            context.Request.Headers["X-Warehouse-Id"] = executionContext.WarehouseId;
        }

        _logger.LogDebug(
            "RequestExecutionContext propagated: TenantId={TenantId}, WarehouseId={WarehouseId}, UserId={UserId}, CorrelationId={CorrelationId}",
            executionContext.TenantId, executionContext.WarehouseId, executionContext.UserId, executionContext.CorrelationId);

        await _next(context);
    }
}
