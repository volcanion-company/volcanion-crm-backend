using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CrmSaas.Api.HealthChecks;

public class HangfireHealthCheck : IHealthCheck
{
    private readonly ILogger<HangfireHealthCheck> _logger;

    public HangfireHealthCheck(ILogger<HangfireHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            
            var servers = monitoringApi.Servers();
            var serverCount = servers.Count;

            if (serverCount == 0)
            {
                return Task.FromResult(HealthCheckResult.Degraded("No Hangfire servers are running", null, new Dictionary<string, object>
                {
                    { "serverCount", 0 },
                    { "timestamp", DateTime.UtcNow }
                }));
            }

            // Get job counts
            var stats = monitoringApi.GetStatistics();
            
            return Task.FromResult(HealthCheckResult.Healthy("Hangfire is running", new Dictionary<string, object>
            {
                { "serverCount", serverCount },
                { "succeededJobs", stats.Succeeded },
                { "failedJobs", stats.Failed },
                { "recurringJobs", stats.Recurring },
                { "scheduledJobs", stats.Scheduled },
                { "enqueuedJobs", stats.Enqueued },
                { "processingJobs", stats.Processing },
                { "timestamp", DateTime.UtcNow }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire is not accessible", ex));
        }
    }
}
