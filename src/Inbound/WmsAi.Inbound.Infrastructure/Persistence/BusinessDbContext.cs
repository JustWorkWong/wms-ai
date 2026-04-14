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
            builder.Property(entity => entity.Status).HasConversion<string>();
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
            builder.Property(entity => entity.Status).HasConversion<string>();
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
            builder.Property(entity => entity.Status).HasConversion<string>();
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
            builder.Property(entity => entity.DecisionResult).HasConversion<string>();
            builder.Property(entity => entity.DecisionSource).HasMaxLength(32);
            builder.Property(entity => entity.ReasonSummary).HasMaxLength(1024);
            builder.HasIndex(entity => entity.QcTaskId).IsUnique();
            builder.HasIndex(entity => new { entity.TenantId, entity.WarehouseId, entity.DecisionResult });
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
            ?? "Host=localhost;Database=BusinessDb;Username=postgres;Password=postgres";

        var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ")
            ?? "amqp://guest:guest@localhost:5672";

        services.AddSingleton<VersionedEntitySaveChangesInterceptor>();
        services.AddSingleton<DomainEventDispatcher>();
        services.AddSingleton<DomainEventInterceptor>();

        services.AddDbContext<BusinessDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<VersionedEntitySaveChangesInterceptor>());
            options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventInterceptor>());
        });
        services.AddScoped<IBusinessDbContext>(serviceProvider => serviceProvider.GetRequiredService<BusinessDbContext>());
        services.AddScoped<CreateInboundNoticeHandler>();
        services.AddScoped<RecordReceiptHandler>();
        services.AddScoped<FinalizeQcDecisionHandler>();
        services.AddScoped<GetQcTasksHandler>();
        services.AddScoped<GetQcTaskByIdHandler>();
        services.AddScoped<GetQcEvidenceHandler>();
        services.AddScoped<GetSkuQualityProfileHandler>();

        services.AddScoped<WmsAi.Inbound.Domain.Inbound.IInboundNoticeRepository, WmsAi.Inbound.Infrastructure.Repositories.InboundNoticeRepository>();
        services.AddScoped<WmsAi.Inbound.Domain.Receipts.IReceiptRepository, WmsAi.Inbound.Infrastructure.Repositories.ReceiptRepository>();
        services.AddScoped<WmsAi.Inbound.Domain.Qc.IQcTaskRepository, WmsAi.Inbound.Infrastructure.Repositories.QcTaskRepository>();
        services.AddScoped<WmsAi.Inbound.Domain.Qc.IQcDecisionRepository, WmsAi.Inbound.Infrastructure.Repositories.QcDecisionRepository>();

        services.AddCap(options =>
        {
            options.UseEntityFramework<BusinessDbContext>();
            options.UseRabbitMQ(rabbitOptions =>
            {
                rabbitOptions.ConnectionFactoryOptions = factory =>
                {
                    factory.Uri = new Uri(rabbitMqConnection);
                };
                rabbitOptions.ExchangeName = "wmsai.events";
            });
        });

        services.AddScoped<WmsAi.Inbound.Infrastructure.Events.CapEventPublisher>();
        services.AddScoped<WmsAi.Inbound.Application.Receipts.IEventPublisher>(sp =>
            sp.GetRequiredService<WmsAi.Inbound.Infrastructure.Events.CapEventPublisher>());
        services.AddScoped<WmsAi.Inbound.Application.Qc.IEventPublisher>(sp =>
            sp.GetRequiredService<WmsAi.Inbound.Infrastructure.Events.CapEventPublisher>());

        services.AddScoped<WmsAi.Inbound.Infrastructure.Events.PlatformEventConsumer>();

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
