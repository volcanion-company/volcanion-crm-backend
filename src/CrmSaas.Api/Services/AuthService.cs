using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CrmSaas.Api.Configuration;
using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CrmSaas.Api.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress = null);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
    Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress = null);
    Task<bool> RevokeAllTokensAsync(Guid userId, string? ipAddress = null);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class AuthService : IAuthService
{
    private readonly TenantDbContext _db;
    private readonly MasterDbContext _masterDb;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        TenantDbContext db,
        MasterDbContext masterDb,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _db = db;
        _masterDb = masterDb;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress = null)
    {
        // Load user with roles and role permissions (but not Permission navigation)
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r!.RolePermissions)
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for email {Email}", request.Email);
            return AuthResult.Failed("Invalid email or password");
        }

        if (user.Status == UserStatus.Locked)
        {
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                _logger.LogWarning("Login failed: User {UserId} is locked out", user.Id);
                return AuthResult.Failed("Account is locked. Please try again later.");
            }
            
            // Reset lockout
            user.Status = UserStatus.Active;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            
            if (user.FailedLoginAttempts >= 5)
            {
                user.Status = UserStatus.Locked;
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                _logger.LogWarning("User {UserId} has been locked out due to too many failed attempts", user.Id);
            }
            
            await _db.SaveChangesAsync();
            return AuthResult.Failed("Invalid email or password");
        }

        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("Login failed: User {UserId} is not active", user.Id);
            return AuthResult.Failed("Account is not active");
        }

        // Reset failed attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LastLoginAt = DateTime.UtcNow;

        var accessToken = await GenerateAccessTokenAsync(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress);

        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return AuthResult.Success(accessToken, refreshToken.Token, user);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u!.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r!.RolePermissions)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null)
        {
            _logger.LogWarning("Refresh token not found");
            return AuthResult.Failed("Invalid refresh token");
        }

        if (!token.IsActive)
        {
            _logger.LogWarning("Refresh token is not active for user {UserId}", token.UserId);
            return AuthResult.Failed("Invalid refresh token");
        }

        var user = token.User!;

        // Revoke old token
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = "Replaced by new token";

        // Generate new tokens
        var newAccessToken = await GenerateAccessTokenAsync(user);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress);
        
        token.ReplacedByToken = newRefreshToken.Token;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Token refreshed for user {UserId}", user.Id);

        return AuthResult.Success(newAccessToken, newRefreshToken.Token, user);
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress = null)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null || !token.IsActive)
        {
            return false;
        }

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = "Revoked by user";

        await _db.SaveChangesAsync();

        _logger.LogInformation("Token revoked for user {UserId}", token.UserId);

        return true;
    }

    public async Task<bool> RevokeAllTokensAsync(Guid userId, string? ipAddress = null)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = "Revoked by user - logout all devices";
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("All tokens revoked for user {UserId}", userId);

        return true;
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    private async Task<string> GenerateAccessTokenAsync(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new("tenant_id", user.TenantId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Get all permission IDs from user's roles
        var permissionIds = user.UserRoles
            .Where(ur => ur.Role != null)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToList();

        // Load permissions from MasterDb
        var permissions = await _masterDb.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .ToListAsync();

        // Add role claims
        foreach (var userRole in user.UserRoles)
        {
            if (userRole.Role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                claims.Add(new Claim("data_scope", ((int)userRole.Role.DataScope).ToString()));

                // Add permission claims
                foreach (var rp in userRole.Role.RolePermissions)
                {
                    var permission = permissions.FirstOrDefault(p => p.Id == rp.PermissionId);
                    if (permission != null)
                    {
                        claims.Add(new Claim("permission", permission.Code));
                    }
                }
            }
        }

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string? ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = GenerateSecureToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _db.RefreshTokens.Add(refreshToken);
        
        // Clean up old tokens
        var oldTokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && (t.ExpiresAt < DateTime.UtcNow || t.RevokedAt != null))
            .ToListAsync();
        
        _db.RefreshTokens.RemoveRange(oldTokens);

        return refreshToken;
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public UserDto? User { get; set; }

    public static AuthResult Success(string accessToken, string refreshToken, User user) => new()
    {
        IsSuccess = true,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        User = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            TenantId = user.TenantId,
            Roles = user.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Name).ToList(),
            Permissions = user.UserRoles
                .Where(ur => ur.Role != null)
                .SelectMany(ur => ur.Role!.RolePermissions)
                .Where(rp => rp.Permission != null)
                .Select(rp => rp.Permission!.Code)
                .Distinct()
                .ToList()
        }
    };

    public static AuthResult Failed(string error) => new()
    {
        IsSuccess = false,
        Error = error
    };
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
}
