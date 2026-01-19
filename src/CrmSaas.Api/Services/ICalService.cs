using CrmSaas.Api.Data;
using CrmSaas.Api.DTOs.Calendar;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CrmSaas.Api.Services;

public interface IICalService
{
    Task<string> ExportActivitiesToICalAsync(ICalExportOptionsDto options, CancellationToken cancellationToken = default);
    Task<string> ExportActivityToICalAsync(string activityId, CancellationToken cancellationToken = default);
    string GenerateICalContent(List<Activity> activities);
}

public class ICalService : IICalService
{
    private readonly TenantDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ICalService> _logger;

    public ICalService(
        TenantDbContext context,
        ICurrentUserService currentUser,
        ILogger<ICalService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<string> ExportActivitiesToICalAsync(ICalExportOptionsDto options, CancellationToken cancellationToken = default)
    {
        var query = _context.Activities.AsQueryable();

        // Apply filters
        if (options.ActivityIds?.Any() == true)
        {
            var guidIds = options.ActivityIds.Select(Guid.Parse).ToList();
            query = query.Where(a => guidIds.Contains(a.Id));
        }

        if (options.StartDate.HasValue)
            query = query.Where(a => a.StartDate >= options.StartDate.Value);

        if (options.EndDate.HasValue)
            query = query.Where(a => a.DueDate <= options.EndDate.Value);

        if (!string.IsNullOrEmpty(options.ActivityType))
        {
            if (Enum.TryParse<ActivityType>(options.ActivityType, out var activityType))
                query = query.Where(a => a.Type == activityType);
        }

        if (!string.IsNullOrEmpty(options.AssignedToUserId))
        {
            var userId = Guid.Parse(options.AssignedToUserId);
            query = query.Where(a => a.AssignedToUserId == userId);
        }

        if (!options.IncludeCompleted)
            query = query.Where(a => a.Status != ActivityStatus.Completed);

        var activities = await query
            .OrderBy(a => a.StartDate)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Exporting {Count} activities to iCal", activities.Count);

        return GenerateICalContent(activities);
    }

    public async Task<string> ExportActivityToICalAsync(string activityId, CancellationToken cancellationToken = default)
    {
        var guid = Guid.Parse(activityId);
        var activity = await _context.Activities
            .FirstOrDefaultAsync(a => a.Id == guid, cancellationToken)
            ?? throw new KeyNotFoundException($"Activity {activityId} not found");

        return GenerateICalContent(new List<Activity> { activity });
    }

    public string GenerateICalContent(List<Activity> activities)
    {
        var sb = new StringBuilder();

        // iCalendar header
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//CRM SaaS//Calendar Export//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine($"X-WR-CALNAME:CRM Activities");
        sb.AppendLine($"X-WR-TIMEZONE:UTC");

        // Add each activity as an event
        foreach (var activity in activities)
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{activity.Id}@crm-saas.com");
            sb.AppendLine($"DTSTAMP:{FormatDateTime(DateTime.UtcNow)}");
            
            var startTime = activity.StartDate ?? DateTime.UtcNow;
            var endTime = activity.DurationMinutes.HasValue 
                ? startTime.AddMinutes(activity.DurationMinutes.Value) 
                : startTime.AddHours(1);
            
            sb.AppendLine($"DTSTART:{FormatDateTime(startTime)}");
            sb.AppendLine($"DTEND:{FormatDateTime(endTime)}");

            sb.AppendLine($"SUMMARY:{EscapeText(activity.Subject)}");
            
            if (!string.IsNullOrEmpty(activity.Description))
                sb.AppendLine($"DESCRIPTION:{EscapeText(activity.Description)}");

            sb.AppendLine($"STATUS:{MapActivityStatus(activity.Status)}");
            sb.AppendLine($"CATEGORIES:{activity.Type}");

            sb.AppendLine($"PRIORITY:{MapPriority(activity.Priority)}");
            sb.AppendLine($"CREATED:{FormatDateTime(activity.CreatedAt)}");
            sb.AppendLine($"LAST-MODIFIED:{FormatDateTime(activity.UpdatedAt ?? activity.CreatedAt)}");

            // Add alarm/reminder if due date exists
            if (activity.DueDate.HasValue)
            {
                sb.AppendLine("BEGIN:VALARM");
                sb.AppendLine("TRIGGER:-PT15M"); // 15 minutes before
                sb.AppendLine("ACTION:DISPLAY");
                sb.AppendLine($"DESCRIPTION:Reminder: {EscapeText(activity.Subject)}");
                sb.AppendLine("END:VALARM");
            }

            sb.AppendLine("END:VEVENT");
        }

        // iCalendar footer
        sb.AppendLine("END:VCALENDAR");

        return sb.ToString();
    }

    private string FormatDateTime(DateTime dateTime)
    {
        // iCalendar format: YYYYMMDDTHHmmssZ (UTC)
        return dateTime.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
    }

    private string EscapeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }

    private string MapActivityStatus(ActivityStatus status)
    {
        return status switch
        {
            ActivityStatus.Completed => "COMPLETED",
            ActivityStatus.Cancelled => "CANCELLED",
            ActivityStatus.InProgress => "IN-PROCESS",
            _ => "TENTATIVE"
        };
    }

    private int MapPriority(ActivityPriority priority)
    {
        // iCalendar priority: 1 (highest) to 9 (lowest), 0 = undefined
        return priority switch
        {
            ActivityPriority.Urgent => 1,
            ActivityPriority.High => 3,
            ActivityPriority.Medium => 5,
            ActivityPriority.Low => 7,
            _ => 0
        };
    }
}
