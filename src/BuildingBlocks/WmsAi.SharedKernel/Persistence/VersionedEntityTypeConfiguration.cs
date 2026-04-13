using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WmsAi.SharedKernel.Domain;

namespace WmsAi.SharedKernel.Persistence;

public static class VersionedEntityTypeConfiguration
{
    public static void ApplyVersion<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : AggregateRoot
    {
        builder.Property(entity => entity.Version).IsConcurrencyToken();
    }
}
