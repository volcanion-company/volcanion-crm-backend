using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Services;

public interface ISlaAutomationService
{
    Task InitializeSlaForTicketAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task CheckAndEscalateTicketsAsync(CancellationToken cancellationToken = default);
    Task ProcessTicketResponseAsync(Ticket ticket, bool isFirstResponse, CancellationToken cancellationToken = default);
}

public class SlaAutomationService : ISlaAutomationService
{
    private readonly TenantDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SlaAutomationService> _logger;

    public SlaAutomationService(
        TenantDbContext context,
        INotificationService notificationService,
        ILogger<SlaAutomationService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task InitializeSlaForTicketAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        // Set SLA targets based on priority
        var (firstResponseMinutes, resolutionMinutes) = GetSlaTargets(ticket.Priority);

        ticket.FirstResponseTarget = DateTime.UtcNow.AddMinutes(firstResponseMinutes);
        ticket.ResolutionTarget = DateTime.UtcNow.AddMinutes(resolutionMinutes);
        ticket.SlaPaused = false;
        ticket.SlaPausedMinutes = 0;
        ticket.EscalationCount = 0;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "SLA initialized for ticket {TicketId}: FirstResponse={FirstResponse}, Resolution={Resolution}",
            ticket.Id, ticket.FirstResponseTarget, ticket.ResolutionTarget);
    }

    public async Task CheckAndEscalateTicketsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Find tickets approaching SLA breach (80% of time elapsed)
        var ticketsAtRisk = await _context.Tickets
            .Include(t => t.AssignedToUser)
            .Where(t => t.Status != TicketStatus.Closed &&
                       t.Status != TicketStatus.Resolved &&
                       !t.SlaPaused &&
                       t.ResolutionTarget.HasValue &&
                       !t.SlaBreached)
            .ToListAsync(cancellationToken);

        foreach (var ticket in ticketsAtRisk)
        {
            var elapsedTime = (now - ticket.CreatedAt).TotalMinutes - ticket.SlaPausedMinutes;
            var totalTime = (ticket.ResolutionTarget!.Value - ticket.CreatedAt).TotalMinutes;
            var percentElapsed = (elapsedTime / totalTime) * 100;

            // Auto-escalate at 80% threshold
            if (percentElapsed >= 80 && ticket.EscalationCount == 0)
            {
                await EscalateTicketAsync(ticket, "Approaching SLA breach (80% time elapsed)", cancellationToken);
            }
            // Second escalation at 95%
            else if (percentElapsed >= 95 && ticket.EscalationCount == 1)
            {
                await EscalateTicketAsync(ticket, "Critical: Approaching SLA breach (95% time elapsed)", cancellationToken);
            }
        }
    }

    public async Task ProcessTicketResponseAsync(
        Ticket ticket, 
        bool isFirstResponse, 
        CancellationToken cancellationToken = default)
    {
        if (isFirstResponse && !ticket.FirstResponseDate.HasValue)
        {
            ticket.FirstResponseDate = DateTime.UtcNow;

            // Check if first response SLA met
            if (ticket.FirstResponseTarget.HasValue)
            {
                var elapsedMinutes = (DateTime.UtcNow - ticket.CreatedAt).TotalMinutes - ticket.SlaPausedMinutes;
                var targetMinutes = (ticket.FirstResponseTarget.Value - ticket.CreatedAt).TotalMinutes;

                if (elapsedMinutes <= targetMinutes)
                {
                    _logger.LogInformation(
                        "First response SLA met for ticket {TicketId}: {ElapsedMinutes}min <= {TargetMinutes}min",
                        ticket.Id, elapsedMinutes, targetMinutes);
                }
                else
                {
                    _logger.LogWarning(
                        "First response SLA breached for ticket {TicketId}: {ElapsedMinutes}min > {TargetMinutes}min",
                        ticket.Id, elapsedMinutes, targetMinutes);

                    ticket.SlaBreached = true;

                    // Notify assigned user and manager
                    if (ticket.AssignedToUserId.HasValue)
                    {
                        await _notificationService.SendNotificationAsync(new NotificationRequest
                        {
                            UserId = ticket.AssignedToUserId.Value,
                            Type = NotificationType.SlaViolation,
                            Title = "SLA Breach: First Response",
                            Message = $"Ticket #{ticket.TicketNumber} exceeded first response SLA",
                            Priority = NotificationPriority.Urgent,
                            RelatedEntityType = "Ticket",
                            RelatedEntityId = ticket.Id
                        }, cancellationToken);
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EscalateTicketAsync(Ticket ticket, string reason, CancellationToken cancellationToken)
    {
        // Increase priority
        if (ticket.Priority == TicketPriority.Low)
            ticket.Priority = TicketPriority.Medium;
        else if (ticket.Priority == TicketPriority.Medium)
            ticket.Priority = TicketPriority.High;
        else if (ticket.Priority == TicketPriority.High)
            ticket.Priority = TicketPriority.Critical;

        ticket.EscalationCount = ticket.EscalationCount + 1;
        ticket.LastEscalatedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        // Find next available senior user (simplified - can be enhanced with role/skill matching)
        var seniorUser = await _context.Users
            .Where(u => u.Status == UserStatus.Active && u.Id != ticket.AssignedToUserId)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (seniorUser != null)
        {
            var previousAssignee = ticket.AssignedToUserId;
            ticket.EscalatedToUserId = seniorUser.Id;
            ticket.AssignedToUserId = seniorUser.Id;

            await _context.SaveChangesAsync(cancellationToken);

            // Notify escalated user
            await _notificationService.SendNotificationAsync(new NotificationRequest
            {
                UserId = seniorUser.Id,
                Type = NotificationType.TicketEscalated,
                Title = "Ticket Escalated to You",
                Message = $"Ticket #{ticket.TicketNumber} escalated: {reason}",
                Priority = NotificationPriority.Urgent,
                RelatedEntityType = "Ticket",
                RelatedEntityId = ticket.Id
            }, cancellationToken);

            // Notify previous assignee
            if (previousAssignee.HasValue)
            {
                await _notificationService.SendNotificationAsync(new NotificationRequest
                {
                    UserId = previousAssignee.Value,
                    Type = NotificationType.TicketEscalated,
                    Title = "Ticket Escalated",
                    Message = $"Ticket #{ticket.TicketNumber} escalated to {seniorUser.FullName}",
                    Priority = NotificationPriority.Normal,
                    RelatedEntityType = "Ticket",
                    RelatedEntityId = ticket.Id
                }, cancellationToken);
            }

            _logger.LogInformation(
                "Ticket {TicketId} escalated to user {UserId}: {Reason}",
                ticket.Id, seniorUser.Id, reason);
        }
    }

    private (int firstResponseMinutes, int resolutionMinutes) GetSlaTargets(TicketPriority priority)
    {
        return priority switch
        {
            TicketPriority.Critical => (15, 240),   // 15min, 4h
            TicketPriority.High => (30, 480),       // 30min, 8h
            TicketPriority.Medium => (60, 1440),    // 1h, 24h
            TicketPriority.Low => (240, 2880),      // 4h, 48h
            _ => (60, 1440)
        };
    }
}
