using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WmsAi.Platform.Application.Tenants;
using WmsAi.Platform.Domain.Tenants;
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
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

        try
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:UserDb"] = $"Data Source={databasePath}"
                })
                .Build();

            services.AddPlatformModule(configuration);

            await using var provider = services.BuildServiceProvider();
            await PlatformDatabaseInitializer.InitializeAsync(provider);

            await using var scope = provider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();

            var tenant = new Tenant("TENANT_VERSION", "版本租户");
            await dbContext.AddTenantAsync(tenant, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            tenant.Rename("版本租户-更新");
            await dbContext.SaveChangesAsync(CancellationToken.None);

            tenant.Version.Should().Be(2);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
