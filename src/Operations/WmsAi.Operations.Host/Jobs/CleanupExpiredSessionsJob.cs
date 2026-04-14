using Hangfire;
using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Infrastructure.Persistence;

namespace WmsAi.Operations.Host.Jobs;

public class CleanupExpiredSessionsJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CleanupExpiredSessionsJob> _logger;

    public CleanupExpiredSessionsJob(IServiceProvider serviceProvider, ILogger<CleanupExpiredSessionsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Cleaning up expired AI sessions");

        await using var scope = _serviceProvider.CreateAsyncScope();
        var aiDb = scope.ServiceProvider.GetRequiredService<AiDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        var expiredSessions = await aiDb.MafSessions
            .Where(s => s.Status == WmsAi.AiGateway.Domain.MafSessions.SessionStatus.Completed
                && s.UpdatedAt < cutoffDate)
            .ToListAsync();

        if (expiredSessions.Count == 0)
        {
            _logger.LogInformation("No expired sessions found");
            return;
        }

        _logger.LogInformation("Found {Count} expired sessions to archive", expiredSessions.Count);

        // In production, would move to cold storage or archive table
        // For now, just log the count
        _logger.LogInformation("Expired sessions logged for archival (actual archival not implemented)");

        _logger.LogInformation("Completed cleanup of expired AI sessions");
    }
}
