# CRM SaaS - Production Configuration Guide

## 📝 appsettings.Production.json Configuration

Create `appsettings.Production.json` in the API project with the following structure:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Hangfire": "Information"
    }
  },
  "AllowedHosts": "yourdomain.com,www.yourdomain.com",
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-sql-server.database.windows.net;Database=CrmSaas_Master;User Id=sqladmin;Password=YourSecurePassword123!;Encrypt=True;TrustServerCertificate=False;",
    "TenantTemplate": "Server=prod-sql-server.database.windows.net;Database=CrmSaas_{TenantId};User Id=sqladmin;Password=YourSecurePassword123!;Encrypt=True;TrustServerCertificate=False;"
  },
  "JwtSettings": {
    "Secret": "YourProductionSecretKey256BitMinimum32Characters!!!",
    "Issuer": "https://api.yourdomain.com",
    "Audience": "https://yourdomain.com",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "TenantSettings": {
    "ConnectionStringTemplate": "Server=prod-sql-server.database.windows.net;Database=CrmSaas_{TenantId};User Id=sqladmin;Password=YourSecurePassword123!;Encrypt=True;TrustServerCertificate=False;",
    "MaxTenantsPerServer": 1000
  },
  "CorsSettings": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://www.yourdomain.com",
      "https://app.yourdomain.com"
    ]
  },
  "RateLimitSettings": {
    "PermitLimit": 100,
    "Window": 60,
    "QueueLimit": 10
  },
  "EmailSettings": {
    "Enabled": true,
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "UseSsl": true,
    "Username": "apikey",
    "Password": "SG.your_sendgrid_api_key",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "CRM SaaS"
  },
  "SmsSettings": {
    "Enabled": true,
    "Provider": "Twilio",
    "AccountSid": "your_twilio_account_sid",
    "AuthToken": "your_twilio_auth_token",
    "FromNumber": "+1234567890"
  },
  "PushNotificationSettings": {
    "Enabled": true,
    "FirebaseServerKey": "your_firebase_server_key",
    "ApnsKeyId": "your_apns_key_id",
    "ApnsTeamId": "your_apns_team_id"
  },
  "CalendarSyncSettings": {
    "GoogleClientId": "your_google_client_id.apps.googleusercontent.com",
    "GoogleClientSecret": "your_google_client_secret",
    "MicrosoftClientId": "your_microsoft_client_id",
    "MicrosoftClientSecret": "your_microsoft_client_secret",
    "RedirectUri": "https://api.yourdomain.com/api/calendar/oauth/callback"
  }
}
```

## 🔐 Secure Configuration with Azure Key Vault

### Setup Azure Key Vault

1. Create Key Vault:
```bash
az keyvault create --name crmsaas-keyvault --resource-group crmsaas-rg --location eastus
```

2. Add secrets:
```bash
az keyvault secret set --vault-name crmsaas-keyvault --name "ConnectionStrings--DefaultConnection" --value "Server=..."
az keyvault secret set --vault-name crmsaas-keyvault --name "JwtSettings--Secret" --value "..."
az keyvault secret set --vault-name crmsaas-keyvault --name "EmailSettings--Password" --value "..."
```

3. Update Program.cs to use Key Vault:
```csharp
// Add before builder.Build()
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}
```

## 🌍 Environment-Specific Settings

### Development (appsettings.Development.json)
- Detailed logging (Debug level)
- Local SQL Server
- Swagger enabled
- CORS allows localhost
- Email disabled or uses test provider

### Staging (appsettings.Staging.json)
- Information level logging
- Staging database
- Swagger enabled (password protected)
- CORS limited to staging domains
- Email uses test account

### Production (appsettings.Production.json)
- Warning level logging
- Production database with failover
- Swagger disabled or heavily restricted
- CORS strict origin checking
- Email uses production provider (SendGrid/AWS SES)
- Rate limiting enforced

## 🚀 IIS Configuration

### web.config
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\CrmSaas.Api.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
      <httpErrors errorMode="Detailed" />
    </system.webServer>
  </location>
</configuration>
```

### Application Pool Settings
- .NET CLR Version: No Managed Code
- Managed Pipeline Mode: Integrated
- Identity: ApplicationPoolIdentity
- Enable 32-Bit Applications: False
- Idle Time-out: 20 minutes (or 0 for always running)

## 🐳 Docker Configuration

### Dockerfile
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CrmSaas.Api/CrmSaas.Api.csproj", "CrmSaas.Api/"]
RUN dotnet restore "CrmSaas.Api/CrmSaas.Api.csproj"
COPY src/ .
WORKDIR "/src/CrmSaas.Api"
RUN dotnet build "CrmSaas.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CrmSaas.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user
RUN useradd -m -s /bin/bash appuser && chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

EXPOSE 8080
ENTRYPOINT ["dotnet", "CrmSaas.Api.dll"]
```

### docker-compose.yml
```yaml
version: '3.8'

services:
  crmsaas-api:
    image: crmsaas-api:latest
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
      - JwtSettings__Secret=${JWT_SECRET}
    depends_on:
      - sqlserver
    restart: unless-stopped
    networks:
      - crmsaas-network

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    restart: unless-stopped
    networks:
      - crmsaas-network

volumes:
  sqldata:

networks:
  crmsaas-network:
    driver: bridge
```

## ☸️ Kubernetes Configuration

### deployment.yaml
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: crmsaas-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: crmsaas-api
  template:
    metadata:
      labels:
        app: crmsaas-api
    spec:
      containers:
      - name: api
        image: yourdockerhub/crmsaas-api:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: crmsaas-secrets
              key: db-connection-string
        - name: JwtSettings__Secret
          valueFrom:
            secretKeyRef:
              name: crmsaas-secrets
              key: jwt-secret
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
---
apiVersion: v1
kind: Service
metadata:
  name: crmsaas-api-service
spec:
  selector:
    app: crmsaas-api
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
  type: LoadBalancer
```

### secrets.yaml
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: crmsaas-secrets
type: Opaque
data:
  db-connection-string: <base64-encoded-connection-string>
  jwt-secret: <base64-encoded-jwt-secret>
```

## 🔥 Performance Tuning

### Database Connection Pooling
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=...;Min Pool Size=10;Max Pool Size=100;Connection Timeout=30;"
}
```

### Response Caching
Add to Program.cs:
```csharp
builder.Services.AddResponseCaching();
builder.Services.AddOutputCache();

// In middleware
app.UseResponseCaching();
app.UseOutputCache();
```

### Compression
```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
```

## 📊 Monitoring Configuration

### Application Insights (Azure)
```json
"ApplicationInsights": {
  "InstrumentationKey": "your-instrumentation-key",
  "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=..."
}
```

Program.cs:
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Prometheus Metrics
```bash
dotnet add package prometheus-net.AspNetCore
```

```csharp
app.UseHttpMetrics();
app.MapMetrics(); // Exposes /metrics endpoint
```

## 🔒 Security Hardening

### HTTPS Enforcement
```csharp
app.UseHttpsRedirection();
app.UseHsts(); // Production only
```

### Security Headers
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    await next();
});
```

### Rate Limiting (Production)
```json
"RateLimitSettings": {
  "PermitLimit": 100,
  "Window": 60,
  "QueueLimit": 10
}
```

## 🗄️ Database Backup Strategy

### Automated Backups
```sql
-- Full backup daily at 2 AM
BACKUP DATABASE CrmSaas_Master
TO DISK = 'D:\Backups\CrmSaas_Master_Full.bak'
WITH INIT, COMPRESSION;

-- Transaction log backup every hour
BACKUP LOG CrmSaas_Master
TO DISK = 'D:\Backups\CrmSaas_Master_Log.trn'
WITH COMPRESSION;
```

### Retention Policy
- Full backups: Keep 30 days
- Transaction logs: Keep 7 days
- Archive monthly backups: Keep 1 year

## 🔄 CI/CD Pipeline

### Azure DevOps
```yaml
# azure-pipelines.yml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

stages:
- stage: Build
  jobs:
  - job: BuildJob
    steps:
    - task: UseDotNet@2
      inputs:
        version: '10.x'
    - task: DotNetCoreCLI@2
      displayName: 'Restore'
      inputs:
        command: 'restore'
    - task: DotNetCoreCLI@2
      displayName: 'Build'
      inputs:
        command: 'build'
        arguments: '--configuration Release'
    - task: DotNetCoreCLI@2
      displayName: 'Publish'
      inputs:
        command: 'publish'
        publishWebProjects: true
        arguments: '--configuration Release --output $(Build.ArtifactStagingDirectory)'
    - task: PublishBuildArtifacts@1
      inputs:
        PathtoPublish: '$(Build.ArtifactStagingDirectory)'
        ArtifactName: 'drop'

- stage: Deploy
  dependsOn: Build
  jobs:
  - deployment: DeployProduction
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              azureSubscription: 'your-subscription'
              appName: 'crmsaas-api'
              package: '$(Pipeline.Workspace)/drop/**/*.zip'
```

## 📈 Scaling Recommendations

### Horizontal Scaling
- **Load Balancer**: Azure Load Balancer / AWS ELB / NGINX
- **API Instances**: 3+ instances for high availability
- **Database**: Read replicas for read-heavy operations
- **Hangfire**: Single server instance (uses distributed locks)

### Auto-scaling Rules
```yaml
# Azure App Service
minInstances: 2
maxInstances: 10
rules:
  - metricName: "CPU Percentage"
    operator: "GreaterThan"
    threshold: 70
    scaleAction: "Increase"
    instanceCount: 2
  - metricName: "CPU Percentage"
    operator: "LessThan"
    threshold: 30
    scaleAction: "Decrease"
    instanceCount: 1
```

---

**✅ Configuration Complete - Ready for Production Deployment**
