using CrmSaas.Api.Data;
using CrmSaas.Api.DTOs.Calendar;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CrmSaas.Api.Services;

public interface ICalendarSyncService
{
    // Configuration management
    Task<CalendarSyncConfigurationDto> CreateConfigurationAsync(CreateCalendarSyncConfigurationDto dto, CancellationToken cancellationToken = default);
    Task<CalendarSyncConfigurationDto?> GetConfigurationAsync(string id, CancellationToken cancellationToken = default);
    Task<List<CalendarSyncConfigurationDto>> GetUserConfigurationsAsync(string userId, CancellationToken cancellationToken = default);
    Task<CalendarSyncConfigurationDto> UpdateConfigurationAsync(string id, UpdateCalendarSyncConfigurationDto dto, CancellationToken cancellationToken = default);
    Task DeleteConfigurationAsync(string id, CancellationToken cancellationToken = default);
    
    // OAuth flow
    Task<CalendarAuthorizationUrlDto> GetAuthorizationUrlAsync(CalendarProvider provider, string redirectUri, CancellationToken cancellationToken = default);
    Task<CalendarSyncConfigurationDto> ExchangeCodeForTokenAsync(CalendarProvider provider, string code, string redirectUri, CancellationToken cancellationToken = default);
    
    // Calendar operations
    Task<List<ExternalCalendarDto>> GetExternalCalendarsAsync(string configurationId, CancellationToken cancellationToken = default);
    Task<CalendarSyncResultDto> SyncCalendarAsync(string configurationId, CancellationToken cancellationToken = default);
    Task<CalendarSyncResultDto> SyncAllActiveConfigurationsAsync(CancellationToken cancellationToken = default);
    
    // Activity mapping
    Task<CalendarEventMappingDto?> GetEventMappingAsync(string activityId, CancellationToken cancellationToken = default);
    Task<List<CalendarEventMappingDto>> GetEventMappingsAsync(string configurationId, CancellationToken cancellationToken = default);
}

public class CalendarSyncService : ICalendarSyncService
{
    private readonly TenantDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CalendarSyncService> _logger;
    private readonly IConfiguration _configuration;

    public CalendarSyncService(
        TenantDbContext context,
        ICurrentUserService currentUser,
        ILogger<CalendarSyncService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<CalendarSyncConfigurationDto> CreateConfigurationAsync(CreateCalendarSyncConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        // Parse provider
        if (!Enum.TryParse<CalendarProvider>(dto.Provider, out var provider))
            throw new ArgumentException($"Invalid calendar provider: {dto.Provider}");

        // Exchange authorization code for access token
        var (accessToken, refreshToken, expiresAt, accountEmail) = await ExchangeAuthorizationCodeAsync(provider, dto.AuthorizationCode, dto.RedirectUri, cancellationToken);

        var config = new CalendarSyncConfiguration
        {
            UserId = userId,
            Provider = provider,
            ProviderAccountEmail = accountEmail,
            AccessToken = EncryptToken(accessToken),
            RefreshToken = refreshToken != null ? EncryptToken(refreshToken) : null,
            TokenExpiresAt = expiresAt,
            IsActive = true,
            SyncToExternal = dto.SyncToExternal,
            SyncFromExternal = dto.SyncFromExternal,
            Status = CalendarSyncStatus.Active,
            CalendarIds = dto.CalendarIds != null ? string.Join(",", dto.CalendarIds) : null,
            SyncDaysBack = dto.SyncDaysBack,
            SyncDaysForward = dto.SyncDaysForward,
            TotalEventsSynced = 0,
            TotalEventsCreated = 0,
            TotalEventsUpdated = 0,
            FailedSyncCount = 0
        };

        _context.CalendarSyncConfigurations.Add(config);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created calendar sync configuration {ConfigId} for user {UserId} with provider {Provider}",
            config.Id, userId, provider);

        return MapToDto(config);
    }

    public async Task<CalendarSyncConfigurationDto?> GetConfigurationAsync(string id, CancellationToken cancellationToken = default)
    {
        var guid = Guid.Parse(id);
        var config = await _context.CalendarSyncConfigurations
            .FirstOrDefaultAsync(c => c.Id == guid, cancellationToken);

        return config != null ? MapToDto(config) : null;
    }

    public async Task<List<CalendarSyncConfigurationDto>> GetUserConfigurationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var guid = Guid.Parse(userId);
        var configs = await _context.CalendarSyncConfigurations
            .Where(c => c.UserId == guid)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return configs.Select(MapToDto).ToList();
    }

    public async Task<CalendarSyncConfigurationDto> UpdateConfigurationAsync(string id, UpdateCalendarSyncConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        var guid = Guid.Parse(id);
        var config = await _context.CalendarSyncConfigurations
            .FirstOrDefaultAsync(c => c.Id == guid, cancellationToken)
            ?? throw new KeyNotFoundException($"Calendar sync configuration {id} not found");

        if (dto.IsActive.HasValue)
            config.IsActive = dto.IsActive.Value;
        if (dto.SyncToExternal.HasValue)
            config.SyncToExternal = dto.SyncToExternal.Value;
        if (dto.SyncFromExternal.HasValue)
            config.SyncFromExternal = dto.SyncFromExternal.Value;
        if (dto.CalendarIds != null)
            config.CalendarIds = string.Join(",", dto.CalendarIds);
        if (dto.SyncDaysBack.HasValue)
            config.SyncDaysBack = dto.SyncDaysBack.Value;
        if (dto.SyncDaysForward.HasValue)
            config.SyncDaysForward = dto.SyncDaysForward.Value;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated calendar sync configuration {ConfigId}", id);

        return MapToDto(config);
    }

    public async Task DeleteConfigurationAsync(string id, CancellationToken cancellationToken = default)
    {
        var guid = Guid.Parse(id);
        var config = await _context.CalendarSyncConfigurations
            .FirstOrDefaultAsync(c => c.Id == guid, cancellationToken)
            ?? throw new KeyNotFoundException($"Calendar sync configuration {id} not found");

        _context.CalendarSyncConfigurations.Remove(config);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted calendar sync configuration {ConfigId}", id);
    }

    public async Task<CalendarAuthorizationUrlDto> GetAuthorizationUrlAsync(CalendarProvider provider, string redirectUri, CancellationToken cancellationToken = default)
    {
        var state = GenerateState();
        var authUrl = provider switch
        {
            CalendarProvider.GoogleCalendar => GenerateGoogleAuthUrl(redirectUri, state),
            CalendarProvider.MicrosoftOutlook or CalendarProvider.Microsoft365 => GenerateMicrosoftAuthUrl(redirectUri, state),
            _ => throw new NotSupportedException($"Provider {provider} is not supported for OAuth")
        };

        return new CalendarAuthorizationUrlDto
        {
            AuthorizationUrl = authUrl,
            State = state
        };
    }

    public async Task<CalendarSyncConfigurationDto> ExchangeCodeForTokenAsync(CalendarProvider provider, string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        var (accessToken, refreshToken, expiresAt, accountEmail) = await ExchangeAuthorizationCodeAsync(provider, code, redirectUri, cancellationToken);

        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        var config = new CalendarSyncConfiguration
        {
            UserId = userId,
            Provider = provider,
            ProviderAccountEmail = accountEmail,
            AccessToken = EncryptToken(accessToken),
            RefreshToken = refreshToken != null ? EncryptToken(refreshToken) : null,
            TokenExpiresAt = expiresAt,
            IsActive = true,
            SyncToExternal = true,
            SyncFromExternal = true,
            Status = CalendarSyncStatus.Active,
            SyncDaysBack = 30,
            SyncDaysForward = 90
        };

        _context.CalendarSyncConfigurations.Add(config);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(config);
    }

    public async Task<List<ExternalCalendarDto>> GetExternalCalendarsAsync(string configurationId, CancellationToken cancellationToken = default)
    {
        var guid = Guid.Parse(configurationId);
        var config = await _context.CalendarSyncConfigurations
            .FirstOrDefaultAsync(c => c.Id == guid, cancellationToken)
            ?? throw new KeyNotFoundException($"Calendar sync configuration {configurationId} not found");

        // TODO: Call external API to get calendar list
        // This is a placeholder implementation
        return new List<ExternalCalendarDto>();
    }

    public async Task<CalendarSyncResultDto> SyncCalendarAsync(string configurationId, CancellationToken cancellationToken = default)
    {
        var guid = Guid.Parse(configurationId);
        var config = await _context.CalendarSyncConfigurations
            .FirstOrDefaultAsync(c => c.Id == guid, cancellationToken)
            ?? throw new KeyNotFoundException($"Calendar sync configuration {configurationId} not found");

        if (!config.IsActive)
            throw new InvalidOperationException("Calendar sync configuration is not active");

        try
        {
            config.Status = CalendarSyncStatus.Active;
            var result = new CalendarSyncResultDto
            {
                Success = true,
                SyncedAt = DateTime.UtcNow
            };

            // Sync CRM activities to external calendar
            if (config.SyncToExternal)
            {
                var syncToExternalResult = await SyncToExternalCalendarAsync(config, cancellationToken);
                result.EventsCreated += syncToExternalResult.EventsCreated;
                result.EventsUpdated += syncToExternalResult.EventsUpdated;
                result.EventsFailed += syncToExternalResult.EventsFailed;
                result.Errors.AddRange(syncToExternalResult.Errors);
            }

            // Sync external calendar events to CRM
            if (config.SyncFromExternal)
            {
                var syncFromExternalResult = await SyncFromExternalCalendarAsync(config, cancellationToken);
                result.EventsCreated += syncFromExternalResult.EventsCreated;
                result.EventsUpdated += syncFromExternalResult.EventsUpdated;
                result.EventsFailed += syncFromExternalResult.EventsFailed;
                result.Errors.AddRange(syncFromExternalResult.Errors);
            }

            // Update statistics
            config.LastSyncAt = DateTime.UtcNow;
            config.TotalEventsSynced += result.EventsCreated + result.EventsUpdated;
            config.TotalEventsCreated += result.EventsCreated;
            config.TotalEventsUpdated += result.EventsUpdated;
            if (result.EventsFailed > 0)
            {
                config.FailedSyncCount += result.EventsFailed;
                config.LastSyncError = string.Join("; ", result.Errors);
            }
            else
            {
                config.LastSyncError = null;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Synced calendar {ConfigId}: {Created} created, {Updated} updated, {Failed} failed",
                configurationId, result.EventsCreated, result.EventsUpdated, result.EventsFailed);

            return result;
        }
        catch (Exception ex)
        {
            config.Status = CalendarSyncStatus.Error;
            config.LastSyncError = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Failed to sync calendar {ConfigId}", configurationId);

            return new CalendarSyncResultDto
            {
                Success = false,
                Error = ex.Message,
                SyncedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<CalendarSyncResultDto> SyncAllActiveConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        var activeConfigs = await _context.CalendarSyncConfigurations
            .Where(c => c.IsActive && c.Status == CalendarSyncStatus.Active)
            .ToListAsync(cancellationToken);

        var totalResult = new CalendarSyncResultDto
        {
            Success = true,
            SyncedAt = DateTime.UtcNow
        };

        foreach (var config in activeConfigs)
        {
            try
            {
                var result = await SyncCalendarAsync(config.Id.ToString(), cancellationToken);
                totalResult.EventsCreated += result.EventsCreated;
                totalResult.EventsUpdated += result.EventsUpdated;
                totalResult.EventsDeleted += result.EventsDeleted;
                totalResult.EventsFailed += result.EventsFailed;
                totalResult.Errors.AddRange(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync configuration {ConfigId}", config.Id);
                totalResult.EventsFailed++;
                totalResult.Errors.Add($"Config {config.Id}: {ex.Message}");
            }
        }

        _logger.LogInformation("Synced {Count} calendar configurations: {Created} created, {Updated} updated, {Failed} failed",
            activeConfigs.Count, totalResult.EventsCreated, totalResult.EventsUpdated, totalResult.EventsFailed);

        return totalResult;
    }

    public async Task<CalendarEventMappingDto?> GetEventMappingAsync(string activityId, CancellationToken cancellationToken = default)
    {
        var guid = Guid.Parse(activityId);
        var mapping = await _context.CalendarEventMappings
            .FirstOrDefaultAsync(m => m.ActivityId == guid, cancellationToken);

        return mapping != null ? MapToDto(mapping) : null;
    }

    public async Task<List<CalendarEventMappingDto>> GetEventMappingsAsync(string configurationId, CancellationToken cancellationToken = default)
    {
        var guid = Guid.Parse(configurationId);
        var mappings = await _context.CalendarEventMappings
            .Where(m => m.CalendarSyncConfigurationId == guid)
            .OrderByDescending(m => m.LastSyncedAt)
            .ToListAsync(cancellationToken);

        return mappings.Select(MapToDto).ToList();
    }

    // Private helper methods

    private async Task<CalendarSyncResultDto> SyncToExternalCalendarAsync(CalendarSyncConfiguration config, CancellationToken cancellationToken)
    {
        // TODO: Implement actual sync logic
        // 1. Get activities that need to be synced
        // 2. Create/update events in external calendar
        // 3. Create/update event mappings
        return new CalendarSyncResultDto { Success = true, SyncedAt = DateTime.UtcNow };
    }

    private async Task<CalendarSyncResultDto> SyncFromExternalCalendarAsync(CalendarSyncConfiguration config, CancellationToken cancellationToken)
    {
        // TODO: Implement actual sync logic
        // 1. Get events from external calendar
        // 2. Create/update activities in CRM
        // 3. Create/update event mappings
        return new CalendarSyncResultDto { Success = true, SyncedAt = DateTime.UtcNow };
    }

    private async Task<(string accessToken, string? refreshToken, DateTime? expiresAt, string accountEmail)> ExchangeAuthorizationCodeAsync(
        CalendarProvider provider, string code, string? redirectUri, CancellationToken cancellationToken)
    {
        // TODO: Implement OAuth token exchange
        // This is a placeholder that returns dummy data
        return ("access_token_placeholder", "refresh_token_placeholder", DateTime.UtcNow.AddHours(1), "user@example.com");
    }

    private string GenerateGoogleAuthUrl(string redirectUri, string state)
    {
        var clientId = _configuration["GoogleCalendar:ClientId"] ?? throw new InvalidOperationException("GoogleCalendar:ClientId not configured");
        var scope = "https://www.googleapis.com/auth/calendar";
        return $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(scope)}&state={state}&access_type=offline&prompt=consent";
    }

    private string GenerateMicrosoftAuthUrl(string redirectUri, string state)
    {
        var clientId = _configuration["MicrosoftCalendar:ClientId"] ?? throw new InvalidOperationException("MicrosoftCalendar:ClientId not configured");
        var scope = "https://graph.microsoft.com/Calendars.ReadWrite offline_access";
        return $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(scope)}&state={state}";
    }

    private string GenerateState()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string EncryptToken(string token)
    {
        // TODO: Implement proper encryption (e.g., using Azure Key Vault or Data Protection API)
        // For now, just return the token (NOT PRODUCTION READY)
        return token;
    }

    private string DecryptToken(string encryptedToken)
    {
        // TODO: Implement proper decryption
        return encryptedToken;
    }

    private CalendarSyncConfigurationDto MapToDto(CalendarSyncConfiguration config)
    {
        return new CalendarSyncConfigurationDto
        {
            Id = config.Id.ToString(),
            UserId = config.UserId.ToString(),
            Provider = config.Provider.ToString(),
            ProviderAccountEmail = config.ProviderAccountEmail,
            IsActive = config.IsActive,
            SyncToExternal = config.SyncToExternal,
            SyncFromExternal = config.SyncFromExternal,
            LastSyncAt = config.LastSyncAt,
            Status = config.Status.ToString(),
            LastSyncError = config.LastSyncError,
            CalendarIds = config.CalendarIds?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            SyncDaysBack = config.SyncDaysBack,
            SyncDaysForward = config.SyncDaysForward,
            TotalEventsSynced = config.TotalEventsSynced,
            TotalEventsCreated = config.TotalEventsCreated,
            TotalEventsUpdated = config.TotalEventsUpdated,
            FailedSyncCount = config.FailedSyncCount,
            CreatedAt = config.CreatedAt
        };
    }

    private CalendarEventMappingDto MapToDto(CalendarEventMapping mapping)
    {
        return new CalendarEventMappingDto
        {
            Id = mapping.Id.ToString(),
            ActivityId = mapping.ActivityId?.ToString(),
            ExternalEventId = mapping.ExternalEventId,
            ExternalCalendarId = mapping.ExternalCalendarId,
            Provider = mapping.Provider.ToString(),
            Direction = mapping.Direction.ToString(),
            LastSyncedAt = mapping.LastSyncedAt,
            SyncStatus = mapping.SyncStatus.ToString(),
            LastSyncError = mapping.LastSyncError,
            EventTitle = mapping.EventTitle,
            EventStartAt = mapping.EventStartAt,
            EventEndAt = mapping.EventEndAt,
            IsAllDay = mapping.IsAllDay
        };
    }
}
