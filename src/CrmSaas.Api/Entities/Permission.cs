using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Permission : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Module { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    // Navigation
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    
    // Navigation
    public virtual Role? Role { get; set; }
    public virtual Permission? Permission { get; set; }
}

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    
    // Navigation
    public virtual User? User { get; set; }
    public virtual Role? Role { get; set; }
}
