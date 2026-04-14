using System.Security.Claims;

namespace WmsAi.Gateway.Host.Middleware;

public sealed class FakeIdentityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FakeIdentityMiddleware> _logger;

    public FakeIdentityMiddleware(RequestDelegate next, ILogger<FakeIdentityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "dev-user";
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "TENANT_DEMO";
        var warehouseId = context.Request.Headers["X-Warehouse-Id"].FirstOrDefault() ?? "WH_SZ_01";
        var membershipId = context.Request.Headers["X-Membership-Id"].FirstOrDefault() ?? $"membership-{userId}-{tenantId}";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("tenant_id", tenantId),
            new("warehouse_id", warehouseId),
            new("membership_id", membershipId)
        };

        var identity = new ClaimsIdentity(claims, "FakeAuth");
        context.User = new ClaimsPrincipal(identity);

        _logger.LogDebug(
            "FakeIdentity set: UserId={UserId}, TenantId={TenantId}, WarehouseId={WarehouseId}, MembershipId={MembershipId}",
            userId, tenantId, warehouseId, membershipId);

        await _next(context);
    }
}
