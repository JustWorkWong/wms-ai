using Hangfire;
using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Infrastructure.Persistence;

namespace WmsAi.Operations.Host.Jobs;

public class ScanPendingAiRunsJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScanPendingAiRunsJob> _logger;

    public ScanPendingAiRunsJob(IServiceProvider serviceProvider, ILogger<ScanPendingAiRunsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Scanning for stuck AI workflow runs");

        await using var scope = _serviceProvider.CreateAsyncScope();
        var aiDb = scope.ServiceProvider.GetRequiredService<AiDbContext>();

        var cutoffTime = DateTime.UtcNow.AddMinutes(-30);

        var stuckRuns = await aiDb.MafWorkflowRuns
            .Where(r => r.Status == WmsAi.AiGateway.Domain.Workflows.WorkflowStatus.Running
                && r.UpdatedAt < cutoffTime)
            .ToListAsync();

        if (stuckRuns.Count == 0)
        {
            _logger.LogInformation("No stuck workflow runs found");
            return;
        }

        _logger.LogWarning("Found {Count} stuck workflow runs", stuckRuns.Count);

        foreach (var run in stuckRuns)
        {
            run.Fail("Timeout - no progress for 30 minutes");
            _logger.LogWarning("Workflow run {RunId} marked as failed due to timeout", run.Id);
        }

        await aiDb.SaveChangesAsync();
        _logger.LogInformation("Completed scanning for stuck AI workflow runs");
    }
}
