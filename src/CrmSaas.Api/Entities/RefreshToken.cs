using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;
    
    public DateTime ExpiresAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(50)]
    public string? CreatedByIp { get; set; }
    
    public DateTime? RevokedAt { get; set; }
    
    [MaxLength(50)]
    public string? RevokedByIp { get; set; }
    
    [MaxLength(500)]
    public string? ReplacedByToken { get; set; }
    
    [MaxLength(200)]
    public string? ReasonRevoked { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
    
    // Navigation
    public virtual User? User { get; set; }
}
