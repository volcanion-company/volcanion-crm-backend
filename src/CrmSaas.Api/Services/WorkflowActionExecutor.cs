using CrmSaas.Api.Entities;
using CrmSaas.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Reflection;

namespace CrmSaas.Api.Services;

public interface IWorkflowActionExecutor
{
    Task<WorkflowActionResult> ExecuteActionAsync(WorkflowAction action, object entity, CancellationToken cancellationToken = default);
}

public class WorkflowActionExecutor : IWorkflowActionExecutor
{
    private readonly TenantDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IBackgroundJobService? _backgroundJobService;
    private readonly ILogger<WorkflowActionExecutor> _logger;

    public WorkflowActionExecutor(
        TenantDbContext context,
        ICurrentUserService currentUserService,
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<WorkflowActionExecutor> logger,
        IBackgroundJobService? backgroundJobService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _emailService = emailService;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    public async Task<WorkflowActionResult> ExecuteActionAsync(
        WorkflowAction action, 
        object entity, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Handle delayed execution
            if (action.DelayMinutes > 0 && _backgroundJobService != null)
            {
                var delay = TimeSpan.FromMinutes(action.DelayMinutes);
                _backgroundJobService.Schedule<IWorkflowActionExecutor>(
                    x => x.ExecuteActionAsync(action, entity, default),
                    delay);
                
                _logger.LogInformation(
                    "Scheduled delayed action {ActionId} to execute in {Minutes} minutes",
                    action.Id, action.DelayMinutes);
                
                return WorkflowActionResult.Success($"Action scheduled for {delay.TotalMinutes} minutes from now");
            }

            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(action.ActionConfig);
            if (config == null)
            {
                return WorkflowActionResult.Failed("Invalid action configuration");
            }

            var result = action.ActionType switch
            {
                WorkflowActionType.UpdateField => await ExecuteUpdateFieldAsync(entity, config, cancellationToken),
                WorkflowActionType.SendEmail => await ExecuteSendEmailAsync(entity, config, cancellationToken),
                WorkflowActionType.CreateTask => await ExecuteCreateTaskAsync(entity, config, cancellationToken),
                WorkflowActionType.AssignOwner => await ExecuteAssignOwnerAsync(entity, config, cancellationToken),
                WorkflowActionType.CreateActivity => await ExecuteCreateActivityAsync(entity, config, cancellationToken),
                WorkflowActionType.SendWebhook => await ExecuteSendWebhookAsync(entity, config, cancellationToken),
                WorkflowActionType.CreateRecord => await ExecuteCreateRecordAsync(entity, config, cancellationToken),
                WorkflowActionType.SendNotification => await ExecuteSendNotificationAsync(entity, config, cancellationToken),
                _ => WorkflowActionResult.Failed($"Action type {action.ActionType} not implemented")
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow action {ActionId} of type {ActionType}", 
                action.Id, action.ActionType);
            return WorkflowActionResult.Failed(ex.Message);
        }
    }

    private async Task<WorkflowActionResult> ExecuteUpdateFieldAsync(
        object entity, 
        Dictionary<string, JsonElement> config,
        CancellationToken cancellationToken)
    {
        if (!config.TryGetValue("Field", out var fieldElement) ||
            !config.TryGetValue("Value", out var valueElement))
        {
            return WorkflowActionResult.Failed("UpdateField requires 'Field' and 'Value' configuration");
        }

        var field = fieldElement.GetString();
        var value = valueElement.GetString();

        if (string.IsNullOrEmpty(field))
        {
            return WorkflowActionResult.Failed("Field name is required");
        }

        // Use reflection to set field value
        var prop = entity.GetType().GetProperty(field, BindingFlags.Public | BindingFlags.Instance);
        if (prop == null || !prop.CanWrite)
        {
            return WorkflowActionResult.Failed($"Field '{field}' not found or not writable");
        }

        try
        {
            // Convert value to property type
            var convertedValue = ConvertValue(value, prop.PropertyType);
            prop.SetValue(entity, convertedValue);

            return WorkflowActionResult.Success($"Updated field '{field}' to '{value}'");
        }
        catch (Exception ex)
        {
            return WorkflowActionResult.Failed($"Failed to update field: {ex.Message}");
        }
    }

    private async Task<WorkflowActionResult> ExecuteSendEmailAsync(
        object entity,
        Dictionary<string, JsonElement> config,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract config
            var to = config.TryGetValue("To", out var toElement) ? toElement.GetString() : null;
            var subject = config.TryGetValue("Subject", out var subjectElement) ? subjectElement.GetString() : "Workflow Email";
            var body = config.TryGetValue("Body", out var bodyElement) ? bodyElement.GetString() : string.Empty;
            var isHtml = config.TryGetValue("IsHtml", out var isHtmlElement) && isHtmlElement.GetBoolean();

            if (string.IsNullOrEmpty(to))
            {
                return WorkflowActionResult.Failed("Email recipient (To) is required");
            }

            // Support user ID lookup - if To is a Guid, fetch email from user
            if (Guid.TryParse(to, out var userId))
            {
                var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    return WorkflowActionResult.Failed($"User {userId} not found or has no email");
                }
                to = user.Email;
            }
            // Support placeholder like {{AssignedToUserId}}
            else if (to.StartsWith("{{") && to.EndsWith("}}"))
            {
                var placeholderUserId = ParsePlaceholder(to, entity);
                var user = await _context.Users.FindAsync(new object[] { placeholderUserId }, cancellationToken);
                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    return WorkflowActionResult.Failed($"User for placeholder {to} not found or has no email");
                }
                to = user.Email;
            }

            // Send email
            await _emailService.SendEmailAsync(to, subject ?? "Workflow Email", body, isHtml, cancellationToken);

            return WorkflowActionResult.Success($"Email sent to {to}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email");
            return WorkflowActionResult.Failed($"Failed to send email: {ex.Message}");
        }
    }

    private async Task<WorkflowActionResult> ExecuteCreateTaskAsync(
        object entity,
        Dictionary<string, JsonElement> config,
        CancellationToken cancellationToken)
    {
        var subject = config.TryGetValue("Subject", out var subjectElement) ? subjectElement.GetString() : "Workflow Task";
        var dueDateStr = config.TryGetValue("DueDate", out var dueDateElement) ? dueDateElement.GetString() : null;
        var assignedToStr = config.TryGetValue("AssignedTo", out var assignedToElement) ? assignedToElement.GetString() : null;

        // Parse due date (support relative dates like "+3d", "+1w")
        var dueDate = ParseRelativeDate(dueDateStr);

        // Parse assigned user
        var assignedToUserId = ParseUserId(assignedToStr, entity);

        // Create activity (task)
        var activity = new Activity
        {
            Subject = subject ?? "Workflow Task",
            Type = ActivityType.Task,
            Status = ActivityStatus.NotStarted,
            Priority = ActivityPriority.Medium,
            DueDate = dueDate,
            AssignedToUserId = assignedToUserId ?? _currentUserService.UserId,
            CreatedAt = DateTime.UtcNow
        };

        // Link to entity if it has an Id property
        var entityIdProp = entity.GetType().GetProperty("Id");
        if (entityIdProp != null)
        {
            var entityId = (Guid?)entityIdProp.GetValue(entity);
            var entityType = entity.GetType().Name;

            // Set appropriate foreign key based on entity type
            switch (entityType)
            {
                case "Lead":
                    activity.LeadId = entityId;
                    break;
                case "Opportunity":
                    activity.OpportunityId = entityId;
                    break;
                case "Customer":
                    activity.CustomerId = entityId;
                    break;
                case "Ticket":
                    activity.TicketId = entityId;
                    break;
            }
        }

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);

        return WorkflowActionResult.Success($"Created task: {subject}");
    }

    private async Task<WorkflowActionResult> ExecuteAssignOwnerAsync(
        object entity,
        Dictionary<string, JsonElement> config,
        CancellationToken cancellationToken)
    {
        if (!config.TryGetValue("UserId", out var userIdElement))
        {
            return WorkflowActionResult.Failed("AssignOwner requires 'UserId' configuration");
        }

        var userIdStr = userIdElement.GetString();
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return WorkflowActionResult.Failed("Invalid UserId");
        }

        // Find AssignedToUserId or OwnerId property
        var assignedProp = entity.GetType().GetProperty("AssignedToUserId", BindingFlags.Public | BindingFlags.Instance);
        var ownerProp = entity.GetType().GetProperty("OwnerId", BindingFlags.Public | BindingFlags.Instance);

        if (assignedProp != null && assignedProp.CanWrite)
        {
            assignedProp.SetValue(entity, userId);
            return WorkflowActionResult.Success($"Assigned to user {userId}");
        }
        else if (ownerProp != null && ownerProp.CanWrite)
        {
            ownerProp.SetValue(entity, userId);
            return WorkflowActionResult.Success($"Set owner to user {userId}");
        }

        return WorkflowActionResult.Failed("Entity does not have AssignedToUserId or OwnerId property");
    }

    private async Task<WorkflowActionResult> ExecuteCreateActivityAsync(
        object entity,
        Dictionary<string, JsonElement> config,
        CancellationToken cancellationToken)
    {
        // Similar to CreateTask but with more flexibility
        var typeStr = config.TryGetValue("Type", out var typeElement) ? typeElement.GetString() : "Task";
        var subject = config.TryGetValue("Subject", out var subjectElement) ? subjectElement.GetString() : "Workflow Activity";
        var dueDateStr = config.TryGetValue("DueDate", out var dueDateElement) ? dueDateElement.GetString() : null;

        var activityType = Enum.TryParse<ActivityType>(typeStr, true, out var parsed) ? parsed : ActivityType.Task;
        var dueDate = ParseRelativeDate(dueDateStr);

        var activity = new Activity
        {
            Subject = subject ?? "Workflow Activity",
            Type = activityType,
            Status = ActivityStatus.NotStarted,
            Priority = ActivityPriority.Medium,
            DueDate = dueDate,
            AssignedToUserId = _currentUserService.UserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);

        return WorkflowActionResult.Success($"Created {activityType} activity: {subject}");
    }

    private async Task<WorkflowActionResult> ExecuteSendWebhookAsync(
        object entity,
        Dictionary<string, JsonElement> config,
        CancellationToken cancellationToken)
    {
        // TODO: Implement webhook sending
        var url = config.TryGetValue("Url", out var urlElement) ? urlElement.GetString() : null;
        _logger.LogInformation("Workflow action: Send webhook to {Url}", url);

        return WorkflowActionResult.Success($"Webhook sent to {url}");
    }

    private async Task<WorkflowActionResult> ExecuteCreateRecordAsync(
        object entity,
        Dictionary<string, JsonElement> config,
        CancellationToken cancellationToken)
    {
        // TODO: Implement record creation
        _logger.LogInformation("Workflow action: Create record");
        return WorkflowActionResult.Success("Record creation not yet implemented");
    }

    private async Task<WorkflowActionResult> ExecuteSendNotificationAsync(
        object entity,
        Dictionary<string, JsonElement> config,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract config
            var title = config.TryGetValue("Title", out var titleElement) ? titleElement.GetString() : "Workflow Notification";
            var message = config.TryGetValue("Message", out var messageElement) ? messageElement.GetString() : null;
            var userIdStr = config.TryGetValue("UserId", out var userIdElement) ? userIdElement.GetString() : null;
            var channelsStr = config.TryGetValue("Channels", out var channelsElement) ? channelsElement.GetString() : "InApp";

            // Determine recipient
            Guid userId;
            if (!string.IsNullOrEmpty(userIdStr))
            {
                // Specific user or placeholder like {{AssignedToUserId}}
                userId = ParsePlaceholder(userIdStr, entity);
            }
            else
            {
                // Default to current user
                userId = _currentUserService.UserId ?? Guid.Empty;
            }

            if (userId == Guid.Empty)
            {
                return WorkflowActionResult.Failed("No valid recipient for notification");
            }

            // Parse channels
            var channels = NotificationChannel.InApp;
            if (Enum.TryParse<NotificationChannel>(channelsStr, true, out var parsedChannels))
            {
                channels = parsedChannels;
            }

            // Get entity info for context
            var entityType = entity.GetType().Name;
            var entityId = GetEntityId(entity);

            // Send notification
            await _notificationService.SendNotificationAsync(new NotificationRequest
            {
                UserId = userId,
                Title = title ?? "Workflow Notification",
                Message = message,
                Type = NotificationType.WorkflowAction,
                Priority = NotificationPriority.Normal,
                Channels = channels,
                RelatedEntityType = entityType,
                RelatedEntityId = entityId
            }, cancellationToken);

            return WorkflowActionResult.Success($"Notification sent to user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification");
            return WorkflowActionResult.Failed($"Failed to send notification: {ex.Message}");
        }
    }

    #region Helper Methods

    private DateTime? ParseRelativeDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
            return null;

        // Support formats: "+3d" (3 days), "+1w" (1 week), "+2h" (2 hours)
        if (dateStr.StartsWith("+"))
        {
            var numStr = dateStr[1..^1];
            var unit = dateStr[^1];

            if (int.TryParse(numStr, out var num))
            {
                return unit switch
                {
                    'h' => DateTime.UtcNow.AddHours(num),
                    'd' => DateTime.UtcNow.AddDays(num),
                    'w' => DateTime.UtcNow.AddDays(num * 7),
                    'm' => DateTime.UtcNow.AddMonths(num),
                    _ => null
                };
            }
        }

        // Try parse as absolute date
        if (DateTime.TryParse(dateStr, out var date))
        {
            return date;
        }

        return null;
    }

    private Guid? ParseUserId(string? userIdStr, object entity)
    {
        if (string.IsNullOrEmpty(userIdStr))
            return null;

        // Support placeholders: "{{OwnerId}}", "{{AssignedToUserId}}"
        if (userIdStr.StartsWith("{{") && userIdStr.EndsWith("}}"))
        {
            var fieldName = userIdStr[2..^2];
            var prop = entity.GetType().GetProperty(fieldName);
            if (prop != null)
            {
                var value = prop.GetValue(entity);
                if (value is Guid guidValue)
                    return guidValue;
            }
            return null;
        }

        // Direct GUID
        if (Guid.TryParse(userIdStr, out var userId))
        {
            return userId;
        }

        return null;
    }

    private object? ConvertValue(string? value, Type targetType)
    {
        if (value == null) return null;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(string))
            return value;

        if (underlyingType == typeof(int))
            return int.Parse(value);

        if (underlyingType == typeof(decimal))
            return decimal.Parse(value);

        if (underlyingType == typeof(bool))
            return bool.Parse(value);

        if (underlyingType == typeof(DateTime))
            return DateTime.Parse(value);

        if (underlyingType == typeof(Guid))
            return Guid.Parse(value);

        if (underlyingType.IsEnum)
            return Enum.Parse(underlyingType, value, true);

        return Convert.ChangeType(value, underlyingType);
    }

    private Guid ParsePlaceholder(string placeholder, object entity)
    {
        if (placeholder.StartsWith("{{") && placeholder.EndsWith("}}"))
        {
            var fieldName = placeholder[2..^2];
            var prop = entity.GetType().GetProperty(fieldName);
            if (prop != null)
            {
                var value = prop.GetValue(entity);
                if (value is Guid guidValue)
                    return guidValue;
            }
        }
        else if (Guid.TryParse(placeholder, out var guid))
        {
            return guid;
        }

        return Guid.Empty;
    }

    private Guid GetEntityId(object entity)
    {
        var idProp = entity.GetType().GetProperty("Id");
        if (idProp != null && idProp.PropertyType == typeof(Guid))
        {
            return (Guid)(idProp.GetValue(entity) ?? Guid.Empty);
        }
        return Guid.Empty;
    }

    #endregion
}

#region Models

public class WorkflowActionResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object>? Data { get; set; }

    public static WorkflowActionResult Success(string message, Dictionary<string, object>? data = null)
    {
        return new WorkflowActionResult
        {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    public static WorkflowActionResult Failed(string message)
    {
        return new WorkflowActionResult
        {
            IsSuccess = false,
            Message = message
        };
    }
}

#endregion
