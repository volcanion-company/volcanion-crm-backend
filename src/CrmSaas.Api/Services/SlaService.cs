using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Services;

public interface ISlaService
{
    Task<SlaStatus> CalculateSlaStatusAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task PauseSlaAsync(Guid ticketId, string reason, CancellationToken cancellationToken = default);
    Task ResumeSlaAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<bool> CheckSlaBreachAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task EscalateTicketAsync(Guid ticketId, Guid? escalateToUserId = null, CancellationToken cancellationToken = default);
}

public class SlaService : ISlaService
{
    private readonly TenantDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SlaService> _logger;

    public SlaService(
        TenantDbContext context,
        INotificationService notificationService,
        ILogger<SlaService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<SlaStatus> CalculateSlaStatusAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        if (ticket.Sla == null)
        {
            return new SlaStatus
            {
                HasSla = false,
                Message = "No SLA assigned"
            };
        }

        var now = DateTime.UtcNow;
        
        // Calculate first response time
        var firstResponseTarget = GetFirstResponseTarget(ticket.Sla, ticket.Priority);
        var firstResponseElapsed = ticket.FirstResponseDate.HasValue
            ? (ticket.FirstResponseDate.Value - ticket.CreatedAt).TotalMinutes
            : (now - ticket.CreatedAt).TotalMinutes - ticket.SlaPausedMinutes;

        var firstResponseRemaining = firstResponseTarget - firstResponseElapsed;
        var firstResponseBreached = !ticket.FirstResponseDate.HasValue && firstResponseRemaining < 0;

        // Calculate resolution time
        var resolutionTarget = GetResolutionTarget(ticket.Sla, ticket.Priority);
        var resolutionElapsed = ticket.ResolvedDate.HasValue
            ? (ticket.ResolvedDate.Value - ticket.CreatedAt).TotalMinutes
            : (now - ticket.CreatedAt).TotalMinutes - ticket.SlaPausedMinutes;

        var resolutionRemaining = resolutionTarget - resolutionElapsed;
        var resolutionBreached = ticket.Status != TicketStatus.Resolved && 
                                 ticket.Status != TicketStatus.Closed && 
                                 resolutionRemaining < 0;

        return new SlaStatus
        {
            HasSla = true,
            IsPaused = ticket.SlaPaused,
            FirstResponseTargetMinutes = firstResponseTarget,
            FirstResponseElapsedMinutes = firstResponseElapsed,
            FirstResponseRemainingMinutes = firstResponseRemaining,
            FirstResponseBreached = firstResponseBreached,
            ResolutionTargetMinutes = resolutionTarget,
            ResolutionElapsedMinutes = resolutionElapsed,
            ResolutionRemainingMinutes = resolutionRemaining,
            ResolutionBreached = resolutionBreached,
            OverallBreached = firstResponseBreached || resolutionBreached,
            WarningThreshold = resolutionRemaining <= 30 && resolutionRemaining > 0,
            Message = GetSlaMessage(firstResponseBreached, resolutionBreached, firstResponseRemaining, resolutionRemaining)
        };
    }

    public async Task PauseSlaAsync(Guid ticketId, string reason, CancellationToken cancellationToken = default)
    {
        var ticket = await _context.Tickets.FindAsync(new object[] { ticketId }, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException("Ticket not found");
        }

        if (ticket.SlaPaused)
        {
            _logger.LogWarning("SLA for ticket {TicketId} is already paused", ticketId);
            return;
        }

        ticket.SlaPaused = true;
        ticket.SlaPausedAt = DateTime.UtcNow;
        ticket.SlaPauseReason = reason;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SLA paused for ticket {TicketId}: {Reason}", ticketId, reason);
    }

    public async Task ResumeSlaAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _context.Tickets.FindAsync(new object[] { ticketId }, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException("Ticket not found");
        }

        if (!ticket.SlaPaused)
        {
            _logger.LogWarning("SLA for ticket {TicketId} is not paused", ticketId);
            return;
        }

        if (ticket.SlaPausedAt.HasValue)
        {
            var pausedDuration = (DateTime.UtcNow - ticket.SlaPausedAt.Value).TotalMinutes;
            ticket.SlaPausedMinutes += (int)pausedDuration;
        }

        ticket.SlaPaused = false;
        ticket.SlaPausedAt = null;
        ticket.SlaPauseReason = null;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SLA resumed for ticket {TicketId}", ticketId);
    }

    public async Task<bool> CheckSlaBreachAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        var status = await CalculateSlaStatusAsync(ticket, cancellationToken);
        
        if (status.OverallBreached && !ticket.SlaBreached)
        {
            ticket.SlaBreached = true;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Send breach notification
            if (ticket.AssignedToUserId.HasValue)
            {
                await _notificationService.SendNotificationAsync(new NotificationRequest
                {
                    UserId = ticket.AssignedToUserId.Value,
                    Type = NotificationType.SlaBreached,
                    Title = "SLA Breached",
                    Message = $"Ticket #{ticket.TicketNumber} has breached SLA",
                    Priority = NotificationPriority.Urgent,
                    RelatedEntityType = "Ticket",
                    RelatedEntityId = ticket.Id
                }, cancellationToken);
            }

            _logger.LogWarning("SLA breached for ticket {TicketId}", ticket.Id);
            return true;
        }

        return false;
    }

    public async Task EscalateTicketAsync(Guid ticketId, Guid? escalateToUserId = null, CancellationToken cancellationToken = default)
    {
        var ticket = await _context.Tickets
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket == null)
        {
            throw new InvalidOperationException("Ticket not found");
        }

        ticket.EscalationCount++;
        ticket.LastEscalatedAt = DateTime.UtcNow;
        ticket.Priority = ticket.Priority < TicketPriority.Critical 
            ? ticket.Priority + 1 
            : TicketPriority.Critical;

        if (escalateToUserId.HasValue)
        {
            ticket.EscalatedToUserId = escalateToUserId;
            ticket.AssignedToUserId = escalateToUserId;

            // Notify escalated user
            await _notificationService.SendNotificationAsync(new NotificationRequest
            {
                UserId = escalateToUserId.Value,
                Type = NotificationType.TicketAssigned,
                Title = "Escalated Ticket Assigned",
                Message = $"High priority ticket #{ticket.TicketNumber} has been escalated to you",
                Priority = NotificationPriority.Urgent,
                RelatedEntityType = "Ticket",
                RelatedEntityId = ticket.Id
            }, cancellationToken);
        }

        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Ticket {TicketId} escalated (count: {Count}) to user {UserId}",
            ticketId, ticket.EscalationCount, escalateToUserId);
    }

    private int GetFirstResponseTarget(Sla sla, TicketPriority priority)
    {
        return priority switch
        {
            TicketPriority.Low => sla.FirstResponseTimeLow,
            TicketPriority.Medium => sla.FirstResponseTimeMedium,
            TicketPriority.High => sla.FirstResponseTimeHigh,
            TicketPriority.Critical => sla.FirstResponseTimeCritical,
            _ => sla.FirstResponseTimeMedium
        };
    }

    private int GetResolutionTarget(Sla sla, TicketPriority priority)
    {
        return priority switch
        {
            TicketPriority.Low => sla.ResolutionTimeLow,
            TicketPriority.Medium => sla.ResolutionTimeMedium,
            TicketPriority.High => sla.ResolutionTimeHigh,
            TicketPriority.Critical => sla.ResolutionTimeCritical,
            _ => sla.ResolutionTimeMedium
        };
    }

    private string GetSlaMessage(bool firstResponseBreached, bool resolutionBreached, 
        double firstResponseRemaining, double resolutionRemaining)
    {
        if (firstResponseBreached)
            return "First response SLA breached";
        if (resolutionBreached)
            return "Resolution SLA breached";
        if (resolutionRemaining <= 30)
            return $"Warning: {resolutionRemaining:F0} minutes until SLA breach";
        if (firstResponseRemaining <= 30)
            return $"Warning: {firstResponseRemaining:F0} minutes until first response SLA breach";
        
        return "SLA on track";
    }
}

public class SlaStatus
{
    public bool HasSla { get; set; }
    public bool IsPaused { get; set; }
    public double FirstResponseTargetMinutes { get; set; }
    public double FirstResponseElapsedMinutes { get; set; }
    public double FirstResponseRemainingMinutes { get; set; }
    public bool FirstResponseBreached { get; set; }
    public double ResolutionTargetMinutes { get; set; }
    public double ResolutionElapsedMinutes { get; set; }
    public double ResolutionRemainingMinutes { get; set; }
    public bool ResolutionBreached { get; set; }
    public bool OverallBreached { get; set; }
    public bool WarningThreshold { get; set; }
    public string Message { get; set; } = string.Empty;
}
