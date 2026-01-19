using Microsoft.Extensions.Diagnostics.HealthChecks;
using CrmSaas.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly MasterDbContext _masterContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(MasterDbContext masterContext, ILogger<DatabaseHealthCheck> logger)
    {
        _masterContext = masterContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check database connectivity
            await _masterContext.Database.CanConnectAsync(cancellationToken);

            // Get tenant count as a metric
            var tenantCount = await _masterContext.Tenants.CountAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database is accessible", new Dictionary<string, object>
            {
                { "tenantCount", tenantCount },
                { "timestamp", DateTime.UtcNow }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database is not accessible", ex);
        }
    }
}
