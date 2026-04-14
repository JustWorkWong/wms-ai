using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WmsAi.Platform.Application.Tenants;
using WmsAi.Platform.Domain.Tenants;
using WmsAi.Platform.Infrastructure.Persistence;
using WmsAi.SharedKernel.Persistence;
using Xunit;

namespace WmsAi.Platform.Tests;

public class CreateTenantTests
{
    [Fact]
    public async Task Create_tenant_should_create_default_warehouse_and_membership()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlite(database)
            .Options;

        await using var dbContext = new UserDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var handler = new CreateTenantHandler(dbContext);

        var result = await handler.Handle(new CreateTenantCommand(
            "TENANT_DEMO",
            "演示租户",
            "WH_SZ_01",
            "深圳一仓",
            "admin.demo"));

        result.TenantCode.Should().Be("TENANT_DEMO");
        result.DefaultWarehouseCode.Should().Be("WH_SZ_01");
        dbContext.Tenants.Should().ContainSingle();
        dbContext.Warehouses.Should().ContainSingle();
        dbContext.Users.Should().ContainSingle();
        dbContext.Memberships.Should().ContainSingle();

        var tenant = await dbContext.Tenants.SingleAsync();
        var warehouse = await dbContext.Warehouses.SingleAsync();
        var user = await dbContext.Users.SingleAsync();
        var membership = await dbContext.Memberships.SingleAsync();

        warehouse.TenantId.Should().Be(tenant.Id);
        membership.TenantId.Should().Be(tenant.Id);
        membership.WarehouseId.Should().Be(warehouse.Id);
        membership.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Add_platform_module_should_register_version_interceptor()
    {
        // Use in-memory SQLite for testing
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlite(database)
            .AddInterceptors(new VersionedEntitySaveChangesInterceptor())
            .Options;

        await using var dbContext = new UserDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var tenant = new Tenant("TENANT_VERSION", "版本租户");
        await dbContext.AddTenantAsync(tenant, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        tenant.Rename("版本租户-更新");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        tenant.Version.Should().Be(2);
    }
}
