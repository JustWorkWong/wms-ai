using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WmsAi.Platform.Domain.Tenants;
using WmsAi.Platform.Infrastructure.Persistence;
using WmsAi.SharedKernel.Persistence;
using Xunit;

namespace WmsAi.Platform.Tests;

public class DomainEventDispatcherTests
{
    [Fact]
    public async Task SaveChanges_should_collect_domain_events()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var dispatcher = new DomainEventDispatcher();
        var interceptor = new DomainEventInterceptor(dispatcher);

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlite(database)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new UserDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var tenant = new Tenant("TENANT_001", "Test Tenant");
        var warehouse = new Warehouse(tenant.Id, "WH_001", "Test Warehouse", true);

        await dbContext.AddTenantAsync(tenant, CancellationToken.None);
        await dbContext.AddWarehouseAsync(warehouse, CancellationToken.None);

        dispatcher.GetCollectedEvents().Should().BeEmpty();

        await dbContext.SaveChangesAsync(CancellationToken.None);

        var events = dispatcher.GetCollectedEvents();
        events.Should().HaveCount(2);
        events.Should().ContainSingle(e => e is TenantCreatedEvent);
        events.Should().ContainSingle(e => e is WarehouseCreatedEvent);

        tenant.DomainEvents.Should().BeEmpty();
        warehouse.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Multiple_SaveChanges_should_collect_events_separately()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var dispatcher = new DomainEventDispatcher();
        var interceptor = new DomainEventInterceptor(dispatcher);

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlite(database)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new UserDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var tenant1 = new Tenant("TENANT_001", "Test Tenant 1");
        await dbContext.AddTenantAsync(tenant1, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var firstBatchEvents = dispatcher.GetCollectedEvents();
        firstBatchEvents.Should().ContainSingle();

        dispatcher.Clear();

        var tenant2 = new Tenant("TENANT_002", "Test Tenant 2");
        await dbContext.AddTenantAsync(tenant2, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var secondBatchEvents = dispatcher.GetCollectedEvents();
        secondBatchEvents.Should().ContainSingle();
    }
}
