using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class AuditLog : BaseEntity
{
    public Guid? TenantId { get; set; }
    
    public Guid? UserId { get; set; }
    
    [MaxLength(100)]
    public string? UserEmail { get; set; }
    
    [MaxLength(100)]
    public string? UserName { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    public Guid? EntityId { get; set; }
    
    [MaxLength(200)]
    public string? EntityName { get; set; }
    
    public string? OldValues { get; set; } // JSON
    
    public string? NewValues { get; set; } // JSON
    
    public string? ChangedProperties { get; set; } // JSON array
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(200)]
    public string? RequestPath { get; set; }
    
    [MaxLength(10)]
    public string? HttpMethod { get; set; }
    
    public string? AdditionalData { get; set; } // JSON
}

public static class AuditActions
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string SoftDelete = "SoftDelete";
    public const string Restore = "Restore";
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string LoginFailed = "LoginFailed";
    public const string PasswordChanged = "PasswordChanged";
    public const string PasswordReset = "PasswordReset";
    public const string Export = "Export";
    public const string Import = "Import";
    public const string View = "View";
    public const string Assign = "Assign";
    public const string Convert = "Convert";
    public const string StatusChange = "StatusChange";
}
