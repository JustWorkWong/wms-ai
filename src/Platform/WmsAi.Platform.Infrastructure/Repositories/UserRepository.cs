using Microsoft.EntityFrameworkCore;
using WmsAi.Platform.Domain.Users;
using WmsAi.Platform.Infrastructure.Persistence;

namespace WmsAi.Platform.Infrastructure.Repositories;

public sealed class UserRepository(UserDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<User?> GetByLoginNameAsync(string loginName, CancellationToken cancellationToken = default)
    {
        return context.Users.FirstOrDefaultAsync(u => u.LoginName == loginName, cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByLoginNameAsync(string loginName, CancellationToken cancellationToken = default)
    {
        return context.Users.AnyAsync(u => u.LoginName == loginName, cancellationToken);
    }
}
