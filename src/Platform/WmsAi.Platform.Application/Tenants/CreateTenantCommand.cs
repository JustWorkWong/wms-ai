using WmsAi.Platform.Application.Abstractions;
using WmsAi.Platform.Domain.Tenants;
using WmsAi.Platform.Domain.Users;

namespace WmsAi.Platform.Application.Tenants;

public sealed record CreateTenantCommand(
    string TenantCode,
    string TenantName,
    string DefaultWarehouseCode,
    string DefaultWarehouseName,
    string AdminLoginName);

public sealed record CreateTenantResult(
    string TenantCode,
    string DefaultWarehouseCode,
    string AdminLoginName);

public sealed class CreateTenantHandler(IPlatformUserDbContext userDbContext)
{
    public async Task<CreateTenantResult> Handle(
        CreateTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenant = new Tenant(command.TenantCode, command.TenantName);
        var warehouse = tenant.AddDefaultWarehouse(command.DefaultWarehouseCode, command.DefaultWarehouseName);
        var user = new User(command.AdminLoginName);
        var membership = new Membership(tenant.Code, warehouse.Code, user.LoginName, "owner");

        await userDbContext.AddTenantAsync(tenant, cancellationToken);
        await userDbContext.AddWarehouseAsync(warehouse, cancellationToken);
        await userDbContext.AddUserAsync(user, cancellationToken);
        await userDbContext.AddMembershipAsync(membership, cancellationToken);
        await userDbContext.SaveChangesAsync(cancellationToken);

        return new CreateTenantResult(tenant.Code, warehouse.Code, user.LoginName);
    }
}
