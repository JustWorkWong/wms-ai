using Hangfire;

namespace WmsAi.Operations.Host.Services;

public class JobScheduler : IHostedService
{
    private readonly ILogger<JobScheduler> _logger;

    public JobScheduler(ILogger<JobScheduler> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduling recurring background jobs");

        // Scan for stuck AI runs every 10 minutes
        RecurringJob.AddOrUpdate<Jobs.ScanPendingAiRunsJob>(
            "scan-pending-ai-runs",
            job => job.ExecuteAsync(),
            "*/10 * * * *"); // Every 10 minutes

        _logger.LogInformation("Scheduled job: scan-pending-ai-runs (every 10 minutes)");

        // Build daily metrics at 1 AM
        RecurringJob.AddOrUpdate<Jobs.BuildDailyQcMetricsJob>(
            "build-daily-qc-metrics",
            job => job.ExecuteAsync(),
            "0 1 * * *"); // Daily at 1 AM

        _logger.LogInformation("Scheduled job: build-daily-qc-metrics (daily at 1 AM)");

        // Cleanup expired sessions weekly on Sunday at 2 AM
        RecurringJob.AddOrUpdate<Jobs.CleanupExpiredSessionsJob>(
            "cleanup-expired-sessions",
            job => job.ExecuteAsync(),
            "0 2 * * 0"); // Weekly on Sunday at 2 AM

        _logger.LogInformation("Scheduled job: cleanup-expired-sessions (weekly on Sunday at 2 AM)");

        _logger.LogInformation("All recurring jobs scheduled successfully");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping job scheduler");
        return Task.CompletedTask;
    }
}
