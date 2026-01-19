using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Role : TenantAuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsSystemRole { get; set; } = false;
    
    public DataScope DataScope { get; set; } = DataScope.Own;
    
    // Navigation
    public virtual Tenant? Tenant { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public enum DataScope
{
    Own = 0,        // Only own records
    Team = 1,       // Team records
    Department = 2, // Department records
    All = 3         // All tenant records
}
