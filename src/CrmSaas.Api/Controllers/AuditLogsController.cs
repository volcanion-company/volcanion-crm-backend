using CrmSaas.Api.Authorization;
using CrmSaas.Api.Common;
using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using CrmSaas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Controllers;

[Authorize]
public class AuditLogsController : BaseController
{
    private readonly MasterDbContext _masterDb;
    private readonly ICurrentUserService _currentUser;

    public AuditLogsController(MasterDbContext masterDb, ICurrentUserService currentUser)
    {
        _masterDb = masterDb;
        _currentUser = currentUser;
    }

    [HttpGet]
    [RequirePermission(Permissions.AuditView)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogResponse>>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = _masterDb.AuditLogs
            .AsNoTracking()
            .Where(a => a.TenantId == _currentUser.TenantId)
            .WhereIf(!string.IsNullOrEmpty(action), a => a.Action == action)
            .WhereIf(!string.IsNullOrEmpty(entityType), a => a.EntityType == entityType)
            .WhereIf(userId.HasValue, a => a.UserId == userId)
            .WhereIf(startDate.HasValue, a => a.Timestamp >= startDate)
            .WhereIf(endDate.HasValue, a => a.Timestamp <= endDate)
            .ApplySorting(pagination.SortBy ?? "Timestamp", pagination.SortDescending);

        var result = await query
            .Select(a => new AuditLogResponse
            {
                Id = a.Id,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                EntityName = a.EntityName,
                UserId = a.UserId,
                UserEmail = a.UserEmail,
                IpAddress = a.IpAddress,
                Timestamp = a.Timestamp
            })
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.AuditView)]
    public async Task<ActionResult<ApiResponse<AuditLogDetailResponse>>> GetById(Guid id)
    {
        var log = await _masterDb.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _currentUser.TenantId);

        if (log == null)
        {
            return NotFoundResponse<AuditLogDetailResponse>($"Audit log with id {id} not found");
        }

        return OkResponse(new AuditLogDetailResponse
        {
            Id = log.Id,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            EntityName = log.EntityName,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            ChangedProperties = log.ChangedProperties,
            UserId = log.UserId,
            UserEmail = log.UserEmail,
            UserName = log.UserName,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            RequestPath = log.RequestPath,
            HttpMethod = log.HttpMethod,
            AdditionalData = log.AdditionalData,
            Timestamp = log.Timestamp
        });
    }

    [HttpGet("actions")]
    [RequirePermission(Permissions.AuditView)]
    public ActionResult<ApiResponse<List<string>>> GetActions()
    {
        var actions = new List<string>
        {
            AuditActions.Create,
            AuditActions.Update,
            AuditActions.Delete,
            AuditActions.SoftDelete,
            AuditActions.Restore,
            AuditActions.Login,
            AuditActions.Logout,
            AuditActions.LoginFailed,
            AuditActions.PasswordChanged,
            AuditActions.PasswordReset,
            AuditActions.StatusChange,
            AuditActions.Assign,
            AuditActions.Convert,
            AuditActions.View,
            AuditActions.Import,
            AuditActions.Export
        };

        return OkResponse(actions);
    }

    [HttpGet("entity-types")]
    [RequirePermission(Permissions.AuditView)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetEntityTypes()
    {
        var entityTypes = await _masterDb.AuditLogs
            .Where(a => a.TenantId == _currentUser.TenantId)
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        return OkResponse(entityTypes);
    }

    [HttpGet("statistics")]
    [RequirePermission(Permissions.AuditView)]
    public async Task<ActionResult<ApiResponse<AuditStatistics>>> GetStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var logs = await _masterDb.AuditLogs
            .AsNoTracking()
            .Where(a => a.TenantId == _currentUser.TenantId)
            .Where(a => a.Timestamp >= start && a.Timestamp <= end)
            .ToListAsync();

        var stats = new AuditStatistics
        {
            TotalActions = logs.Count,
            ByAction = logs
                .GroupBy(a => a.Action)
                .Select(g => new ActionCount
                {
                    Action = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(a => a.Count)
                .ToList(),
            ByEntityType = logs
                .GroupBy(a => a.EntityType)
                .Select(g => new EntityTypeCount
                {
                    EntityType = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(e => e.Count)
                .ToList(),
            ByUser = logs
                .GroupBy(a => new { a.UserId, a.UserEmail })
                .Select(g => new UserActivityCount
                {
                    UserId = g.Key.UserId,
                    UserEmail = g.Key.UserEmail,
                    ActionCount = g.Count()
                })
                .OrderByDescending(u => u.ActionCount)
                .Take(10)
                .ToList(),
            ByDay = logs
                .GroupBy(a => a.Timestamp.Date)
                .Select(g => new DailyActivity
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList()
        };

        return OkResponse(stats);
    }
}

// Response DTOs
public class AuditLogResponse
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? EntityName { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditLogDetailResponse : AuditLogResponse
{
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedProperties { get; set; }
    public string? UserName { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public string? AdditionalData { get; set; }
}

public class AuditStatistics
{
    public int TotalActions { get; set; }
    public List<ActionCount> ByAction { get; set; } = [];
    public List<EntityTypeCount> ByEntityType { get; set; } = [];
    public List<UserActivityCount> ByUser { get; set; } = [];
    public List<DailyActivity> ByDay { get; set; } = [];
}

public class ActionCount
{
    public string Action { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class EntityTypeCount
{
    public string EntityType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class UserActivityCount
{
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public int ActionCount { get; set; }
}

public class DailyActivity
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
