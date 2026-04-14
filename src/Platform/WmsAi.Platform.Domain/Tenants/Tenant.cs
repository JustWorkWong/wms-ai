using WmsAi.SharedKernel.Domain;

namespace WmsAi.Platform.Domain.Tenants;

public sealed class Tenant : AggregateRoot
{
    private Tenant()
    {
    }

    public Tenant(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Code = code.Trim();
        Name = name.Trim();
        Status = "active";

        RaiseDomainEvent(new TenantCreatedEvent(Id, Code, Name));
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}
