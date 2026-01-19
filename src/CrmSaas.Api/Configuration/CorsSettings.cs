namespace CrmSaas.Api.Configuration;

public class CorsSettings
{
    public const string SectionName = "CorsSettings";
    
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = [];
    public string[] AllowedHeaders { get; set; } = [];
    public bool AllowCredentials { get; set; } = true;
}
