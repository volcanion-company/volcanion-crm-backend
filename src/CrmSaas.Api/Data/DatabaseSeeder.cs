using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CrmSaas.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(MasterDbContext masterDb, TenantDbContext tenantDb)
    {
        await SeedPermissionsAsync(masterDb);
        await SeedDefaultTenantAsync(masterDb);
        await SeedDefaultRolesAndUserAsync(tenantDb, masterDb);
    }

    private static async Task SeedPermissionsAsync(MasterDbContext db)
    {
        if (await db.Permissions.AnyAsync()) return;

        var modules = new[] { "Customer", "Contact", "Lead", "Opportunity", "Order", "Contract", "Ticket", "Campaign", "Activity", "Report", "User", "Role", "Tenant", "Settings" };
        var actions = new[] { "View", "Create", "Update", "Delete", "Export", "Import" };

        var permissions = new List<Permission>();

        foreach (var module in modules)
        {
            foreach (var action in actions)
            {
                permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = $"{action} {module}",
                    Code = $"{module.ToLower()}.{action.ToLower()}",
                    Module = module,
                    Description = $"Permission to {action.ToLower()} {module.ToLower()} records"
                });
            }
        }

        // Add special permissions (avoid duplicates with generated permissions)
        permissions.AddRange(new[]
        {
            new Permission { Id = Guid.NewGuid(), Name = "Assign Lead", Code = "lead.assign", Module = "Lead", Description = "Permission to assign leads" },
            new Permission { Id = Guid.NewGuid(), Name = "Convert Lead", Code = "lead.convert", Module = "Lead", Description = "Permission to convert leads to customers" },
            new Permission { Id = Guid.NewGuid(), Name = "Assign Opportunity", Code = "opportunity.assign", Module = "Opportunity", Description = "Permission to assign opportunities" },
            new Permission { Id = Guid.NewGuid(), Name = "Assign Ticket", Code = "ticket.assign", Module = "Ticket", Description = "Permission to assign tickets" },
            new Permission { Id = Guid.NewGuid(), Name = "View Audit Logs", Code = "audit.view", Module = "Audit", Description = "Permission to view audit logs" },
            new Permission { Id = Guid.NewGuid(), Name = "Manage Tenant Settings", Code = "tenant.settings", Module = "Tenant", Description = "Permission to manage tenant settings" },
        });

        db.Permissions.AddRange(permissions);
        await db.SaveChangesAsync();
    }

    private static async Task SeedDefaultTenantAsync(MasterDbContext db)
    {
        if (await db.Tenants.AnyAsync()) return;

        var defaultTenant = new Tenant
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Default Tenant",
            Identifier = "default",
            Subdomain = "default",
            Status = TenantStatus.Active,
            Plan = TenantPlan.Enterprise,
            MaxUsers = 100,
            MaxStorageBytes = 10737418240, // 10GB
            TimeZone = "UTC",
            Culture = "en-US",
            CreatedAt = DateTime.UtcNow
        };

        db.Tenants.Add(defaultTenant);
        await db.SaveChangesAsync();
    }

    private static async Task SeedDefaultRolesAndUserAsync(TenantDbContext tenantDb, MasterDbContext masterDb)
    {
        var defaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // Check if roles already exist
        if (await tenantDb.Roles.IgnoreQueryFilters().AnyAsync(r => r.TenantId == defaultTenantId)) return;

        var allPermissions = await masterDb.Permissions.ToListAsync();

        // Create Admin Role
        var adminRole = new Role
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000010"),
            TenantId = defaultTenantId,
            Name = "Administrator",
            Description = "Full system access",
            IsSystemRole = true,
            DataScope = DataScope.All,
            CreatedAt = DateTime.UtcNow
        };

        // Create Sales Manager Role
        var salesManagerRole = new Role
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000011"),
            TenantId = defaultTenantId,
            Name = "Sales Manager",
            Description = "Sales team management access",
            IsSystemRole = true,
            DataScope = DataScope.Team,
            CreatedAt = DateTime.UtcNow
        };

        // Create Sales Rep Role
        var salesRepRole = new Role
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000012"),
            TenantId = defaultTenantId,
            Name = "Sales Representative",
            Description = "Individual sales access",
            IsSystemRole = true,
            DataScope = DataScope.Own,
            CreatedAt = DateTime.UtcNow
        };

        // Create Support Role
        var supportRole = new Role
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000013"),
            TenantId = defaultTenantId,
            Name = "Support Agent",
            Description = "Customer support access",
            IsSystemRole = true,
            DataScope = DataScope.All,
            CreatedAt = DateTime.UtcNow
        };

        tenantDb.Roles.AddRange(adminRole, salesManagerRole, salesRepRole, supportRole);

        // Assign all permissions to admin
        foreach (var permission in allPermissions)
        {
            tenantDb.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = adminRole.Id,
                PermissionId = permission.Id
            });
        }

        // Assign sales permissions to sales manager
        var salesModules = new[] { "Customer", "Contact", "Lead", "Opportunity", "Order", "Contract", "Activity", "Report" };
        foreach (var permission in allPermissions.Where(p => salesModules.Contains(p.Module)))
        {
            tenantDb.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = salesManagerRole.Id,
                PermissionId = permission.Id
            });
        }

        // Assign basic sales permissions to sales rep
        var salesRepActions = new[] { "view", "create", "update" };
        foreach (var permission in allPermissions.Where(p => 
            salesModules.Contains(p.Module) && 
            salesRepActions.Any(a => p.Code.Contains(a))))
        {
            tenantDb.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = salesRepRole.Id,
                PermissionId = permission.Id
            });
        }

        // Assign support permissions
        var supportModules = new[] { "Customer", "Contact", "Ticket", "Activity" };
        foreach (var permission in allPermissions.Where(p => supportModules.Contains(p.Module)))
        {
            tenantDb.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = supportRole.Id,
                PermissionId = permission.Id
            });
        }

        // Create default admin user
        var adminUser = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000100"),
            TenantId = defaultTenantId,
            Email = "admin@volcanion.vn",
            PasswordHash = HashPassword("Admin@123"),
            FirstName = "System",
            LastName = "Administrator",
            Status = UserStatus.Active,
            EmailConfirmed = true,
            TimeZone = "Asia/Ho_Chi_Minh",
            Culture = "vi-VN",
            CreatedAt = DateTime.UtcNow
        };

        tenantDb.Users.Add(adminUser);

        tenantDb.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        // Create default pipeline
        var defaultPipeline = new Pipeline
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000001000"),
            TenantId = defaultTenantId,
            Name = "Default Sales Pipeline",
            Description = "Standard sales pipeline",
            IsDefault = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        tenantDb.Pipelines.Add(defaultPipeline);

        // Create pipeline stages
        var stages = new[]
        {
            new PipelineStage { Id = Guid.NewGuid(), TenantId = defaultTenantId, PipelineId = defaultPipeline.Id, Name = "Qualification", SortOrder = 1, Probability = 10, Color = "#6366f1", CreatedAt = DateTime.UtcNow },
            new PipelineStage { Id = Guid.NewGuid(), TenantId = defaultTenantId, PipelineId = defaultPipeline.Id, Name = "Needs Analysis", SortOrder = 2, Probability = 25, Color = "#8b5cf6", CreatedAt = DateTime.UtcNow },
            new PipelineStage { Id = Guid.NewGuid(), TenantId = defaultTenantId, PipelineId = defaultPipeline.Id, Name = "Proposal", SortOrder = 3, Probability = 50, Color = "#a855f7", CreatedAt = DateTime.UtcNow },
            new PipelineStage { Id = Guid.NewGuid(), TenantId = defaultTenantId, PipelineId = defaultPipeline.Id, Name = "Negotiation", SortOrder = 4, Probability = 75, Color = "#d946ef", CreatedAt = DateTime.UtcNow },
            new PipelineStage { Id = Guid.NewGuid(), TenantId = defaultTenantId, PipelineId = defaultPipeline.Id, Name = "Closed Won", SortOrder = 5, Probability = 100, IsWon = true, Color = "#22c55e", CreatedAt = DateTime.UtcNow },
            new PipelineStage { Id = Guid.NewGuid(), TenantId = defaultTenantId, PipelineId = defaultPipeline.Id, Name = "Closed Lost", SortOrder = 6, Probability = 0, IsLost = true, Color = "#ef4444", CreatedAt = DateTime.UtcNow }
        };

        tenantDb.PipelineStages.AddRange(stages);

        // Create default SLA
        var defaultSla = new Sla
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000002000"),
            TenantId = defaultTenantId,
            Name = "Standard SLA",
            Description = "Default service level agreement",
            IsActive = true,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };

        tenantDb.Slas.Add(defaultSla);

        await tenantDb.SaveChangesAsync();
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
