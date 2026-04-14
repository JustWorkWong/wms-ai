using Hangfire;
using Microsoft.EntityFrameworkCore;
using WmsAi.Inbound.Infrastructure.Persistence;

namespace WmsAi.Operations.Host.Jobs;

public class BuildDailyQcMetricsJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BuildDailyQcMetricsJob> _logger;

    public BuildDailyQcMetricsJob(IServiceProvider serviceProvider, ILogger<BuildDailyQcMetricsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Building daily QC metrics");

        await using var scope = _serviceProvider.CreateAsyncScope();
        var businessDb = scope.ServiceProvider.GetRequiredService<BusinessDbContext>();

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var today = yesterday.AddDays(1);

        // Count total tasks (no CreatedAt property, so we count all)
        var totalTasks = await businessDb.QcTasks.CountAsync();

        var completedTasks = await businessDb.QcTasks
            .Where(t => t.Status == WmsAi.Inbound.Domain.Qc.QcTaskStatus.Completed)
            .CountAsync();

        // Count decisions by ReviewedAt
        var totalDecisions = await businessDb.QcDecisions
            .Where(d => d.ReviewedAt >= yesterday && d.ReviewedAt < today)
            .CountAsync();

        var acceptedDecisions = await businessDb.QcDecisions
            .Where(d => d.ReviewedAt >= yesterday
                && d.ReviewedAt < today
                && d.DecisionResult == WmsAi.Inbound.Domain.Qc.QcDecisionResult.Accepted)
            .CountAsync();

        _logger.LogInformation(
            "Daily QC Metrics for {Date}: TotalTasks={Total}, CompletedTasks={Completed}, TotalDecisions={Decisions}, AcceptedDecisions={Accepted}",
            yesterday.ToString("yyyy-MM-dd"), totalTasks, completedTasks, totalDecisions, acceptedDecisions);

        _logger.LogInformation("Completed building daily QC metrics");
    }
}
