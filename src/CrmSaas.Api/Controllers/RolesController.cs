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
public class RolesController(
    TenantDbContext db,
    MasterDbContext masterDb,
    ICurrentUserService currentUser,
    IAuditService auditService) : BaseController
{
    [HttpGet]
    [RequirePermission(Permissions.RoleView)]
    public async Task<ActionResult<ApiResponse<List<RoleResponse>>>> GetAll()
    {
        var roles = await db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync();

        var response = roles.Select(r => new RoleResponse
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsSystemRole = r.IsSystemRole,
            DataScope = r.DataScope.ToString(),
            PermissionCount = r.RolePermissions.Count,
            CreatedAt = r.CreatedAt
        }).ToList();

        return OkResponse(response);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.RoleView)]
    public async Task<ActionResult<ApiResponse<RoleDetailResponse>>> GetById(Guid id)
    {
        var role = await db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
        {
            return NotFoundResponse<RoleDetailResponse>($"Role with id {id} not found");
        }

        return OkResponse(new RoleDetailResponse
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            DataScope = role.DataScope.ToString(),
            Permissions = role.RolePermissions.Select(rp => new PermissionResponse
            {
                Id = rp.Permission!.Id,
                Name = rp.Permission.Name,
                Code = rp.Permission.Code,
                Module = rp.Permission.Module,
                Description = rp.Permission.Description
            }).ToList(),
            CreatedAt = role.CreatedAt
        });
    }

    [HttpGet("permissions")]
    [RequirePermission(Permissions.RoleView)]
    public async Task<ActionResult<ApiResponse<List<PermissionGroupResponse>>>> GetAllPermissions()
    {
        var permissions = await masterDb.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var grouped = permissions
            .GroupBy(p => p.Module)
            .Select(g => new PermissionGroupResponse
            {
                Module = g.Key,
                Permissions = g.Select(p => new PermissionResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    Description = p.Description,
                    Module = p.Module
                }).ToList()
            })
            .ToList();

        return OkResponse(grouped);
    }

    [HttpPost]
    [RequirePermission(Permissions.RoleCreate)]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> Create([FromBody] CreateRoleRequest request)
    {
        if (await db.Roles.AnyAsync(r => r.Name == request.Name))
        {
            return BadRequestResponse<RoleResponse>("Role name already exists");
        }

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            DataScope = request.DataScope,
            IsSystemRole = false,
            CreatedBy = currentUser.UserId
        };

        // Add permissions
        if (request.PermissionIds?.Any() == true)
        {
            foreach (var permissionId in request.PermissionIds)
            {
                role.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionId
                });
            }
        }

        db.Roles.Add(role);
        await db.SaveChangesAsync();

        await auditService.LogAsync(AuditActions.Create, nameof(Role), role.Id, role.Name);

        return CreatedResponse(new RoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            DataScope = role.DataScope.ToString(),
            PermissionCount = role.RolePermissions.Count,
            CreatedAt = role.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.RoleUpdate)]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> Update(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
        {
            return NotFoundResponse<RoleResponse>($"Role with id {id} not found");
        }

        if (role.IsSystemRole)
        {
            return BadRequestResponse<RoleResponse>("Cannot modify system roles");
        }

        role.Name = request.Name ?? role.Name;
        role.Description = request.Description ?? role.Description;
        role.DataScope = request.DataScope ?? role.DataScope;
        role.UpdatedBy = currentUser.UserId;

        await db.SaveChangesAsync();

        await auditService.LogAsync(AuditActions.Update, nameof(Role), role.Id, role.Name);

        return OkResponse(new RoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            DataScope = role.DataScope.ToString(),
            PermissionCount = role.RolePermissions.Count,
            CreatedAt = role.CreatedAt
        });
    }

    [HttpPut("{id:guid}/permissions")]
    [RequirePermission(Permissions.RoleUpdate)]
    public async Task<ActionResult<ApiResponse>> UpdatePermissions(Guid id, [FromBody] UpdateRolePermissionsRequest request)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
        {
            return NotFoundResponse($"Role with id {id} not found");
        }

        if (role.IsSystemRole)
        {
            return BadRequestResponse("Cannot modify system role permissions");
        }

        // Remove existing permissions
        db.RolePermissions.RemoveRange(role.RolePermissions);

        // Add new permissions
        foreach (var permissionId in request.PermissionIds)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            });
        }

        await db.SaveChangesAsync();

        await auditService.LogAsync(AuditActions.Update, nameof(Role), role.Id, role.Name);

        return OkResponse("Permissions updated successfully");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.RoleDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var role = await db.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
        {
            return NotFoundResponse($"Role with id {id} not found");
        }

        if (role.IsSystemRole)
        {
            return BadRequestResponse("Cannot delete system roles");
        }

        if (role.UserRoles.Any())
        {
            return BadRequestResponse("Cannot delete role with assigned users");
        }

        role.IsDeleted = true;
        role.DeletedAt = DateTime.UtcNow;
        role.DeletedBy = currentUser.UserId;

        await db.SaveChangesAsync();

        await auditService.LogAsync(AuditActions.SoftDelete, nameof(Role), role.Id, role.Name);

        return OkResponse("Role deleted successfully");
    }
}

// Request/Response DTOs
public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DataScope DataScope { get; set; } = DataScope.Own;
    public List<Guid>? PermissionIds { get; set; }
}

public class UpdateRoleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DataScope? DataScope { get; set; }
}

public class UpdateRolePermissionsRequest
{
    public List<Guid> PermissionIds { get; set; } = [];
}

public class RoleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public string DataScope { get; set; } = string.Empty;
    public int PermissionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RoleDetailResponse : RoleResponse
{
    public List<PermissionResponse> Permissions { get; set; } = [];
}

public class PermissionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class PermissionGroupResponse
{
    public string Module { get; set; } = string.Empty;
    public List<PermissionResponse> Permissions { get; set; } = [];
}
