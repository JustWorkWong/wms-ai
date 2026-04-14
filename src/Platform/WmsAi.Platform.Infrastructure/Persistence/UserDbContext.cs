using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WmsAi.Platform.Application.Abstractions;
using WmsAi.Platform.Application.Tenants;
using WmsAi.Platform.Domain.Tenants;
using WmsAi.Platform.Domain.Users;
using WmsAi.SharedKernel.Persistence;

namespace WmsAi.Platform.Infrastructure.Persistence;

public sealed class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options), IPlatformUserDbContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public Task AddTenantAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        Tenants.Add(tenant);
        return Task.CompletedTask;
    }

    public Task AddWarehouseAsync(Warehouse warehouse, CancellationToken cancellationToken)
    {
        Warehouses.Add(warehouse);
        return Task.CompletedTask;
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task AddMembershipAsync(Membership membership, CancellationToken cancellationToken)
    {
        Memberships.Add(membership);
        return Task.CompletedTask;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.ToTable("tenants");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Code).HasMaxLength(64);
            builder.Property(entity => entity.Name).HasMaxLength(256);
            builder.Property(entity => entity.Status).HasConversion<string>();
            builder.HasIndex(entity => entity.Code).IsUnique();
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });

        modelBuilder.Entity<Warehouse>(builder =>
        {
            builder.ToTable("warehouses");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.TenantId);
            builder.Property(entity => entity.Code).HasMaxLength(64);
            builder.Property(entity => entity.Name).HasMaxLength(256);
            builder.Property(entity => entity.Status).HasConversion<string>();
            builder.HasIndex(entity => new { entity.TenantId, entity.Code }).IsUnique();
            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(entity => entity.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.LoginName).HasMaxLength(128);
            builder.Property(entity => entity.Status).HasConversion<string>();
            builder.HasIndex(entity => entity.LoginName).IsUnique();
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });

        modelBuilder.Entity<Membership>(builder =>
        {
            builder.ToTable("memberships");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.TenantId);
            builder.Property(entity => entity.WarehouseId);
            builder.Property(entity => entity.UserId);
            builder.Property(entity => entity.Role).HasMaxLength(64);
            builder.Property(entity => entity.Status).HasConversion<string>();
            builder.HasIndex(entity => new { entity.TenantId, entity.WarehouseId, entity.UserId }).IsUnique();
            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(entity => entity.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(entity => entity.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(entity => entity.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });
    }
}

public static class PlatformModuleExtensions
{
    public static IServiceCollection AddPlatformModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("UserDb")
            ?? "Data Source=wmsai-platform-user.db";

        services.AddSingleton<VersionedEntitySaveChangesInterceptor>();
        services.AddDbContext<UserDbContext>((serviceProvider, options) =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<VersionedEntitySaveChangesInterceptor>());
        });
        services.AddScoped<IPlatformUserDbContext>(serviceProvider => serviceProvider.GetRequiredService<UserDbContext>());
        services.AddScoped<CreateTenantHandler>();

        services.AddScoped<WmsAi.Platform.Domain.Tenants.ITenantRepository, WmsAi.Platform.Infrastructure.Repositories.TenantRepository>();
        services.AddScoped<WmsAi.Platform.Domain.Tenants.IWarehouseRepository, WmsAi.Platform.Infrastructure.Repositories.WarehouseRepository>();
        services.AddScoped<WmsAi.Platform.Domain.Users.IUserRepository, WmsAi.Platform.Infrastructure.Repositories.UserRepository>();

        return services;
    }
}

public static class PlatformDatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
