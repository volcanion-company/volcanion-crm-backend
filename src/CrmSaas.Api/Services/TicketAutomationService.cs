using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Services;

public interface ITicketAutomationService
{
    Task AutoAssignTicketAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task AutoCategorizeTicketAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task ProcessEmailToTicketAsync(string from, string subject, string body, CancellationToken cancellationToken = default);
}

public class TicketAutomationService : ITicketAutomationService
{
    private readonly TenantDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TicketAutomationService> _logger;

    public TicketAutomationService(
        TenantDbContext context,
        INotificationService notificationService,
        ILogger<TicketAutomationService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task AutoAssignTicketAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        if (ticket.AssignedToUserId.HasValue)
        {
            _logger.LogDebug("Ticket {TicketId} already assigned", ticket.Id);
            return;
        }

        // Strategy 1: Round-robin by category
        var lastAssignedUser = await _context.Tickets
            .Where(t => t.Category == ticket.Category && t.AssignedToUserId != null)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => t.AssignedToUserId)
            .FirstOrDefaultAsync(cancellationToken);

        // Get available users (active users with support role)
        var availableUsers = await _context.Users
            .Where(u => u.Status == UserStatus.Active)
            .OrderBy(u => u.Id)
            .ToListAsync(cancellationToken);

        if (!availableUsers.Any())
        {
            _logger.LogWarning("No available users for ticket assignment");
            return;
        }

        Guid? selectedUserId = null;

        if (lastAssignedUser.HasValue)
        {
            // Find next user in round-robin
            var currentIndex = availableUsers.FindIndex(u => u.Id == lastAssignedUser.Value);
            var nextIndex = (currentIndex + 1) % availableUsers.Count;
            selectedUserId = availableUsers[nextIndex].Id;
        }
        else
        {
            // Strategy 2: Least workload
            var userWorkloads = await _context.Tickets
                .Where(t => t.Status != TicketStatus.Closed && 
                           t.Status != TicketStatus.Resolved &&
                           t.AssignedToUserId != null)
                .GroupBy(t => t.AssignedToUserId!.Value)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var userWithLeastWorkload = availableUsers
                .Select(u => new
                {
                    User = u,
                    Workload = userWorkloads.FirstOrDefault(w => w.UserId == u.Id)?.Count ?? 0
                })
                .OrderBy(x => x.Workload)
                .FirstOrDefault();

            selectedUserId = userWithLeastWorkload?.User.Id;
        }

        if (selectedUserId.HasValue)
        {
            ticket.AssignedToUserId = selectedUserId;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Send notification
            await _notificationService.SendNotificationAsync(new NotificationRequest
            {
                UserId = selectedUserId.Value,
                Type = NotificationType.TicketAssigned,
                Title = "New Ticket Assigned",
                Message = $"Ticket #{ticket.TicketNumber}: {ticket.Subject}",
                Priority = ticket.Priority == TicketPriority.Critical 
                    ? NotificationPriority.Urgent 
                    : NotificationPriority.Normal,
                RelatedEntityType = "Ticket",
                RelatedEntityId = ticket.Id
            }, cancellationToken);

            _logger.LogInformation(
                "Ticket {TicketId} auto-assigned to user {UserId}",
                ticket.Id, selectedUserId.Value);
        }
    }

    public async Task AutoCategorizeTicketAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(ticket.Category))
        {
            _logger.LogDebug("Ticket {TicketId} already categorized", ticket.Id);
            return;
        }

        // Simple keyword-based categorization
        var text = $"{ticket.Subject} {ticket.Description}".ToLowerInvariant();

        var category = DetectCategory(text);
        var subCategory = DetectSubCategory(text, category);

        if (!string.IsNullOrEmpty(category))
        {
            ticket.Category = category;
            ticket.SubCategory = subCategory;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Ticket {TicketId} auto-categorized as {Category}/{SubCategory}",
                ticket.Id, category, subCategory);
        }
    }

    public async Task ProcessEmailToTicketAsync(
        string from, 
        string subject, 
        string body, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find customer by email
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == from, cancellationToken);

            var contact = customer == null
                ? await _context.Contacts
                    .FirstOrDefaultAsync(c => c.Email == from, cancellationToken)
                : null;

            // Generate ticket number
            var ticketCount = await _context.Tickets.CountAsync(cancellationToken);
            var ticketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{ticketCount + 1:D4}";

            // Create ticket
            var ticket = new Ticket
            {
                TicketNumber = ticketNumber,
                Subject = subject,
                Description = body,
                CustomerId = customer?.Id,
                ContactId = contact?.Id,
                Status = TicketStatus.New,
                Priority = TicketPriority.Medium,
                Type = TicketType.Question,
                Channel = "Email",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync(cancellationToken);

            // Auto-categorize and auto-assign
            await AutoCategorizeTicketAsync(ticket, cancellationToken);
            await AutoAssignTicketAsync(ticket, cancellationToken);

            _logger.LogInformation(
                "Email from {From} converted to ticket {TicketNumber}",
                from, ticketNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email to ticket from {From}", from);
            throw;
        }
    }

    private string? DetectCategory(string text)
    {
        // Technical keywords
        if (text.Contains("bug") || text.Contains("error") || text.Contains("crash") ||
            text.Contains("not working") || text.Contains("broken"))
            return "Technical";

        // Billing keywords
        if (text.Contains("invoice") || text.Contains("payment") || text.Contains("billing") ||
            text.Contains("subscription") || text.Contains("refund"))
            return "Billing";

        // Account keywords
        if (text.Contains("account") || text.Contains("login") || text.Contains("password") ||
            text.Contains("access") || text.Contains("permission"))
            return "Account";

        // Feature request keywords
        if (text.Contains("feature") || text.Contains("enhancement") || text.Contains("suggestion") ||
            text.Contains("would like") || text.Contains("request"))
            return "Feature Request";

        // General inquiry
        if (text.Contains("how to") || text.Contains("question") || text.Contains("help"))
            return "General Inquiry";

        return "Other";
    }

    private string? DetectSubCategory(string text, string? category)
    {
        if (category == "Technical")
        {
            if (text.Contains("performance") || text.Contains("slow"))
                return "Performance";
            if (text.Contains("integration") || text.Contains("api"))
                return "Integration";
            if (text.Contains("data") || text.Contains("sync"))
                return "Data Issue";
            return "Bug";
        }

        if (category == "Billing")
        {
            if (text.Contains("invoice"))
                return "Invoice";
            if (text.Contains("payment"))
                return "Payment";
            if (text.Contains("refund"))
                return "Refund";
            return "General";
        }

        if (category == "Account")
        {
            if (text.Contains("login") || text.Contains("password"))
                return "Authentication";
            if (text.Contains("permission") || text.Contains("access"))
                return "Access Control";
            return "Account Management";
        }

        return null;
    }
}
