using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class User : TenantAuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
    
    public UserStatus Status { get; set; } = UserStatus.Active;
    
    public DateTime? LastLoginAt { get; set; }
    
    public int FailedLoginAttempts { get; set; } = 0;
    
    public DateTime? LockoutEnd { get; set; }
    
    public bool EmailConfirmed { get; set; } = false;
    
    public string? EmailConfirmationToken { get; set; }
    
    public string? PasswordResetToken { get; set; }
    
    public DateTime? PasswordResetTokenExpiry { get; set; }
    
    [MaxLength(100)]
    public string? TimeZone { get; set; } = "UTC";
    
    [MaxLength(10)]
    public string? Culture { get; set; } = "en-US";
    
    public string? Preferences { get; set; } // JSON
    
    // SSO fields
    [MaxLength(50)]
    public string? ExternalProvider { get; set; }
    
    [MaxLength(256)]
    public string? ExternalId { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
    
    // Navigation
    public virtual Tenant? Tenant { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

public enum UserStatus
{
    Pending = 0,
    Active = 1,
    Inactive = 2,
    Locked = 3
}
