using CrmSaas.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Services;

/// <summary>
/// Service containing scheduled job implementations
/// </summary>
public class ScheduledJobsService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledJobsService> _logger;

    public ScheduledJobsService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledJobsService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Check and process SLA breaches
    /// Runs every 5 minutes
    /// </summary>
    public async Task CheckSlaBreachesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var slaService = scope.ServiceProvider.GetService<ISlaAutomationService>();
        
        try
        {
            if (slaService != null)
            {
                await slaService.CheckAndEscalateTicketsAsync();
                _logger.LogInformation("SLA check and auto-escalation completed");
            }
            else
            {
                _logger.LogWarning("SlaAutomationService not available - skipping SLA check");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SLA breaches");
            throw;
        }
    }

    /// <summary>
    /// Send activity reminders
    /// Runs every 15 minutes
    /// </summary>
    public async Task SendActivityRemindersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        
        try
        {
            var now = DateTime.UtcNow;
            var reminderWindow = now.AddMinutes(15); // Send reminders for next 15 min
            
            // Find activities with reminders due
            var activities = await context.Activities
                .Include(a => a.AssignedToUser)
                .Where(a => 
                    a.DueDate.HasValue &&
                    a.DueDate.Value >= now &&
                    a.DueDate.Value <= reminderWindow)
                .ToListAsync();

            foreach (var activity in activities)
            {
                if (activity.AssignedToUserId.HasValue && activity.AssignedToUser != null)
                {
                    await notificationService.SendNotificationAsync(new NotificationRequest
                    {
                        UserId = activity.AssignedToUserId.Value,
                        Type = Entities.NotificationType.ActivityReminder,
                        Title = $"Activity Reminder: {activity.Subject}",
                        Message = $"Your activity '{activity.Subject}' is due at {activity.DueDate:HH:mm}",
                        Priority = Entities.NotificationPriority.High,
                        RelatedEntityType = "Activity",
                        RelatedEntityId = activity.Id
                    });
                }
            }

            if (activities.Any())
            {
                _logger.LogInformation("Sent {Count} activity reminders", activities.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending activity reminders");
            throw;
        }
    }

    /// <summary>
    /// Clean up old notifications
    /// Runs daily at 2 AM
    /// </summary>
    public async Task CleanupOldNotificationsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep 30 days
            
            var oldNotifications = await context.Notifications
                .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
                .ToListAsync();

            context.Notifications.RemoveRange(oldNotifications);
            await context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} old notifications", oldNotifications.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old notifications");
            throw;
        }
    }

    /// <summary>
    /// Process scheduled workflow triggers
    /// Runs every minute
    /// </summary>
    public async Task ProcessScheduledWorkflowsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var workflowEngine = scope.ServiceProvider.GetRequiredService<IWorkflowEngine>();
        
        try
        {
            // TODO: Implement cron-based workflow scheduling
            // For now, this is a placeholder
            
            _logger.LogDebug("Scheduled workflow check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled workflows");
            throw;
        }
    }

    /// <summary>
    /// Send contract renewal reminders
    /// Runs daily at 9 AM
    /// </summary>
    public async Task SendContractRenewalRemindersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        
        try
        {
            var now = DateTime.UtcNow;
            var reminderDate = now.AddDays(30); // 30 days before expiry
            
            var expiringContracts = await context.Contracts
                .Include(c => c.AssignedToUser)
                .Where(c => 
                    c.EndDate <= reminderDate &&
                    c.EndDate >= now)
                .ToListAsync();

            foreach (var contract in expiringContracts)
            {
                if (contract.AssignedToUserId != null)
                {
                    var daysRemaining = (contract.EndDate - now).Days;
                    
                    await notificationService.SendNotificationAsync(new NotificationRequest
                    {
                        UserId = contract.AssignedToUserId.Value,
                        Type = Entities.NotificationType.TaskAssigned, // Use existing type
                        Title = "Contract Expiring Soon",
                        Message = $"Contract '{contract.ContractNumber}' expires in {daysRemaining} days",
                        Priority = Entities.NotificationPriority.High,
                        RelatedEntityType = "Contract",
                        RelatedEntityId = contract.Id
                    });
                }
            }

            _logger.LogInformation("Sent {Count} contract renewal reminders", expiringContracts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending contract renewal reminders");
            throw;
        }
    }

    /// <summary>
    /// Clean up soft-deleted records permanently
    /// Runs weekly (Sundays at 3 AM)
    /// </summary>
    public async Task PurgeDeletedRecordsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-90); // Purge after 90 days
            
            // This would need to be done for each entity type
            // For now, just log
            _logger.LogInformation("Purge deleted records job executed - cutoff date: {Date}", cutoffDate);
            
            // TODO: Implement permanent deletion for soft-deleted records older than 90 days
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purging deleted records");
            throw;
        }
    }
}
