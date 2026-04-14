using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WmsAi.SharedKernel.Domain;

namespace WmsAi.SharedKernel.Persistence;

public sealed class DomainEventDispatcher
{
    private readonly List<IDomainEvent> _events = [];

    public void CollectEvents(DbContext context)
    {
        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            _events.AddRange(aggregate.DomainEvents);
            aggregate.ClearDomainEvents();
        }
    }

    public IReadOnlyList<IDomainEvent> GetCollectedEvents() => _events.AsReadOnly();

    public void Clear() => _events.Clear();
}

public sealed class DomainEventInterceptor(DomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            dispatcher.CollectEvents(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            dispatcher.CollectEvents(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }
}
