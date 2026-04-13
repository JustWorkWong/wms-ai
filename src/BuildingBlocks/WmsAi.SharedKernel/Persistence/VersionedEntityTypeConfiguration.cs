using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WmsAi.SharedKernel.Domain;

namespace WmsAi.SharedKernel.Persistence;

public static class VersionedEntityTypeConfiguration
{
    public static void ApplyVersion<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : AggregateRoot
    {
        builder.Property(entity => entity.Version)
            .IsConcurrencyToken()
            .ValueGeneratedNever();
    }
}

public sealed class VersionedEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        IncrementTrackedVersions(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        IncrementTrackedVersions(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void IncrementTrackedVersions(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<AggregateRoot>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(AggregateRoot.Version)).CurrentValue = entry.Entity.Version + 1;
            }
        }
    }
}
