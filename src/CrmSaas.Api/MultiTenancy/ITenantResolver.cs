using CrmSaas.Api.Entities;

namespace CrmSaas.Api.MultiTenancy;

public interface ITenantResolver
{
    Task<Tenant?> ResolveAsync(HttpContext context);
}
