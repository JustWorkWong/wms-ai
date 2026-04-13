using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WmsAi.Inbound.Application.Abstractions;
using WmsAi.Inbound.Application.Inbound;
using WmsAi.Inbound.Application.Qc;
using WmsAi.Inbound.Application.Receipts;
using WmsAi.Inbound.Domain.Inbound;
using WmsAi.Inbound.Domain.Qc;
using WmsAi.Inbound.Domain.Receipts;
using WmsAi.SharedKernel.Persistence;

namespace WmsAi.Inbound.Infrastructure.Persistence;

public sealed class BusinessDbContext(DbContextOptions<BusinessDbContext> options)
    : DbContext(options), IBusinessDbContext
{
    public DbSet<InboundNotice> InboundNotices => Set<InboundNotice>();

    public DbSet<Receipt> Receipts => Set<Receipt>();

    public DbSet<QcTask> QcTasks => Set<QcTask>();

    public DbSet<QcDecision> QcDecisions => Set<QcDecision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboundNotice>(builder =>
        {
            builder.ToTable("inbound_notices");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.TenantId).HasMaxLength(64);
            builder.Property(entity => entity.WarehouseId).HasMaxLength(64);
            builder.Property(entity => entity.NoticeNo).HasMaxLength(64);
            builder.Property(entity => entity.Status).HasMaxLength(32);
            builder.HasIndex(entity => new { entity.TenantId, entity.WarehouseId, entity.NoticeNo }).IsUnique();
            builder.OwnsMany(entity => entity.Lines, lines =>
            {
                lines.ToTable("inbound_notice_lines");
                lines.WithOwner().HasForeignKey("InboundNoticeId");
                lines.HasKey(line => line.Id);
                lines.Property(line => line.SkuCode).HasMaxLength(128);
                lines.Property(line => line.ExpectedQuantity).HasColumnType("TEXT");
            });
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });

        modelBuilder.Entity<Receipt>(builder =>
        {
            builder.ToTable("receipts");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.TenantId).HasMaxLength(64);
            builder.Property(entity => entity.WarehouseId).HasMaxLength(64);
            builder.Property(entity => entity.ReceiptNo).HasMaxLength(64);
            builder.Property(entity => entity.Status).HasMaxLength(32);
            builder.HasIndex(entity => new { entity.TenantId, entity.WarehouseId, entity.ReceiptNo }).IsUnique();
            builder.HasOne<InboundNotice>()
                .WithMany()
                .HasForeignKey(entity => entity.InboundNoticeId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.OwnsMany(entity => entity.Lines, lines =>
            {
                lines.ToTable("receipt_lines");
                lines.WithOwner().HasForeignKey("ReceiptId");
                lines.HasKey(line => line.Id);
                lines.Property(line => line.SkuCode).HasMaxLength(128);
                lines.Property(line => line.ReceivedQuantity).HasColumnType("TEXT");
            });
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });

        modelBuilder.Entity<QcTask>(builder =>
        {
            builder.ToTable("qc_tasks");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.TenantId).HasMaxLength(64);
            builder.Property(entity => entity.WarehouseId).HasMaxLength(64);
            builder.Property(entity => entity.SkuCode).HasMaxLength(128);
            builder.Property(entity => entity.TaskNo).HasMaxLength(64);
            builder.Property(entity => entity.Status).HasMaxLength(32);
            builder.HasIndex(entity => new { entity.TenantId, entity.WarehouseId, entity.TaskNo }).IsUnique();
            builder.HasIndex(entity => new { entity.TenantId, entity.WarehouseId, entity.Status });
            builder.HasOne<InboundNotice>()
                .WithMany()
                .HasForeignKey(entity => entity.InboundNoticeId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Receipt>()
                .WithMany()
                .HasForeignKey(entity => entity.ReceiptId)
                .OnDelete(DeleteBehavior.Restrict);
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });

        modelBuilder.Entity<QcDecision>(builder =>
        {
            builder.ToTable("qc_decisions");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.TenantId).HasMaxLength(64);
            builder.Property(entity => entity.WarehouseId).HasMaxLength(64);
            builder.Property(entity => entity.DecisionStatus).HasMaxLength(32);
            builder.Property(entity => entity.DecisionSource).HasMaxLength(32);
            builder.Property(entity => entity.ReasonSummary).HasMaxLength(1024);
            builder.HasIndex(entity => entity.QcTaskId).IsUnique();
            builder.HasIndex(entity => new { entity.TenantId, entity.WarehouseId, entity.DecisionStatus });
            builder.HasOne<QcTask>()
                .WithMany()
                .HasForeignKey(entity => entity.QcTaskId)
                .OnDelete(DeleteBehavior.Restrict);
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });
    }
}

public static class InboundModuleExtensions
{
    public static IServiceCollection AddInboundModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BusinessDb")
            ?? "Data Source=wmsai-inbound-business.db";

        services.AddSingleton<VersionedEntitySaveChangesInterceptor>();
        services.AddDbContext<BusinessDbContext>((serviceProvider, options) =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<VersionedEntitySaveChangesInterceptor>());
        });
        services.AddScoped<IBusinessDbContext>(serviceProvider => serviceProvider.GetRequiredService<BusinessDbContext>());
        services.AddScoped<CreateInboundNoticeHandler>();
        services.AddScoped<RecordReceiptHandler>();
        services.AddScoped<FinalizeQcDecisionHandler>();
        services.AddScoped<GetQcTasksHandler>();

        return services;
    }
}

public static class BusinessDatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BusinessDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
