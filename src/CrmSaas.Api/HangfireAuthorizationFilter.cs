using Hangfire.Dashboard;

namespace CrmSaas.Api;

/// <summary>
/// Simple authorization filter for Hangfire Dashboard in development
/// In production, this should be replaced with proper authentication
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In development, allow all access
        // In production, implement proper authorization
        return true;
    }
}
