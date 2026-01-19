using CrmSaas.Api.Entities;
using CrmSaas.Api.MultiTenancy;
using CrmSaas.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CrmSaas.Api.Data;

public class TenantDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly IDataScopeService? _dataScopeService;
    private readonly IServiceProvider? _serviceProvider;

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        ITenantContext tenantContext,
        IDataScopeService? dataScopeService = null,
        IServiceProvider? serviceProvider = null) : base(options)
    {
        _tenantContext = tenantContext;
        _dataScopeService = dataScopeService;
        _serviceProvider = serviceProvider;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // Suppress PendingModelChangesWarning for cross-database entity relationships
        // Permission and Tenant entities are intentionally ignored as they belong to MasterDbContext
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    // Identity & Access
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    // Note: Permission is in MasterDbContext (master schema), not here
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // CRM Core
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Interaction> Interactions => Set<Interaction>();
    
    // Lead Management
    public DbSet<Lead> Leads => Set<Lead>();
    
    // Sales Pipeline
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    
    // Order & Contract
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Contract> Contracts => Set<Contract>();
    
    // Support
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<Sla> Slas => Set<Sla>();
    
    // Marketing
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignMember> CampaignMembers => Set<CampaignMember>();
    public DbSet<CommunicationTemplate> CommunicationTemplates => Set<CommunicationTemplate>();
    public DbSet<Segment> Segments => Set<Segment>();
    
    // Webhooks
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    
    // Activity & Tasks
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    
    // Calendar Sync
    public DbSet<CalendarSyncConfiguration> CalendarSyncConfigurations => Set<CalendarSyncConfiguration>();
    public DbSet<CalendarEventMapping> CalendarEventMappings => Set<CalendarEventMapping>();
    public DbSet<ActivityReminder> ActivityReminders => Set<ActivityReminder>();
    
    // Workflow Engine
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowRule> WorkflowRules => Set<WorkflowRule>();
    public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();
    public DbSet<WorkflowExecutionLog> WorkflowExecutionLogs => Set<WorkflowExecutionLog>();
    
    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore entities from MasterDbContext to avoid cross-database FK constraints
        modelBuilder.Ignore<Tenant>();
        modelBuilder.Ignore<Permission>();
        modelBuilder.Ignore<AuditLog>();

        ConfigureIdentityEntities(modelBuilder);
        ConfigureCustomerEntities(modelBuilder);
        ConfigureLeadEntities(modelBuilder);
        ConfigureSalesEntities(modelBuilder);
        ConfigureOrderEntities(modelBuilder);
        ConfigureSupportEntities(modelBuilder);
        ConfigureMarketingEntities(modelBuilder);
        ConfigureActivityEntities(modelBuilder);
        ConfigureWorkflowEntities(modelBuilder);
        ConfigureNotificationEntities(modelBuilder);
        
        ApplyTenantFilters(modelBuilder);
    }

    private void ConfigureIdentityEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            // Ignore navigation to Tenant (cross-database relationship)
            entity.Ignore(e => e.Tenant);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            // Ignore navigation to Tenant (cross-database relationship)
            entity.Ignore(e => e.Tenant);
        });

        // Permission is configured in MasterDbContext (master schema)
        // RolePermission references it via FK only

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Permission is in MasterDbContext - no FK constraint (cross-database)
            // Only store PermissionId as a property, no navigation
            entity.Ignore(e => e.Permission);
            entity.Property(e => e.PermissionId).IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureCustomerEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Email });
            entity.HasIndex(e => new { e.TenantId, e.CustomerCode }).IsUnique().HasFilter("[CustomerCode] IS NOT NULL");
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.Property(e => e.LifetimeValue).HasPrecision(18, 2);
            entity.Property(e => e.AnnualRevenue).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("Contacts");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Contacts)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Interaction>(entity =>
        {
            entity.ToTable("Interactions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.InteractionDate);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    private void ConfigureLeadEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("Leads");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Email });
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.Property(e => e.EstimatedValue).HasPrecision(18, 2);
            
            // Lead -> Opportunity (when lead is converted to opportunity)
            entity.HasOne(e => e.ConvertedToOpportunity)
                .WithOne()
                .HasForeignKey<Lead>(e => e.ConvertedToOpportunityId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Lead -> Customer (when lead is converted to customer)
            entity.HasOne(e => e.ConvertedToCustomer)
                .WithMany()
                .HasForeignKey(e => e.ConvertedToCustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureSalesEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pipeline>(entity =>
        {
            entity.ToTable("Pipelines");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<PipelineStage>(entity =>
        {
            entity.ToTable("PipelineStages");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PipelineId);
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.HasOne(e => e.Pipeline)
                .WithMany(p => p.Stages)
                .HasForeignKey(e => e.PipelineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Opportunity>(entity =>
        {
            entity.ToTable("Opportunities");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.StageId);
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Pipeline)
                .WithMany(p => p.Opportunities)
                .HasForeignKey(e => e.PipelineId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Stage)
                .WithMany(s => s.Opportunities)
                .HasForeignKey(e => e.StageId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Opportunity sourced from Lead
            entity.HasOne(e => e.SourceLead)
                .WithMany()
                .HasForeignKey(e => e.SourceLeadId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Opportunity sourced from Campaign
            entity.HasOne(e => e.SourceCampaign)
                .WithMany(c => c.Opportunities)
                .HasForeignKey(e => e.SourceCampaignId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Opportunity -> Customer
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Opportunities)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureOrderEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Sku }).IsUnique().HasFilter("[Sku] IS NOT NULL");
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.CostPrice).HasPrecision(18, 2);
            entity.Property(e => e.TaxRate).HasPrecision(5, 2);
        });

        modelBuilder.Entity<Quotation>(entity =>
        {
            entity.ToTable("Quotations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.QuotationNumber }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.DiscountPercent).HasPrecision(5, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 6);
            
            // Quotation -> Order (when quotation is converted to order)
            entity.HasOne(e => e.ConvertedToOrder)
                .WithOne()
                .HasForeignKey<Quotation>(e => e.ConvertedToOrderId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Quotation -> Customer
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Quotations)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Quotation -> Opportunity
            entity.HasOne(e => e.Opportunity)
                .WithMany(o => o.Quotations)
                .HasForeignKey(e => e.OpportunityId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QuotationItem>(entity =>
        {
            entity.ToTable("QuotationItems");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.QuotationId);
            
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.DiscountPercent).HasPrecision(5, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.TaxPercent).HasPrecision(5, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Quotation)
                .WithMany(q => q.Items)
                .HasForeignKey(e => e.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.OrderNumber }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.DiscountPercent).HasPrecision(5, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.ShippingAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 6);
            
            // Order sourced from Quotation
            entity.HasOne(e => e.Quotation)
                .WithMany()
                .HasForeignKey(e => e.QuotationId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Order -> Customer
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.DiscountPercent).HasPrecision(5, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.TaxPercent).HasPrecision(5, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.ToTable("Contracts");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.ContractNumber }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.Property(e => e.Value).HasPrecision(18, 2);
        });
    }

    private void ConfigureSupportEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.TicketNumber }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<TicketComment>(entity =>
        {
            entity.ToTable("TicketComments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TicketId);
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.HasOne(e => e.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sla>(entity =>
        {
            entity.ToTable("Slas");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    private void ConfigureMarketingEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.ToTable("Campaigns");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.Property(e => e.Budget).HasPrecision(18, 2);
            entity.Property(e => e.ActualCost).HasPrecision(18, 2);
            entity.Property(e => e.ExpectedRevenue).HasPrecision(18, 2);
            entity.Property(e => e.ActualRevenue).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CampaignMember>(entity =>
        {
            entity.ToTable("CampaignMembers");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CampaignId);
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.HasOne(e => e.Campaign)
                .WithMany(c => c.Members)
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CommunicationTemplate>(entity =>
        {
            entity.ToTable("CommunicationTemplates");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    private void ConfigureActivityEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.ToTable("Activities");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.AssignedToUserId);
            entity.HasIndex(e => e.DueDate);
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            // Self-reference - use NoAction to avoid cascade conflict
            entity.HasOne(e => e.ParentActivity)
                .WithMany()
                .HasForeignKey(e => e.ParentActivityId)
                .OnDelete(DeleteBehavior.NoAction);
            
            // Related entities - use NoAction to avoid cascade cycles
            entity.HasOne(e => e.AssignedToUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedToUserId)
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasOne(e => e.Contact)
                .WithMany()
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasOne(e => e.Lead)
                .WithMany(l => l.Activities)
                .HasForeignKey(e => e.LeadId)
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasOne(e => e.Opportunity)
                .WithMany(o => o.Activities)
                .HasForeignKey(e => e.OpportunityId)
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasOne(e => e.Ticket)
                .WithMany()
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.ToTable("Reminders");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.ReminderDate);
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.HasOne(e => e.Activity)
                .WithMany(a => a.Reminders)
                .HasForeignKey(e => e.ActivityId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    
    private void ConfigureWorkflowEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.ToTable("Workflows");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.EntityType });
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.HasMany(e => e.Rules)
                .WithOne(r => r.Workflow)
                .HasForeignKey(r => r.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<WorkflowRule>(entity =>
        {
            entity.ToTable("WorkflowRules");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WorkflowId);
            
            entity.HasMany(e => e.Actions)
                .WithOne(a => a.WorkflowRule)
                .HasForeignKey(a => a.WorkflowRuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<WorkflowAction>(entity =>
        {
            entity.ToTable("WorkflowActions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WorkflowRuleId);
        });
        
        modelBuilder.Entity<WorkflowExecutionLog>(entity =>
        {
            entity.ToTable("WorkflowExecutionLogs");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.WorkflowId, e.ExecutedAt });
            entity.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId });
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }
    
    private void ConfigureNotificationEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.UserId, e.IsRead });
            entity.HasIndex(e => new { e.TenantId, e.UserId, e.CreatedAt });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.ToTable("NotificationTemplates");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
        
        modelBuilder.Entity<UserNotificationPreference>(entity =>
        {
            entity.ToTable("UserNotificationPreferences");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.NotificationType }).IsUnique();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        // Apply tenant filter to all tenant entities
        if (_tenantContext?.TenantId != null)
        {
            var tenantId = _tenantContext.TenantId.Value;
            
            // Apply combined filters: Tenant + Soft Delete + Data Scope
            ApplyFilter<User>(modelBuilder, tenantId);
            ApplyFilter<Role>(modelBuilder, tenantId);
            ApplyFilter<Customer>(modelBuilder, tenantId);
            ApplyFilter<Contact>(modelBuilder, tenantId);
            ApplyFilter<Interaction>(modelBuilder, tenantId);
            ApplyFilter<Lead>(modelBuilder, tenantId);
            ApplyFilter<Pipeline>(modelBuilder, tenantId);
            ApplyFilter<PipelineStage>(modelBuilder, tenantId);
            ApplyFilter<Opportunity>(modelBuilder, tenantId);
            ApplyFilter<Product>(modelBuilder, tenantId);
            ApplyFilter<Quotation>(modelBuilder, tenantId);
            ApplyFilter<Order>(modelBuilder, tenantId);
            ApplyFilter<Contract>(modelBuilder, tenantId);
            ApplyFilter<Ticket>(modelBuilder, tenantId);
            ApplyFilter<TicketComment>(modelBuilder, tenantId);
            ApplyFilter<Sla>(modelBuilder, tenantId);
            ApplyFilter<Campaign>(modelBuilder, tenantId);
            ApplyFilter<CampaignMember>(modelBuilder, tenantId);
            ApplyFilter<CommunicationTemplate>(modelBuilder, tenantId);
            ApplyFilter<Activity>(modelBuilder, tenantId);
            ApplyFilter<Reminder>(modelBuilder, tenantId);
        }
    }

    private void ApplyFilter<T>(ModelBuilder modelBuilder, Guid tenantId) where T : TenantAuditableEntity
    {
        var dataScopeFilter = _dataScopeService?.GetDataScopeFilter<T>();
        
        if (dataScopeFilter != null)
        {
            // Combine tenant filter + soft delete + data scope
            modelBuilder.Entity<T>().HasQueryFilter(e => 
                e.TenantId == tenantId && 
                !e.IsDeleted && 
                dataScopeFilter.Compile()(e));
        }
        else
        {
            // Only tenant filter + soft delete (DataScope = All or not authenticated)
            modelBuilder.Entity<T>().HasQueryFilter(e => 
                e.TenantId == tenantId && 
                !e.IsDeleted);
        }
    }

    public override int SaveChanges()
    {
        SetTenantId();
        SetAuditFields();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantId();
        SetAuditFields();
        
        // Track changes for workflow processing BEFORE saving
        var trackedChanges = await TrackWorkflowChangesAsync(cancellationToken);
        
        // Save changes to database
        var result = await base.SaveChangesAsync(cancellationToken);
        
        // Process workflows AFTER saving (so entity has Id, etc.)
        // Resolve IWorkflowEngine from service provider to avoid circular dependency
        var workflowEngine = _serviceProvider?.GetService<IWorkflowEngine>();
        if (workflowEngine != null && trackedChanges.Any())
        {
            await ProcessWorkflowsAsync(trackedChanges, workflowEngine, cancellationToken);
        }
        
        return result;
    }

    private async Task<List<WorkflowChange>> TrackWorkflowChangesAsync(CancellationToken cancellationToken)
    {
        var changes = new List<WorkflowChange>();

        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || 
                       e.State == EntityState.Modified || 
                       e.State == EntityState.Deleted)
            .Where(e => e.Entity is TenantAuditableEntity)
            .ToList();

        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            var entityType = entity.GetType();
            
            // Skip workflow entities themselves to avoid infinite loops
            if (entityType.Name.StartsWith("Workflow") || 
                entityType.Name == "Notification" ||
                entityType.Name == "AuditLog")
            {
                continue;
            }

            var triggerType = entry.State switch
            {
                EntityState.Added => Entities.WorkflowTriggerType.OnCreate,
                EntityState.Modified => Entities.WorkflowTriggerType.OnUpdate,
                EntityState.Deleted => Entities.WorkflowTriggerType.OnDelete,
                _ => (Entities.WorkflowTriggerType?)null
            };

            if (triggerType.HasValue)
            {
                // Capture old values for OnUpdate
                object? oldEntity = null;
                if (entry.State == EntityState.Modified)
                {
                    oldEntity = entry.OriginalValues.ToObject();
                }

                changes.Add(new WorkflowChange
                {
                    Entity = entity,
                    OldEntity = oldEntity,
                    TriggerType = triggerType.Value
                });
            }
        }

        return changes;
    }

    private async Task ProcessWorkflowsAsync(List<WorkflowChange> changes, IWorkflowEngine workflowEngine, CancellationToken cancellationToken)
    {
        // Skip workflow processing if the context is being disposed or cancellation is requested
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            foreach (var change in changes)
            {
                // Process workflows synchronously to avoid disposed context issues
                // The WorkflowEngine handles its own error logging internally
                try
                {
                    await workflowEngine.ProcessWorkflowsAsync(
                        change.Entity, 
                        change.TriggerType, 
                        change.OldEntity, 
                        cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    // Context was disposed, skip remaining workflows
                    // This can happen during application shutdown or scope disposal
                    break;
                }
                catch (Exception ex)
                {
                    // Log but don't throw - workflows should not break main transaction
                    Console.WriteLine($"Workflow processing error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            // Log workflow processing errors but don't fail the save operation
            Console.WriteLine($"Error triggering workflows: {ex.Message}");
        }
    }

    private void SetTenantId()
    {
        if (_tenantContext?.TenantId == null) return;

        var entries = ChangeTracker.Entries<ITenantEntity>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            if (entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = _tenantContext.TenantId.Value;
            }
        }
    }

    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        var softDeleteEntries = ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (var entry in softDeleteEntries)
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = DateTime.UtcNow;
        }
    }
    
    private class WorkflowChange
    {
        public object Entity { get; set; } = null!;
        public object? OldEntity { get; set; }
        public Entities.WorkflowTriggerType TriggerType { get; set; }
    }
}
