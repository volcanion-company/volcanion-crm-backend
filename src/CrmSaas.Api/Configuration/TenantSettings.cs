namespace CrmSaas.Api.Configuration;

public class TenantSettings
{
    public const string SectionName = "TenantSettings";
    
    public string DefaultTenantId { get; set; } = "default";
    public TenantResolutionStrategy ResolutionStrategy { get; set; } = TenantResolutionStrategy.Header;
    public string HeaderName { get; set; } = "X-Tenant-Id";
    public bool AllowSubdomainResolution { get; set; } = true;
    public TenantIsolationStrategy IsolationStrategy { get; set; } = TenantIsolationStrategy.SharedDatabase;
}

public enum TenantResolutionStrategy
{
    Header,
    Subdomain,
    Token,
    QueryString
}

public enum TenantIsolationStrategy
{
    SharedDatabase,
    SchemaPerTenant,
    DatabasePerTenant
}
