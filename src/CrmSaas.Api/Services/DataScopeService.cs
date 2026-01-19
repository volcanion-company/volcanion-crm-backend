using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CrmSaas.Api.Services;

public interface IDataScopeService
{
    Expression<Func<T, bool>>? GetDataScopeFilter<T>() where T : TenantAuditableEntity;
    bool CanAccessRecord<T>(T entity) where T : TenantAuditableEntity;
}

public class DataScopeService : IDataScopeService
{
    private readonly ICurrentUserService _currentUserService;

    public DataScopeService(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public Expression<Func<T, bool>>? GetDataScopeFilter<T>() where T : TenantAuditableEntity
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return null;
        }

        var userId = _currentUserService.UserId.Value;
        var dataScope = (DataScope)_currentUserService.DataScope;

        return dataScope switch
        {
            DataScope.Own => GetOwnFilter<T>(userId),
            DataScope.Team => GetTeamFilter<T>(userId),
            DataScope.Department => GetDepartmentFilter<T>(userId),
            DataScope.All => null, // No filter - can see all records in tenant
            _ => GetOwnFilter<T>(userId) // Default to Own
        };
    }

    public bool CanAccessRecord<T>(T entity) where T : TenantAuditableEntity
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return false;
        }

        var userId = _currentUserService.UserId.Value;
        var dataScope = (DataScope)_currentUserService.DataScope;

        return dataScope switch
        {
            DataScope.Own => IsOwnRecord(entity, userId),
            DataScope.Team => IsTeamRecord(entity, userId),
            DataScope.Department => IsDepartmentRecord(entity, userId),
            DataScope.All => true,
            _ => IsOwnRecord(entity, userId)
        };
    }

    private Expression<Func<T, bool>>? GetOwnFilter<T>(Guid userId) where T : TenantAuditableEntity
    {
        // Check if entity has a common owner field
        var type = typeof(T);
        
        // Common patterns for ownership
        if (type.GetProperty("AssignedToUserId") != null)
        {
            return e => EF.Property<Guid?>(e, "AssignedToUserId") == userId || e.CreatedBy == userId;
        }
        
        if (type.GetProperty("OwnerId") != null)
        {
            return e => EF.Property<Guid?>(e, "OwnerId") == userId || e.CreatedBy == userId;
        }

        if (type.GetProperty("PerformedByUserId") != null)
        {
            return e => EF.Property<Guid?>(e, "PerformedByUserId") == userId || e.CreatedBy == userId;
        }

        // Default: records created by user
        return e => e.CreatedBy == userId;
    }

    private Expression<Func<T, bool>>? GetTeamFilter<T>(Guid userId) where T : TenantAuditableEntity
    {
        // TODO: Implement team filter when Team/Department structure is in place
        // For now, return same as Own
        return GetOwnFilter<T>(userId);
    }

    private Expression<Func<T, bool>>? GetDepartmentFilter<T>(Guid userId) where T : TenantAuditableEntity
    {
        // TODO: Implement department filter when Team/Department structure is in place
        // For now, return same as Own
        return GetOwnFilter<T>(userId);
    }

    private bool IsOwnRecord<T>(T entity, Guid userId) where T : TenantAuditableEntity
    {
        var type = typeof(T);
        
        // Check common ownership properties
        if (type.GetProperty("AssignedToUserId") != null)
        {
            var assignedTo = type.GetProperty("AssignedToUserId")?.GetValue(entity) as Guid?;
            if (assignedTo == userId) return true;
        }
        
        if (type.GetProperty("OwnerId") != null)
        {
            var owner = type.GetProperty("OwnerId")?.GetValue(entity) as Guid?;
            if (owner == userId) return true;
        }

        if (type.GetProperty("PerformedByUserId") != null)
        {
            var performer = type.GetProperty("PerformedByUserId")?.GetValue(entity) as Guid?;
            if (performer == userId) return true;
        }

        // Check if user created the record
        return entity.CreatedBy == userId;
    }

    private bool IsTeamRecord<T>(T entity, Guid userId) where T : TenantAuditableEntity
    {
        // TODO: Implement team check when Team/Department structure is in place
        return IsOwnRecord(entity, userId);
    }

    private bool IsDepartmentRecord<T>(T entity, Guid userId) where T : TenantAuditableEntity
    {
        // TODO: Implement department check when Team/Department structure is in place
        return IsOwnRecord(entity, userId);
    }
}
