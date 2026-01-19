using CrmSaas.Api.MultiTenancy;
using System.Security.Claims;

namespace CrmSaas.Api.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Name { get; }
    Guid? TenantId { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> Permissions { get; }
    int DataScope { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
    bool HasRole(string role);
    bool HasAnyPermission(params string[] permissions);
    bool HasAllPermissions(params string[] permissions);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantContext _tenantContext;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        ITenantContext tenantContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? Name => User?.FindFirst(ClaimTypes.Name)?.Value;

    public Guid? TenantId
    {
        get
        {
            if (_tenantContext.TenantId.HasValue)
            {
                return _tenantContext.TenantId;
            }

            var tenantClaim = User?.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : null;
        }
    }

    public IEnumerable<string> Roles => 
        User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ?? Enumerable.Empty<string>();

    public IEnumerable<string> Permissions => 
        User?.FindAll("permission")?.Select(c => c.Value) ?? Enumerable.Empty<string>();

    public int DataScope
    {
        get
        {
            var scopeClaim = User?.FindFirst("data_scope")?.Value;
            return int.TryParse(scopeClaim, out var scope) ? scope : 0;
        }
    }

    public bool HasPermission(string permission) => 
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public bool HasRole(string role) => 
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool HasAnyPermission(params string[] permissions) => 
        permissions.Any(p => HasPermission(p));

    public bool HasAllPermissions(params string[] permissions) => 
        permissions.All(p => HasPermission(p));
}
