using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WmsAi.Platform.Application.Tenants;
using WmsAi.Platform.Infrastructure.Persistence;
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
    }
}
