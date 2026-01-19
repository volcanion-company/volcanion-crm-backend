using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using CrmSaas.Api.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CrmSaas.Api.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, Guid? entityId = null, string? entityName = null,
        object? oldValues = null, object? newValues = null, IEnumerable<string>? changedProperties = null);
    Task LogAsync(AuditLog auditLog);
}

public class AuditService : IAuditService
{
    private readonly MasterDbContext _masterDb;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        MasterDbContext masterDb,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _masterDb = masterDb;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(string action, string entityType, Guid? entityId = null, string? entityName = null,
        object? oldValues = null, object? newValues = null, IEnumerable<string>? changedProperties = null)
    {
        var context = _httpContextAccessor.HttpContext;
        var userId = GetCurrentUserId();
        
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            UserId = userId,
            UserEmail = context?.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            UserName = context?.User?.Identity?.Name,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            ChangedProperties = changedProperties != null ? JsonSerializer.Serialize(changedProperties) : null,
            Timestamp = DateTime.UtcNow,
            IpAddress = GetIpAddress(),
            UserAgent = context?.Request.Headers.UserAgent.ToString(),
            RequestPath = context?.Request.Path.Value,
            HttpMethod = context?.Request.Method
        };

        await LogAsync(auditLog);
    }

    public async Task LogAsync(AuditLog auditLog)
    {
        try
        {
            _masterDb.AuditLogs.Add(auditLog);
            await _masterDb.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save audit log for action {Action} on {EntityType}", 
                auditLog.Action, auditLog.EntityType);
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        
        return null;
    }

    private string? GetIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
