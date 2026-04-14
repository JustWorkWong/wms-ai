using WmsAi.SharedKernel.Domain;

namespace WmsAi.Platform.Domain.Users;

public sealed class User : AggregateRoot
{
    private User()
    {
    }

    public User(string loginName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loginName);
        LoginName = loginName.Trim();
        Status = "active";

        RaiseDomainEvent(new UserCreatedEvent(Id, LoginName));
    }

    public string LoginName { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;
}
