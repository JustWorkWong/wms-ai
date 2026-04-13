namespace WmsAi.SharedKernel.Domain;

public abstract class AggregateRoot
{
    protected AggregateRoot()
    {
    }

    public Guid Id { get; protected set; } = Guid.NewGuid();

    public long Version { get; protected set; } = 1;
}
