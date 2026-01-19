namespace CrmSaas.Api.Configuration;

public class RateLimitSettings
{
    public const string SectionName = "RateLimitSettings";
    
    public bool EnableRateLimiting { get; set; } = true;
    public int PermitLimit { get; set; } = 100;
    public int WindowInSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 2;
}
