using CrmSaas.Api.Configuration;
using CrmSaas.Api.Data;
using CrmSaas.Api.Middleware;
using CrmSaas.Api.MultiTenancy;
using CrmSaas.Api.Services;
using CrmSaas.Api.Authorization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Scalar.AspNetCore;
using Hangfire;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// LOGGING CONFIGURATION (Serilog)
// ========================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/crm-saas-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ========================================
// CONFIGURATION BINDING
// ========================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
var tenantSettings = builder.Configuration.GetSection("TenantSettings").Get<TenantSettings>() ?? new TenantSettings();
var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>() ?? new CorsSettings();
var rateLimitSettings = builder.Configuration.GetSection("RateLimitSettings").Get<RateLimitSettings>() ?? new RateLimitSettings();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<TenantSettings>(builder.Configuration.GetSection("TenantSettings"));
builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection("CorsSettings"));
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimitSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// ========================================
// DATABASE CONFIGURATION
// ========================================
builder.Services.AddDbContext<MasterDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30);
        }));

builder.Services.AddDbContext<TenantDbContext>((serviceProvider, options) =>
{
    var tenantContext = serviceProvider.GetService<ITenantContext>();
    var connectionResolver = serviceProvider.GetRequiredService<IConnectionStringResolver>();
    
    var connectionString = tenantContext?.TenantId != null
        ? connectionResolver.GetConnectionString(tenantContext.TenantId.Value)
        : builder.Configuration.GetConnectionString("DefaultConnection");
    
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(30);
    });
});

// ========================================
// MULTI-TENANCY SERVICES
// ========================================
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<ITenantResolver, TenantResolver>();
builder.Services.AddSingleton<IConnectionStringResolver, ConnectionStringResolver>();

// ========================================
// APPLICATION SERVICES
// ========================================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IDataScopeService, DataScopeService>();

// Workflow Engine Services
builder.Services.AddScoped<IWorkflowConditionEvaluator, WorkflowConditionEvaluator>();
builder.Services.AddScoped<IWorkflowActionExecutor, WorkflowActionExecutor>();
builder.Services.AddScoped<IWorkflowEngine, WorkflowEngine>();

// Notification Services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Background Job Services
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddScoped<ScheduledJobsService>();

// SLA & Ticket Automation Services
builder.Services.AddScoped<ISlaAutomationService, SlaAutomationService>();
builder.Services.AddScoped<ITicketAutomationService, TicketAutomationService>();

// Duplicate Detection & Customer 360 Services
builder.Services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
builder.Services.AddScoped<ICustomer360Service, Customer360Service>();

// Marketing Automation Services
builder.Services.AddScoped<ISegmentationService, SegmentationService>();
builder.Services.AddScoped<IMessagingService, MessagingService>();

// Analytics Services
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Webhook Services
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IWebhookDeliveryService, WebhookDeliveryService>();
builder.Services.AddScoped<IWebhookPublisher, WebhookPublisher>();

// HttpClient for webhooks
builder.Services.AddHttpClient("WebhookClient");

// Calendar Sync Services
builder.Services.AddScoped<ICalendarSyncService, CalendarSyncService>();
builder.Services.AddScoped<IICalService, ICalService>();
builder.Services.AddScoped<IActivityReminderService, ActivityReminderService>();

// ========================================
// HEALTH CHECKS
// ========================================
builder.Services.AddHealthChecks()
    .AddCheck<CrmSaas.Api.HealthChecks.DatabaseHealthCheck>("database", tags: new[] { "ready", "db" })
    .AddCheck<CrmSaas.Api.HealthChecks.HangfireHealthCheck>("hangfire", tags: new[] { "ready", "jobs" });

// ========================================
// AUTHENTICATION (JWT Bearer)
// ========================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
            // Additional token validation if needed
            return Task.CompletedTask;
        }
    };
});

// ========================================
// AUTHORIZATION
// ========================================
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

// ========================================
// CORS CONFIGURATION
// ========================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsSettings.AllowedOrigins?.Contains("*") == true)
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(corsSettings.AllowedOrigins ?? ["http://localhost:3000"])
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// ========================================
// RATE LIMITING
// ========================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = rateLimitSettings.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitSettings.WindowInSeconds)
            }));
    
    options.AddPolicy("sliding", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = rateLimitSettings.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitSettings.WindowInSeconds),
                SegmentsPerWindow = 4
            }));
});

// ========================================
// API VERSIONING
// ========================================
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new MediaTypeApiVersionReader("ver"));
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ========================================
// CONTROLLERS & JSON
// ========================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ========================================
// FLUENT VALIDATION
// ========================================
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ========================================
// OPENAPI CONFIGURATION
// ========================================
// Using native OpenAPI for .NET 10
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "CRM SaaS API",
            Version = "v1",
            Description = "A comprehensive CRM SaaS API with multi-tenancy support"
        };
        return Task.CompletedTask;
    });
    
    // Add JWT Bearer authentication to OpenAPI
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme()
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            }
        };
        return Task.CompletedTask;
    });
    
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [
                    new OpenApiSecuritySchemeReference("Bearer")
                ] = new List<string>()
            }
        };
        return Task.CompletedTask;
    });
});

// ========================================
// HEALTH CHECKS
// ========================================
builder.Services.AddHealthChecks();

// ========================================
// HTTP CONTEXT ACCESSOR
// ========================================
builder.Services.AddHttpContextAccessor();

// ========================================
// MEMORY CACHE
// ========================================
builder.Services.AddMemoryCache();

// ========================================
// HANGFIRE - BACKGROUND JOBS
// ========================================
var hangfireConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(hangfireConnectionString, new Hangfire.SqlServer.SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true,
            SchemaName = "hangfire"
        });
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.ServerName = $"{Environment.MachineName}:{Guid.NewGuid()}";
});

// ========================================
// BUILD APPLICATION
// ========================================
var app = builder.Build();

// ========================================
// MIDDLEWARE PIPELINE
// ========================================

// Exception handling (first in pipeline)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});

// Development-specific middleware
if (app.Environment.IsDevelopment())
{
    // Enable OpenAPI endpoint
    app.MapOpenApi();
    
    // Map Scalar UI (which uses the OpenAPI document)
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("CRM SaaS API");
        options.WithTheme(ScalarTheme.BluePlanet);
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// CORS (must be before HTTPS redirection to handle preflight requests)
app.UseCors();

// HTTPS Redirection (skip in Development when using HTTP)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Rate Limiting
app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Multi-tenancy middleware (after authentication)
app.UseMiddleware<TenantMiddleware>();

// Hangfire Dashboard (only in Development or with authentication)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
    {
        Authorization = new[] { new CrmSaas.Api.HangfireAuthorizationFilter() }
    });
}

// Health check endpoint
app.MapHealthChecks("/health");

// Map controllers
app.MapControllers();

// ========================================
// DATABASE INITIALIZATION
// ========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var masterContext = services.GetRequiredService<MasterDbContext>();
        var tenantContext = services.GetRequiredService<TenantDbContext>();
        
        Log.Information("Applying database migrations...");
        
        await masterContext.Database.MigrateAsync();
        await tenantContext.Database.MigrateAsync();
        
        Log.Information("Seeding database...");
        
        await DatabaseSeeder.SeedAsync(masterContext, tenantContext);
        
        Log.Information("Database initialization completed");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the database. Please ensure SQL Server is running and connection string is correct.");
        Log.Warning("The application will continue running, but database operations may fail.");
        
        // Don't throw - let the app start so API documentation is accessible
    }
}

// ========================================
// SETUP RECURRING JOBS
// ========================================
using (var scope = app.Services.CreateScope())
{
    var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
    
    // SLA breach check - every 5 minutes
    backgroundJobService.AddOrUpdateRecurringJob<ScheduledJobsService>(
        "sla-breach-check",
        x => x.CheckSlaBreachesAsync(),
        "*/5 * * * *"); // Every 5 minutes
    
    // Activity reminders - every 15 minutes
    backgroundJobService.AddOrUpdateRecurringJob<ScheduledJobsService>(
        "activity-reminders",
        x => x.SendActivityRemindersAsync(),
        "*/15 * * * *"); // Every 15 minutes
    
    // Scheduled workflow processing - every minute
    backgroundJobService.AddOrUpdateRecurringJob<ScheduledJobsService>(
        "scheduled-workflows",
        x => x.ProcessScheduledWorkflowsAsync(),
        "* * * * *"); // Every minute
    
    // Contract renewal reminders - daily at 9 AM
    backgroundJobService.AddOrUpdateRecurringJob<ScheduledJobsService>(
        "contract-renewal-reminders",
        x => x.SendContractRenewalRemindersAsync(),
        "0 9 * * *"); // 9 AM daily
    
    // Cleanup old notifications - daily at 2 AM
    backgroundJobService.AddOrUpdateRecurringJob<ScheduledJobsService>(
        "cleanup-old-notifications",
        x => x.CleanupOldNotificationsAsync(),
        "0 2 * * *"); // 2 AM daily
    
    // Purge deleted records - weekly on Sundays at 3 AM
    backgroundJobService.AddOrUpdateRecurringJob<ScheduledJobsService>(
        "purge-deleted-records",
        x => x.PurgeDeletedRecordsAsync(),
        "0 3 * * 0"); // 3 AM every Sunday
    
    // Process pending webhook deliveries - every minute
    var webhookDeliveryService = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryService>();
    RecurringJob.AddOrUpdate<IWebhookDeliveryService>(
        "process-pending-webhooks",
        x => x.ProcessPendingDeliveriesAsync(default),
        "* * * * *"); // Every minute
    
    // Retry failed webhook deliveries - every 5 minutes
    RecurringJob.AddOrUpdate<IWebhookDeliveryService>(
        "retry-failed-webhooks",
        x => x.RetryFailedDeliveriesAsync(default),
        "*/5 * * * *"); // Every 5 minutes
    
    // Send activity reminders - every 5 minutes
    RecurringJob.AddOrUpdate<IActivityReminderService>(
        "send-activity-reminders",
        x => x.SendDueRemindersAsync(default),
        "*/5 * * * *"); // Every 5 minutes
    
    Log.Information("Recurring jobs configured successfully");
}

// ========================================
// HEALTH CHECKS
// ========================================
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                data = x.Value.Data,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

// Liveness probe - simple check that application is running
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // No health checks, just returns 200 OK if app is alive
});

// Readiness probe - checks if app is ready to serve requests
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// ========================================
// RUN APPLICATION
// ========================================
Log.Information("Starting CRM SaaS API on {Environment}", app.Environment.EnvironmentName);

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }
