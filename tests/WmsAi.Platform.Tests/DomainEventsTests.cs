using FluentAssertions;
using WmsAi.Platform.Domain.Tenants;
using WmsAi.Platform.Domain.Users;
using WmsAi.SharedKernel.Domain;
using Xunit;

namespace WmsAi.Platform.Tests;

public class DomainEventsTests
{
    [Fact]
    public void Tenant_creation_should_raise_domain_event()
    {
        var tenant = new Tenant("TENANT_001", "Test Tenant");

        tenant.DomainEvents.Should().ContainSingle();
        var domainEvent = tenant.DomainEvents.First();
        domainEvent.Should().BeOfType<TenantCreatedEvent>();

        var tenantCreatedEvent = (TenantCreatedEvent)domainEvent;
        tenantCreatedEvent.TenantId.Should().Be(tenant.Id);
        tenantCreatedEvent.TenantCode.Should().Be("TENANT_001");
        tenantCreatedEvent.TenantName.Should().Be("Test Tenant");
    }

    [Fact]
    public void Warehouse_creation_should_raise_domain_event()
    {
        var tenantId = Guid.NewGuid();
        var warehouse = new Warehouse(tenantId, "WH_001", "Test Warehouse", true);

        warehouse.DomainEvents.Should().ContainSingle();
        var domainEvent = warehouse.DomainEvents.First();
        domainEvent.Should().BeOfType<WarehouseCreatedEvent>();

        var warehouseCreatedEvent = (WarehouseCreatedEvent)domainEvent;
        warehouseCreatedEvent.WarehouseId.Should().Be(warehouse.Id);
        warehouseCreatedEvent.TenantId.Should().Be(tenantId);
        warehouseCreatedEvent.WarehouseCode.Should().Be("WH_001");
        warehouseCreatedEvent.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void User_creation_should_raise_domain_event()
    {
        var user = new User("admin.test");

        user.DomainEvents.Should().ContainSingle();
        var domainEvent = user.DomainEvents.First();
        domainEvent.Should().BeOfType<UserCreatedEvent>();

        var userCreatedEvent = (UserCreatedEvent)domainEvent;
        userCreatedEvent.UserId.Should().Be(user.Id);
        userCreatedEvent.LoginName.Should().Be("admin.test");
    }

    [Fact]
    public void Clear_domain_events_should_remove_all_events()
    {
        var tenant = new Tenant("TENANT_001", "Test Tenant");
        tenant.DomainEvents.Should().ContainSingle();

        tenant.ClearDomainEvents();

        tenant.DomainEvents.Should().BeEmpty();
    }
}
