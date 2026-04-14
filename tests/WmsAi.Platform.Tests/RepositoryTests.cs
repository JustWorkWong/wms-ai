using FluentAssertions;
using WmsAi.Platform.Domain.Tenants;
using WmsAi.Platform.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WmsAi.Platform.Infrastructure.Persistence;
using Xunit;

namespace WmsAi.Platform.Tests;

public class RepositoryTests
{
    [Fact]
    public async Task TenantRepository_should_add_and_retrieve_tenant()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlite(database)
            .Options;

        await using var dbContext = new UserDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new TenantRepository(dbContext);
        var tenant = new Tenant("TENANT_001", "Test Tenant");

        await repository.AddAsync(tenant);
        await dbContext.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(tenant.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Code.Should().Be("TENANT_001");

        var retrievedByCode = await repository.GetByCodeAsync("TENANT_001");
        retrievedByCode.Should().NotBeNull();
        retrievedByCode!.Id.Should().Be(tenant.Id);

        var exists = await repository.ExistsByCodeAsync("TENANT_001");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task WarehouseRepository_should_add_and_retrieve_warehouse()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlite(database)
            .Options;

        await using var dbContext = new UserDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var tenantRepository = new TenantRepository(dbContext);
        var tenant = new Tenant("TENANT_001", "Test Tenant");
        await tenantRepository.AddAsync(tenant);
        await dbContext.SaveChangesAsync();

        var repository = new WarehouseRepository(dbContext);
        var warehouse = new Warehouse(tenant.Id, "WH_001", "Test Warehouse", true);

        await repository.AddAsync(warehouse);
        await dbContext.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(warehouse.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Code.Should().Be("WH_001");

        var retrievedByCode = await repository.GetByCodeAsync(tenant.Id, "WH_001");
        retrievedByCode.Should().NotBeNull();
        retrievedByCode!.Id.Should().Be(warehouse.Id);

        var exists = await repository.ExistsByCodeAsync(tenant.Id, "WH_001");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task UserRepository_should_add_and_retrieve_user()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlite(database)
            .Options;

        await using var dbContext = new UserDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new UserRepository(dbContext);
        var user = new WmsAi.Platform.Domain.Users.User("admin.test");

        await repository.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(user.Id);
        retrieved.Should().NotBeNull();
        retrieved!.LoginName.Should().Be("admin.test");

        var retrievedByLoginName = await repository.GetByLoginNameAsync("admin.test");
        retrievedByLoginName.Should().NotBeNull();
        retrievedByLoginName!.Id.Should().Be(user.Id);

        var exists = await repository.ExistsByLoginNameAsync("admin.test");
        exists.Should().BeTrue();
    }
}
