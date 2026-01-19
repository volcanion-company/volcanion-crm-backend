using System.IdentityModel.Tokens.Jwt;
using CrmSaas.Api.Configuration;
using CrmSaas.Api.Entities;
using CrmSaas.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CrmSaas.Api.MultiTenancy;

public class TenantResolver : ITenantResolver
{
    private readonly TenantSettings _settings;
    private readonly MasterDbContext _masterDb;
    private readonly ILogger<TenantResolver> _logger;

    public TenantResolver(
        IOptions<TenantSettings> settings,
        MasterDbContext masterDb,
        ILogger<TenantResolver> logger)
    {
        _settings = settings.Value;
        _masterDb = masterDb;
        _logger = logger;
    }

    public async Task<Tenant?> ResolveAsync(HttpContext context)
    {
        string? tenantIdentifier = null;

        // Try to resolve tenant from different sources based on strategy
        tenantIdentifier = _settings.ResolutionStrategy switch
        {
            TenantResolutionStrategy.Header => ResolveFromHeader(context),
            TenantResolutionStrategy.Subdomain => ResolveFromSubdomain(context),
            TenantResolutionStrategy.Token => await ResolveFromTokenAsync(context),
            TenantResolutionStrategy.QueryString => ResolveFromQueryString(context),
            _ => ResolveFromHeader(context)
        };

        // Fallback strategies
        if (string.IsNullOrEmpty(tenantIdentifier))
        {
            tenantIdentifier = ResolveFromHeader(context);
        }
        
        if (string.IsNullOrEmpty(tenantIdentifier) && _settings.AllowSubdomainResolution)
        {
            tenantIdentifier = ResolveFromSubdomain(context);
        }

        if (string.IsNullOrEmpty(tenantIdentifier))
        {
            tenantIdentifier = await ResolveFromTokenAsync(context);
        }

        if (string.IsNullOrEmpty(tenantIdentifier))
        {
            tenantIdentifier = _settings.DefaultTenantId;
        }

        // Find tenant by identifier
        var tenant = await _masterDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => 
                t.Identifier == tenantIdentifier || 
                t.Subdomain == tenantIdentifier ||
                t.Id.ToString() == tenantIdentifier);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for identifier: {TenantIdentifier}", tenantIdentifier);
            return null;
        }

        if (tenant.Status != TenantStatus.Active)
        {
            _logger.LogWarning("Tenant {TenantId} is not active. Status: {Status}", tenant.Id, tenant.Status);
            return null;
        }

        return tenant;
    }

    private string? ResolveFromHeader(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_settings.HeaderName, out var headerValue))
        {
            return headerValue.FirstOrDefault();
        }
        return null;
    }

    private string? ResolveFromSubdomain(HttpContext context)
    {
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        
        if (parts.Length >= 3)
        {
            return parts[0];
        }
        
        return null;
    }

    private Task<string?> ResolveFromTokenAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Task.FromResult<string?>(null);
        }

        var token = authHeader["Bearer ".Length..];
        
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var tenantClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_id");
            
            return Task.FromResult(tenantClaim?.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract tenant from token");
            return Task.FromResult<string?>(null);
        }
    }

    private string? ResolveFromQueryString(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("tenantId", out var tenantId))
        {
            return tenantId.FirstOrDefault();
        }
        return null;
    }
}
