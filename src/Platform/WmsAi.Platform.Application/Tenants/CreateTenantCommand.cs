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

public interface IEventPublisher
{
    Task PublishCollectedEventsAsync(CancellationToken cancellationToken = default);
}

public sealed class CreateTenantHandler(IPlatformUserDbContext userDbContext, IEventPublisher eventPublisher)
{
    public async Task<CreateTenantResult> Handle(
        CreateTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenant = new Tenant(command.TenantCode, command.TenantName);
        var warehouse = new Warehouse(tenant.Id, command.DefaultWarehouseCode, command.DefaultWarehouseName, true);
        var user = new User(command.AdminLoginName);
        var membership = new Membership(tenant.Id, warehouse.Id, user.Id, "owner");

        await userDbContext.AddTenantAsync(tenant, cancellationToken);
        await userDbContext.AddWarehouseAsync(warehouse, cancellationToken);
        await userDbContext.AddUserAsync(user, cancellationToken);
        await userDbContext.AddMembershipAsync(membership, cancellationToken);
        await userDbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishCollectedEventsAsync(cancellationToken);

        return new CreateTenantResult(tenant.Code, warehouse.Code, user.LoginName);
    }
}
