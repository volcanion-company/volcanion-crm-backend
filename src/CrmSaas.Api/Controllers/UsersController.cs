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
public class UsersController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly IAuthService _authService;

    public UsersController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService,
        IAuthService authService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
        _authService = authService;
    }

    [HttpGet]
    [RequirePermission(Permissions.UserView)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserResponse>>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] UserStatus? status = null)
    {
        var query = _db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .WhereIf(status.HasValue, u => u.Status == status!.Value)
            .WhereIf(!string.IsNullOrEmpty(pagination.Search), u =>
                u.Email.Contains(pagination.Search!) ||
                u.FirstName.Contains(pagination.Search!) ||
                u.LastName.Contains(pagination.Search!))
            .ApplySorting(pagination.SortBy ?? "CreatedAt", pagination.SortDescending);

        var result = await query
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FullName,
                Status = u.Status,
                Roles = u.UserRoles.Select(ur => ur.Role!.Name).ToList(),
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            })
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.UserView)]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> GetById(Guid id)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFoundResponse<UserDetailResponse>($"User with id {id} not found");
        }

        return OkResponse(MapToDetailResponse(user));
    }

    [HttpPost]
    [RequirePermission(Permissions.UserCreate)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Create([FromBody] CreateUserRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequestResponse<UserResponse>("Email already exists");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _authService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            TimeZone = request.TimeZone ?? "UTC",
            Culture = request.Culture ?? "en-US",
            Status = UserStatus.Active,
            CreatedBy = _currentUser.UserId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Assign roles
        if (request.RoleIds != null && request.RoleIds.Any())
        {
            foreach (var roleId in request.RoleIds)
            {
                _db.Set<UserRole>().Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId
                });
            }
            await _db.SaveChangesAsync();
        }

        await _auditService.LogAsync(AuditActions.Create, nameof(User), user.Id, user.Email);

        return CreatedResponse(new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.UserUpdate)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
        {
            return NotFoundResponse<UserResponse>($"User with id {id} not found");
        }

        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.Phone = request.Phone ?? user.Phone;
        user.TimeZone = request.TimeZone ?? user.TimeZone;
        user.Culture = request.Culture ?? user.Culture;
        user.UpdatedBy = _currentUser.UserId;

        // Update roles if provided
        if (request.RoleIds != null)
        {
            var existingRoles = await _db.Set<UserRole>()
                .Where(ur => ur.UserId == user.Id)
                .ToListAsync();
            
            _db.Set<UserRole>().RemoveRange(existingRoles);
            
            foreach (var roleId in request.RoleIds)
            {
                _db.Set<UserRole>().Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId
                });
            }
        }

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(User), user.Id, user.Email);

        return OkResponse(new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost("{id:guid}/activate")]
    [RequirePermission(Permissions.UserUpdate)]
    public async Task<ActionResult<ApiResponse>> Activate(Guid id)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
        {
            return NotFoundResponse($"User with id {id} not found");
        }

        user.Status = UserStatus.Active;
        user.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(User), user.Id, user.Email);

        return OkResponse("User activated");
    }

    [HttpPost("{id:guid}/deactivate")]
    [RequirePermission(Permissions.UserUpdate)]
    public async Task<ActionResult<ApiResponse>> Deactivate(Guid id)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
        {
            return NotFoundResponse($"User with id {id} not found");
        }

        if (user.Id == _currentUser.UserId)
        {
            return BadRequestResponse("Cannot deactivate yourself");
        }

        user.Status = UserStatus.Inactive;
        user.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(User), user.Id, user.Email);

        return OkResponse("User deactivated");
    }

    [HttpPost("{id:guid}/reset-password")]
    [RequirePermission(Permissions.UserUpdate)]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword(Guid id)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
        {
            return NotFoundResponse<string>($"User with id {id} not found");
        }

        var newPassword = GenerateTemporaryPassword();
        user.PasswordHash = _authService.HashPassword(newPassword);
        user.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.PasswordReset, nameof(User), user.Id, user.Email);

        return OkResponse(newPassword, "Password reset successful. New password generated.");
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await _db.Users.FindAsync(_currentUser.UserId);

        if (user == null)
        {
            return NotFoundResponse("User not found");
        }

        if (!_authService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequestResponse("Current password is incorrect");
        }

        user.PasswordHash = _authService.HashPassword(request.NewPassword);
        user.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.PasswordChanged, nameof(User), user.Id, user.Email);

        return OkResponse("Password changed successfully");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.UserDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
        {
            return NotFoundResponse($"User with id {id} not found");
        }

        if (user.Id == _currentUser.UserId)
        {
            return BadRequestResponse("Cannot delete yourself");
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Delete, nameof(User), user.Id, user.Email);

        return OkResponse("User deleted");
    }

    private static UserDetailResponse MapToDetailResponse(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        Phone = user.Phone,
        AvatarUrl = user.AvatarUrl,
        Status = user.Status,
        Roles = user.UserRoles.Select(ur => new RoleInfo
        {
            Id = ur.Role!.Id,
            Name = ur.Role.Name
        }).ToList(),
        TimeZone = user.TimeZone,
        Culture = user.Culture,
        EmailConfirmed = user.EmailConfirmed,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

// DTOs
public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public List<string> Roles { get; set; } = [];
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDetailResponse : UserResponse
{
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? TimeZone { get; set; }
    public string? Culture { get; set; }
    public bool EmailConfirmed { get; set; }
    public new List<RoleInfo> Roles { get; set; } = [];
    public DateTime? UpdatedAt { get; set; }
}

public class RoleInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? TimeZone { get; set; }
    public string? Culture { get; set; }
    public List<Guid>? RoleIds { get; set; }
}

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? TimeZone { get; set; }
    public string? Culture { get; set; }
    public List<Guid>? RoleIds { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
