using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
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

    [Theory]
    [InlineData("tenantId", "", "user-1", "membership-1", "corr-1")]
    [InlineData("tenantId", "wh-1", " ", "membership-1", "corr-1")]
    [InlineData("tenantId", "wh-1", "user-1", "\t", "corr-1")]
    [InlineData("tenantId", "wh-1", "user-1", "membership-1", "\n")]
    public void Request_execution_context_should_reject_blank_scope_values(
        string tenantId,
        string? warehouseId,
        string userId,
        string membershipId,
        string correlationId)
    {
        var act = () => new RequestExecutionContext(tenantId, warehouseId, userId, membershipId, correlationId);

        act.Should().Throw<ArgumentException>();
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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Tenant_scoped_aggregate_roots_should_reject_blank_tenant_ids(string tenantId)
    {
        var act = () => new TestTenantAggregate(tenantId);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Warehouse_scoped_aggregate_roots_should_reject_blank_tenant_and_warehouse_ids(string value)
    {
        var tenantAct = () => new TestWarehouseAggregate(value, "wh-1");
        var warehouseAct = () => new TestWarehouseAggregate("tenant-a", value);

        tenantAct.Should().Throw<ArgumentException>();
        warehouseAct.Should().Throw<ArgumentException>();
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
    public void Versioned_entity_type_configuration_should_increment_versions_and_block_concurrent_writes()
    {
        using var keeper = new SqliteConnection("Data Source=file:sharedkernel-tests?mode=memory&cache=shared");
        keeper.Open();

        var options = new DbContextOptionsBuilder<TestVersionedDbContext>()
            .UseSqlite("Data Source=file:sharedkernel-tests?mode=memory&cache=shared")
            .AddInterceptors(new VersionedEntitySaveChangesInterceptor())
            .Options;

        using (var setup = new TestVersionedDbContext(options))
        {
            setup.Database.EnsureCreated();
            setup.Entities.Add(new TestVersionedEntity("original"));
            setup.SaveChanges();
        }

        using var firstContext = new TestVersionedDbContext(options);
        using var secondContext = new TestVersionedDbContext(options);

        var first = firstContext.Entities.Single();
        var second = secondContext.Entities.Single();

        first.Name = "first";
        second.Name = "second";

        firstContext.SaveChanges();

        first.Version.Should().Be(2);

        Action act = () => secondContext.SaveChanges();

        act.Should().Throw<DbUpdateConcurrencyException>();
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

    private sealed class TestVersionedEntity : AggregateRoot
    {
        public TestVersionedEntity(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }

    private sealed class TestVersionedDbContext(DbContextOptions<TestVersionedDbContext> options)
        : DbContext(options)
    {
        public DbSet<TestVersionedEntity> Entities => Set<TestVersionedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<TestVersionedEntity>();
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            VersionedEntityTypeConfiguration.ApplyVersion(entity);
        }
    }
}
