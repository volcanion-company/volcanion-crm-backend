using CrmSaas.Api.Entities;

namespace CrmSaas.Api.MultiTenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }
    Tenant? CurrentTenant { get; }
    void SetTenant(Tenant tenant);
    void SetTenantId(Guid tenantId);
}

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public Tenant? CurrentTenant { get; private set; }
    
    public void SetTenant(Tenant tenant)
    {
        CurrentTenant = tenant;
        TenantId = tenant.Id;
    }
    
    public void SetTenantId(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
