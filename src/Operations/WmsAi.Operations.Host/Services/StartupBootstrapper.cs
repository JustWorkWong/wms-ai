using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Infrastructure.Persistence;
using WmsAi.Inbound.Infrastructure.Persistence;
using WmsAi.Platform.Infrastructure.Persistence;

namespace WmsAi.Operations.Host.Services;

public class StartupBootstrapper : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupBootstrapper> _logger;

    public StartupBootstrapper(IServiceProvider serviceProvider, ILogger<StartupBootstrapper> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting database bootstrap process");

        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();

            // 按依赖顺序初始化数据库结构
            await ApplyMigrationsAsync<UserDbContext>(scope, "UserDb", cancellationToken);
            await ApplyMigrationsAsync<BusinessDbContext>(scope, "BusinessDb", cancellationToken);
            await ApplyMigrationsAsync<AiDbContext>(scope, "AiDb", cancellationToken);

            // 导入演示数据
            await LoadSeedDataAsync(scope, cancellationToken);

            _logger.LogInformation("Database bootstrap completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database bootstrap failed");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ApplyMigrationsAsync<TContext>(
        IServiceScope scope,
        string dbName,
        CancellationToken cancellationToken) where TContext : DbContext
    {
        _logger.LogInformation("Applying migrations for {DbName}", dbName);

        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        _logger.LogInformation("Migrations applied for {DbName}", dbName);
    }

    private async Task LoadSeedDataAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading seed data");

        var importer = scope.ServiceProvider.GetRequiredService<SeedDataImporter>();
        await importer.ImportAsync(cancellationToken);

        _logger.LogInformation("Seed data loaded");
    }
}
