using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WmsAi.Contracts.Errors;
using WmsAi.SharedKernel.Domain;
using WmsAi.SharedKernel.Execution;
using WmsAi.SharedKernel.Persistence;
using Xunit;

namespace WmsAi.ArchitectureTests;

public class SharedKernelTests
{
    [Fact]
    public void Request_execution_context_should_expose_requested_scope_fields()
    {
        var context = new RequestExecutionContext("tenant-a", "wh-1", "user-1", "membership-1", "corr-1");

        context.TenantId.Should().Be("tenant-a");
        context.WarehouseId.Should().Be("wh-1");
        context.UserId.Should().Be("user-1");
        context.MembershipId.Should().Be("membership-1");
        context.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public void Aggregate_root_should_start_with_a_concurrency_version()
    {
        var aggregate = new TestAggregate();

        aggregate.Version.Should().Be(1);
        aggregate.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Scoped_aggregate_roots_should_carry_tenant_and_warehouse_ids()
    {
        var tenantScoped = new TestTenantAggregate("tenant-a");
        var warehouseScoped = new TestWarehouseAggregate("tenant-a", "wh-1");

        tenantScoped.TenantId.Should().Be("tenant-a");
        warehouseScoped.TenantId.Should().Be("tenant-a");
        warehouseScoped.WarehouseId.Should().Be("wh-1");
    }

    [Fact]
    public void Error_codes_should_include_the_shared_contract_constants()
    {
        ErrorCodes.AuthUnauthorized.Should().Be("AUTH_UNAUTHORIZED");
        ErrorCodes.RequestInvalid.Should().Be("REQUEST_INVALID");
        ErrorCodes.QcTaskStatusInvalid.Should().Be("QC_TASK_STATUS_INVALID");
        ErrorCodes.AiSuggestionInvalid.Should().Be("AI_SUGGESTION_INVALID");
    }

    [Fact]
    public void Versioned_entity_type_configuration_should_apply_a_concurrency_token()
    {
        var builder = new ModelBuilder();
        var entity = builder.Entity<VersionedEntity>();

        VersionedEntityTypeConfiguration.ApplyVersion(entity);

        entity.Metadata.FindProperty(nameof(VersionedEntity.Version))!.IsConcurrencyToken.Should().BeTrue();
    }

    private sealed class TestAggregate : AggregateRoot;

    private sealed class TestTenantAggregate : TenantScopedAggregateRoot
    {
        public TestTenantAggregate(string tenantId)
            : base(tenantId)
        {
        }
    }

    private sealed class TestWarehouseAggregate : WarehouseScopedAggregateRoot
    {
        public TestWarehouseAggregate(string tenantId, string warehouseId)
            : base(tenantId, warehouseId)
        {
        }
    }

    private sealed class VersionedEntity : AggregateRoot;
}
