# Phase 9: Fix DB Architecture - Report

**Date**: 2026-01-17  
**Status**: ⚠️ PARTIALLY COMPLETED

## Objective
Tách kiến trúc database để MasterDbContext và TenantDbContext không conflict khi tạo migrations.

## Root Cause Analysis

### Vấn đề ban đầu
- **MasterDbContext** và **TenantDbContext** đều định nghĩa `Permission` entity
- EF Core tạo cùng table name `Permissions` trong cùng database
- Khi chạy migration gặp lỗi: table already exists

### Giải pháp đã thực hiện

#### 1. ✅ Tách schema Master vs Tenant
```csharp
// MasterDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasDefaultSchema("master");
    // Tables: master.Tenants, master.Permissions, master.AuditLogs
}
```

**Kết quả**:
- ✅ Master tables nằm trong schema `master`
- ✅ Tenant tables nằm trong schema `dbo` (default)
- ✅ Không còn conflict table names

#### 2. ✅ Loại bỏ Permission khỏi TenantDbContext
```csharp
// TenantDbContext.cs
// public DbSet<Permission> Permissions => Set<Permission>(); // ❌ REMOVED

// ConfigureIdentityEntities - removed Permission configuration
// RolePermission vẫn reference Permission qua FK
```

**Kết quả**:
- ✅ Permission chỉ có trong MasterDbContext
- ✅ TenantDbContext reference qua FK (master.Permissions)

#### 3. ✅ Ignore navigation properties trong MasterDbContext
```csharp
// MasterDbContext.cs - Tenant entity configuration
modelBuilder.Entity<Tenant>(entity =>
{
    // Ignore navigation properties (Users, Roles are in TenantDbContext)
    entity.Ignore(e => e.Users);
    entity.Ignore(e => e.Roles);
});
```

**Kết quả**:
- ✅ MasterDbContext KHÔNG tạo User, Role tables nữa
- ✅ Chỉ có Tenants, Permissions, AuditLogs trong schema `master`

## Current Status

### ✅ Thành công
1. **MasterDbContext migration tạo thành công** (`20260117034732_InitialCreate`)
   - Schema `master`
   - Tables: `Tenants`, `Permissions`, `AuditLogs`
   - Database update thành công

2. **TenantDbContext migration tạo thành công** (`20260117034732_InitialCreate`)
   - Schema `dbo` (default)
   - Tables: Users, Roles, Customers, Leads, Opportunities, Orders, Contracts, Tickets, Campaigns, Activities, v.v.

### ⚠️ Vấn đề còn lại

#### Cascade Delete Conflicts trong TenantDbContext
**Lỗi**: `Introducing FOREIGN KEY constraint 'FK_Activities_Activities_ParentActivityId' on table 'Activities' may cause cycles or multiple cascade paths.`

**Nguyên nhân**:
Activity entity có nhiều FKs:
```csharp
- ParentActivityId -> Activities (self-reference) - SetNull ✅
- AssignedToUserId -> Users (Cascade) 
- CustomerId -> Customers (Cascade)
- ContactId -> Contacts (?)
- TicketId -> Tickets (?)
...
```

Khi User/Customer có cascade delete → Activities cũng bị delete → conflict với Activity self-reference.

**Giải pháp cần thực hiện**:
```csharp
// Trong ConfigureActivityEntities
entity.HasOne(e => e.AssignedToUser)
    .WithMany()
    .HasForeignKey(e => e.AssignedToUserId)
    .OnDelete(DeleteBehavior.NoAction); // Hoặc SetNull

entity.HasOne(e => e.Customer)
    .WithMany()
    .HasForeignKey(e => e.CustomerId)
    .OnDelete(DeleteBehavior.NoAction); // Hoặc SetNull
```

## Architecture Diagram

```
Database: CrmSaas_Master_Dev
├── Schema: master (MasterDbContext)
│   ├── Tenants
│   ├── Permissions (global)
│   └── AuditLogs (all tenants)
│
└── Schema: dbo (TenantDbContext)
    ├── Users
    ├── Roles
    ├── UserRoles
    ├── RolePermissions → FK to master.Permissions
    ├── RefreshTokens
    ├── Customers
    ├── Leads
    ├── Opportunities
    ├── Orders, Quotations, Contracts
    ├── Tickets, TicketComments
    ├── Campaigns, CampaignMembers
    ├── Activities, Reminders
    └── ... (all tenant-specific data)
```

## Next Steps (Phase 9 chưa hoàn thành)

1. **Fix cascade delete conflicts** - HIGH PRIORITY
   - [ ] Review tất cả relationships trong Activity
   - [ ] Set `DeleteBehavior.NoAction` hoặc `SetNull` cho FKs không critical
   - [ ] Test migration lại

2. **Update seed data logic** - MEDIUM PRIORITY
   - [ ] Seed Permissions vào master schema
   - [ ] Seed Tenant vào master schema
   - [ ] Seed Users/Roles vào dbo schema (tenant-specific)

3. **Test database initialization**
   - [ ] Chạy lại app từ database rỗng
   - [ ] Verify data seeding hoạt động
   - [ ] Test tenant isolation

## Files Changed

1. `Data/MasterDbContext.cs` - Added schema `master`, ignored Tenant navigation
2. `Data/TenantDbContext.cs` - Removed Permission DbSet and configuration
3. `Data/Migrations/*` - Recreated all migrations
4. `TODO.md` - Updated with Phase 9-20

## Lessons Learned

1. **Schema separation is critical** for multi-context EF Core projects
2. **Navigation properties cause auto-discovery** - must explicitly ignore them
3. **Cascade delete is tricky** with complex relationships - prefer NoAction/SetNull
4. **Always drop and recreate DB** when making major schema changes

## Estimated Time
- **Planned**: 2 hours
- **Actual**: 3 hours (ongoing)
- **Remaining**: 1 hour to fix cascade conflicts

---

**Action Required**: Tiếp tục fix cascade delete conflicts trước khi sang Phase tiếp theo.
