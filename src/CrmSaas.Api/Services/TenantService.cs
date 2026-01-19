using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Services;

public interface ITenantService
{
    Task<Tenant?> GetByIdAsync(Guid id);
    Task<Tenant?> GetByIdentifierAsync(string identifier);
    Task<IEnumerable<Tenant>> GetAllAsync();
    Task<Tenant> CreateAsync(CreateTenantRequest request);
    Task<Tenant> UpdateAsync(Guid id, UpdateTenantRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string identifier);
}

public class TenantService : ITenantService
{
    private readonly MasterDbContext _db;
    private readonly IAuditService _auditService;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        MasterDbContext db,
        IAuditService auditService,
        ILogger<TenantService> logger)
    {
        _db = db;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id)
    {
        return await _db.Tenants.FindAsync(id);
    }

    public async Task<Tenant?> GetByIdentifierAsync(string identifier)
    {
        return await _db.Tenants.FirstOrDefaultAsync(t => t.Identifier == identifier);
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync()
    {
        return await _db.Tenants.ToListAsync();
    }

    public async Task<Tenant> CreateAsync(CreateTenantRequest request)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Identifier = request.Identifier.ToLowerInvariant(),
            Subdomain = request.Subdomain?.ToLowerInvariant(),
            Status = TenantStatus.Active,
            Plan = request.Plan,
            MaxUsers = GetMaxUsersForPlan(request.Plan),
            MaxStorageBytes = GetMaxStorageForPlan(request.Plan),
            TimeZone = request.TimeZone ?? "UTC",
            Culture = request.Culture ?? "en-US",
            CreatedAt = DateTime.UtcNow
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Create, nameof(Tenant), tenant.Id, tenant.Name, newValues: tenant);

        _logger.LogInformation("Created tenant {TenantId} - {TenantName}", tenant.Id, tenant.Name);

        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Guid id, UpdateTenantRequest request)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with id {id} not found");
        }

        var oldValues = new { tenant.Name, tenant.Status, tenant.Plan };

        tenant.Name = request.Name ?? tenant.Name;
        tenant.Status = request.Status ?? tenant.Status;
        tenant.Plan = request.Plan ?? tenant.Plan;
        tenant.MaxUsers = request.MaxUsers ?? tenant.MaxUsers;
        tenant.TimeZone = request.TimeZone ?? tenant.TimeZone;
        tenant.Culture = request.Culture ?? tenant.Culture;
        tenant.LogoUrl = request.LogoUrl ?? tenant.LogoUrl;
        tenant.PrimaryColor = request.PrimaryColor ?? tenant.PrimaryColor;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Tenant), tenant.Id, tenant.Name, 
            oldValues: oldValues, newValues: new { tenant.Name, tenant.Status, tenant.Plan });

        return tenant;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        
        if (tenant == null)
        {
            return false;
        }

        tenant.IsDeleted = true;
        tenant.DeletedAt = DateTime.UtcNow;
        tenant.Status = TenantStatus.Cancelled;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.SoftDelete, nameof(Tenant), tenant.Id, tenant.Name);

        return true;
    }

    public async Task<bool> ExistsAsync(string identifier)
    {
        return await _db.Tenants.AnyAsync(t => t.Identifier == identifier.ToLowerInvariant());
    }

    private static int GetMaxUsersForPlan(TenantPlan plan) => plan switch
    {
        TenantPlan.Free => 5,
        TenantPlan.Starter => 25,
        TenantPlan.Professional => 100,
        TenantPlan.Enterprise => 1000,
        _ => 5
    };

    private static long GetMaxStorageForPlan(TenantPlan plan) => plan switch
    {
        TenantPlan.Free => 1073741824L,        // 1GB
        TenantPlan.Starter => 5368709120L,     // 5GB
        TenantPlan.Professional => 21474836480L, // 20GB
        TenantPlan.Enterprise => 107374182400L,  // 100GB
        _ => 1073741824L
    };
}

public class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public TenantPlan Plan { get; set; } = TenantPlan.Free;
    public string? TimeZone { get; set; }
    public string? Culture { get; set; }
}

public class UpdateTenantRequest
{
    public string? Name { get; set; }
    public TenantStatus? Status { get; set; }
    public TenantPlan? Plan { get; set; }
    public int? MaxUsers { get; set; }
    public string? TimeZone { get; set; }
    public string? Culture { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
}
