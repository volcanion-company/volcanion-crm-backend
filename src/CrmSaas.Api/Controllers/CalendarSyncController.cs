using CrmSaas.Api.Common;
using CrmSaas.Api.DTOs.Calendar;
using CrmSaas.Api.Entities;
using CrmSaas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSaas.Api.Controllers;

[ApiController]
[Route("api/v1/calendar")]
[Authorize]
public class CalendarSyncController : ControllerBase
{
    private readonly ICalendarSyncService _calendarSyncService;
    private readonly IICalService _iCalService;
    private readonly IActivityReminderService _activityReminderService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CalendarSyncController> _logger;

    public CalendarSyncController(
        ICalendarSyncService calendarSyncService,
        IICalService iCalService,
        IActivityReminderService activityReminderService,
        ICurrentUserService currentUser,
        ILogger<CalendarSyncController> logger)
    {
        _calendarSyncService = calendarSyncService;
        _iCalService = iCalService;
        _activityReminderService = activityReminderService;
        _currentUser = currentUser;
        _logger = logger;
    }

    // ===== Calendar Sync Configuration =====

    [HttpGet("sync/configurations")]
    public async Task<IActionResult> GetConfigurations(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var configs = await _calendarSyncService.GetUserConfigurationsAsync(userId.ToString(), cancellationToken);
        return Ok(ApiResponse<List<CalendarSyncConfigurationDto>>.Ok(configs));
    }

    [HttpGet("sync/configurations/{id}")]
    public async Task<IActionResult> GetConfiguration(string id, CancellationToken cancellationToken)
    {
        var config = await _calendarSyncService.GetConfigurationAsync(id, cancellationToken);
        if (config == null)
            return NotFound(ApiResponse<CalendarSyncConfigurationDto>.Fail("Calendar sync configuration not found"));

        return Ok(ApiResponse<CalendarSyncConfigurationDto>.Ok(config));
    }

    [HttpPost("sync/configurations")]
    public async Task<IActionResult> CreateConfiguration([FromBody] CreateCalendarSyncConfigurationDto dto, CancellationToken cancellationToken)
    {
        var config = await _calendarSyncService.CreateConfigurationAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetConfiguration), new { id = config.Id }, ApiResponse<CalendarSyncConfigurationDto>.Ok(config));
    }

    [HttpPut("sync/configurations/{id}")]
    public async Task<IActionResult> UpdateConfiguration(string id, [FromBody] UpdateCalendarSyncConfigurationDto dto, CancellationToken cancellationToken)
    {
        var config = await _calendarSyncService.UpdateConfigurationAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<CalendarSyncConfigurationDto>.Ok(config));
    }

    [HttpDelete("sync/configurations/{id}")]
    public async Task<IActionResult> DeleteConfiguration(string id, CancellationToken cancellationToken)
    {
        await _calendarSyncService.DeleteConfigurationAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Calendar sync configuration deleted successfully"));
    }

    // ===== OAuth Authorization =====

    [HttpGet("sync/authorize/{provider}")]
    public async Task<IActionResult> GetAuthorizationUrl(string provider, [FromQuery] string redirectUri, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CalendarProvider>(provider, true, out var calendarProvider))
            return BadRequest(ApiResponse<CalendarAuthorizationUrlDto>.Fail($"Invalid calendar provider: {provider}"));

        var result = await _calendarSyncService.GetAuthorizationUrlAsync(calendarProvider, redirectUri, cancellationToken);
        return Ok(ApiResponse<CalendarAuthorizationUrlDto>.Ok(result));
    }

    [HttpPost("sync/token-exchange/{provider}")]
    public async Task<IActionResult> ExchangeToken(string provider, [FromBody] ExchangeTokenRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CalendarProvider>(provider, true, out var calendarProvider))
            return BadRequest(ApiResponse<CalendarSyncConfigurationDto>.Fail($"Invalid calendar provider: {provider}"));

        var config = await _calendarSyncService.ExchangeCodeForTokenAsync(calendarProvider, request.Code, request.RedirectUri, cancellationToken);
        return Ok(ApiResponse<CalendarSyncConfigurationDto>.Ok(config));
    }

    // ===== Calendar Operations =====

    [HttpGet("sync/configurations/{id}/calendars")]
    public async Task<IActionResult> GetExternalCalendars(string id, CancellationToken cancellationToken)
    {
        var calendars = await _calendarSyncService.GetExternalCalendarsAsync(id, cancellationToken);
        return Ok(ApiResponse<List<ExternalCalendarDto>>.Ok(calendars));
    }

    [HttpPost("sync/configurations/{id}/sync")]
    public async Task<IActionResult> SyncCalendar(string id, CancellationToken cancellationToken)
    {
        var result = await _calendarSyncService.SyncCalendarAsync(id, cancellationToken);
        return Ok(ApiResponse<CalendarSyncResultDto>.Ok(result));
    }

    [HttpPost("sync/sync-all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SyncAllCalendars(CancellationToken cancellationToken)
    {
        var result = await _calendarSyncService.SyncAllActiveConfigurationsAsync(cancellationToken);
        return Ok(ApiResponse<CalendarSyncResultDto>.Ok(result));
    }

    // ===== Event Mappings =====

    [HttpGet("sync/mappings/activity/{activityId}")]
    public async Task<IActionResult> GetEventMapping(string activityId, CancellationToken cancellationToken)
    {
        var mapping = await _calendarSyncService.GetEventMappingAsync(activityId, cancellationToken);
        if (mapping == null)
            return NotFound(ApiResponse<CalendarEventMappingDto>.Fail("Calendar event mapping not found"));

        return Ok(ApiResponse<CalendarEventMappingDto>.Ok(mapping));
    }

    [HttpGet("sync/configurations/{id}/mappings")]
    public async Task<IActionResult> GetEventMappings(string id, CancellationToken cancellationToken)
    {
        var mappings = await _calendarSyncService.GetEventMappingsAsync(id, cancellationToken);
        return Ok(ApiResponse<List<CalendarEventMappingDto>>.Ok(mappings));
    }

    // ===== iCal Export =====

    [HttpPost("export/ical")]
    public async Task<IActionResult> ExportICalendar([FromBody] ICalExportOptionsDto options, CancellationToken cancellationToken)
    {
        var iCalContent = await _iCalService.ExportActivitiesToICalAsync(options, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(iCalContent), "text/calendar", "activities.ics");
    }

    [HttpGet("export/ical/{activityId}")]
    public async Task<IActionResult> ExportActivityICalendar(string activityId, CancellationToken cancellationToken)
    {
        var iCalContent = await _iCalService.ExportActivityToICalAsync(activityId, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(iCalContent), "text/calendar", $"activity-{activityId}.ics");
    }

    // ===== Activity Reminders =====

    [HttpGet("reminders/activity/{activityId}")]
    public async Task<IActionResult> GetActivityReminders(string activityId, CancellationToken cancellationToken)
    {
        var reminders = await _activityReminderService.GetActivityRemindersAsync(activityId, cancellationToken);
        return Ok(ApiResponse<List<ActivityReminderDto>>.Ok(reminders));
    }

    [HttpGet("reminders/my-reminders")]
    public async Task<IActionResult> GetMyReminders(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var reminders = await _activityReminderService.GetUserRemindersAsync(userId.ToString(), cancellationToken);
        return Ok(ApiResponse<List<ActivityReminderDto>>.Ok(reminders));
    }

    [HttpPost("reminders")]
    public async Task<IActionResult> CreateReminder([FromBody] CreateActivityReminderDto dto, CancellationToken cancellationToken)
    {
        var reminder = await _activityReminderService.CreateReminderAsync(dto, cancellationToken);
        return Ok(ApiResponse<ActivityReminderDto>.Ok(reminder));
    }

    [HttpDelete("reminders/{id}")]
    public async Task<IActionResult> DeleteReminder(string id, CancellationToken cancellationToken)
    {
        await _activityReminderService.DeleteReminderAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Activity reminder deleted successfully"));
    }
}

public class ExchangeTokenRequest
{
    public required string Code { get; set; }
    public required string RedirectUri { get; set; }
}
