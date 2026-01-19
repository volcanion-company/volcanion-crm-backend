namespace CrmSaas.Api.MultiTenancy;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver, ITenantContext tenantContext)
    {
        // Skip tenant resolution for certain paths
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        
        if (ShouldSkipTenantResolution(path))
        {
            await _next(context);
            return;
        }

        var tenant = await tenantResolver.ResolveAsync(context);

        if (tenant != null)
        {
            tenantContext.SetTenant(tenant);
            _logger.LogDebug("Resolved tenant: {TenantId} - {TenantName}", tenant.Id, tenant.Name);
        }
        else
        {
            _logger.LogDebug("No tenant resolved for request: {Path}", path);
        }

        await _next(context);
    }

    private static bool ShouldSkipTenantResolution(string path)
    {
        var skipPaths = new[]
        {
            "/health",
            "/swagger",
            "/scalar",
            "/openapi",
            "/.well-known",
            "/api/v1/auth/login",
            "/api/v1/auth/refresh",
            "/api/v1/tenants/register"
        };

        return skipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}
