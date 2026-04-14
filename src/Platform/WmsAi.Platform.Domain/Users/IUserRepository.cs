namespace WmsAi.Platform.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByLoginNameAsync(string loginName, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ExistsByLoginNameAsync(string loginName, CancellationToken cancellationToken = default);
}
