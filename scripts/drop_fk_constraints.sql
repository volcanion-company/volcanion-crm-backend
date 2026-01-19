-- Script để xóa các FK constraints cross-database không hợp lệ
USE CrmSaas_Master_Dev;
GO

-- Drop FK từ Roles table
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Roles_Tenant_TenantId')
BEGIN
    ALTER TABLE [dbo].[Roles] DROP CONSTRAINT [FK_Roles_Tenant_TenantId];
    PRINT 'Dropped FK_Roles_Tenant_TenantId';
END
ELSE
BEGIN
    PRINT 'FK_Roles_Tenant_TenantId does not exist';
END
GO

-- Drop FK từ Users table  
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Users_Tenant_TenantId')
BEGIN
    ALTER TABLE [dbo].[Users] DROP CONSTRAINT [FK_Users_Tenant_TenantId];
    PRINT 'Dropped FK_Users_Tenant_TenantId';
END
ELSE
BEGIN
    PRINT 'FK_Users_Tenant_TenantId does not exist';
END
GO

-- Drop FK từ RolePermissions table
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_RolePermissions_Permission_PermissionId')
BEGIN
    ALTER TABLE [dbo].[RolePermissions] DROP CONSTRAINT [FK_RolePermissions_Permission_PermissionId];
    PRINT 'Dropped FK_RolePermissions_Permission_PermissionId';
END
ELSE
BEGIN
    PRINT 'FK_RolePermissions_Permission_PermissionId does not exist';
END
GO

PRINT 'All cross-database FK constraints have been dropped successfully';
