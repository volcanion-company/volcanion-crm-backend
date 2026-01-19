using CrmSaas.Api.Configuration;
using Microsoft.Extensions.Options;

namespace CrmSaas.Api.MultiTenancy;

public interface IConnectionStringResolver
{
    string GetConnectionString(Guid? tenantId);
    string GetMasterConnectionString();
}

public class ConnectionStringResolver : IConnectionStringResolver
{
    private readonly IConfiguration _configuration;
    private readonly TenantSettings _tenantSettings;

    public ConnectionStringResolver(
        IConfiguration configuration,
        IOptions<TenantSettings> tenantSettings)
    {
        _configuration = configuration;
        _tenantSettings = tenantSettings.Value;
    }

    public string GetConnectionString(Guid? tenantId)
    {
        if (tenantId == null)
        {
            return GetMasterConnectionString();
        }

        return _tenantSettings.IsolationStrategy switch
        {
            TenantIsolationStrategy.DatabasePerTenant => GetDatabasePerTenantConnectionString(tenantId.Value),
            TenantIsolationStrategy.SchemaPerTenant => GetMasterConnectionString(), // Same DB, different schema
            TenantIsolationStrategy.SharedDatabase => GetMasterConnectionString(),  // Same DB, TenantId column
            _ => GetMasterConnectionString()
        };
    }

    public string GetMasterConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection string not configured");
    }

    private string GetDatabasePerTenantConnectionString(Guid tenantId)
    {
        var template = _configuration.GetConnectionString("TenantTemplate");
        
        if (string.IsNullOrEmpty(template))
        {
            return GetMasterConnectionString();
        }

        return template.Replace("{TenantId}", tenantId.ToString("N"));
    }
}
