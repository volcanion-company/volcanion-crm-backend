using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Services;

public interface ICustomer360Service
{
    Task<Customer360View> GetCustomer360ViewAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<CustomerHealthScore> CalculateHealthScoreAsync(Guid customerId, CancellationToken cancellationToken = default);
}

public class Customer360Service : ICustomer360Service
{
    private readonly TenantDbContext _context;
    private readonly ILogger<Customer360Service> _logger;

    public Customer360Service(
        TenantDbContext context,
        ILogger<Customer360Service> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Customer360View> GetCustomer360ViewAsync(
        Guid customerId, 
        CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer == null)
            throw new InvalidOperationException($"Customer {customerId} not found");

        // Get interactions timeline
        var interactions = await _context.Interactions
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.InteractionDate)
            .Take(50)
            .Select(i => new TimelineEvent
            {
                Id = i.Id,
                Type = "Interaction",
                Title = i.Type.ToString(),
                Description = i.Description,
                Date = i.InteractionDate,
                RelatedTo = i.ContactId.HasValue ? "Contact" : null,
                RelatedId = i.ContactId
            })
            .ToListAsync(cancellationToken);

        // Get opportunities
        var opportunities = await _context.Opportunities
            .Include(o => o.Stage)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(20)
            .Select(o => new OpportunitySummary
            {
                Id = o.Id,
                Name = o.Name,
                Stage = o.Stage != null ? o.Stage.Name : "Unknown",
                Amount = o.Amount,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.ActualCloseDate.HasValue ? "Closed" : "Open",
                IsWon = o.ActualCloseDate.HasValue && o.Amount > 0
            })
            .ToListAsync(cancellationToken);

        // Get tickets
        var tickets = await _context.Tickets
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .Select(t => new TicketSummary
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Subject = t.Subject,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                CreatedAt = t.CreatedAt,
                ResolvedDate = t.ResolvedDate,
                SlaBreached = t.SlaBreached
            })
            .ToListAsync(cancellationToken);

        // Get activities
        var activities = await _context.Activities
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.DueDate)
            .Take(20)
            .Select(a => new ActivitySummary
            {
                Id = a.Id,
                Subject = a.Subject,
                Type = a.Type.ToString(),
                Status = a.Status.ToString(),
                DueDate = a.DueDate,
                CompletedDate = a.CompletedDate
            })
            .ToListAsync(cancellationToken);

        // Get contracts
        var contracts = await _context.Contracts
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new ContractSummary
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Value = c.Value,
                Status = c.Status.ToString(),
                IsActive = c.EndDate >= DateTime.UtcNow
            })
            .ToListAsync(cancellationToken);

        // Calculate metrics
        var totalRevenue = opportunities.Where(o => o.IsWon).Sum(o => o.Amount);
        var openOpportunitiesValue = opportunities.Where(o => o.Status == "Open").Sum(o => o.Amount);
        var openTicketsCount = tickets.Count(t => t.Status != "Closed" && t.Status != "Resolved");
        var slaBreachCount = tickets.Count(t => t.SlaBreached);
        var activeContractsCount = contracts.Count(c => c.IsActive);

        // Build timeline from all events
        var timeline = interactions
            .Concat(opportunities.Select(o => new TimelineEvent
            {
                Id = o.Id,
                Type = "Opportunity",
                Title = o.Name,
                Description = $"{o.Stage} - {o.Amount:C}",
                Date = o.ExpectedCloseDate ?? DateTime.UtcNow,
                RelatedTo = "Opportunity"
            }))
            .Concat(tickets.Select(t => new TimelineEvent
            {
                Id = t.Id,
                Type = "Ticket",
                Title = t.Subject,
                Description = $"{t.Status} - {t.Priority}",
                Date = t.CreatedAt,
                RelatedTo = "Ticket"
            }))
            .Concat(activities.Select(a => new TimelineEvent
            {
                Id = a.Id,
                Type = "Activity",
                Title = a.Subject,
                Description = $"{a.Type} - {a.Status}",
                Date = a.DueDate ?? DateTime.UtcNow,
                RelatedTo = "Activity"
            }))
            .OrderByDescending(e => e.Date)
            .Take(100)
            .ToList();

        // Calculate health score
        var healthScore = await CalculateHealthScoreAsync(customerId, cancellationToken);

        return new Customer360View
        {
            Customer = new CustomerBasicInfo
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Type = customer.Type.ToString(),
                Status = customer.Status.ToString(),
                CreatedAt = customer.CreatedAt,
                AssignedToUser = customer.AssignedToUser?.FullName
            },
            Contacts = customer.Contacts.Select(c => new ContactBasicInfo
            {
                Id = c.Id,
                FullName = $"{c.FirstName} {c.LastName}",
                Email = c.Email,
                Phone = c.Phone,
                JobTitle = c.JobTitle,
                IsPrimary = c.IsPrimary
            }).ToList(),
            Metrics = new CustomerMetrics
            {
                TotalRevenue = totalRevenue,
                OpenOpportunitiesValue = openOpportunitiesValue,
                OpportunitiesCount = opportunities.Count,
                OpenTicketsCount = openTicketsCount,
                SlaBreachCount = slaBreachCount,
                ActiveContractsCount = activeContractsCount,
                InteractionsCount = interactions.Count,
                ActivitiesCount = activities.Count
            },
            Timeline = timeline,
            Opportunities = opportunities,
            Tickets = tickets,
            Activities = activities,
            Contracts = contracts,
            HealthScore = healthScore
        };
    }

    public async Task<CustomerHealthScore> CalculateHealthScoreAsync(
        Guid customerId, 
        CancellationToken cancellationToken = default)
    {
        var score = 100; // Start with perfect score
        var factors = new List<string>();

        // Factor 1: Open tickets (deduct points)
        var openTickets = await _context.Tickets
            .CountAsync(t => t.CustomerId == customerId && 
                           t.Status != TicketStatus.Closed && 
                           t.Status != TicketStatus.Resolved, 
                       cancellationToken);

        if (openTickets > 5)
        {
            score -= 20;
            factors.Add($"High open tickets ({openTickets})");
        }
        else if (openTickets > 2)
        {
            score -= 10;
            factors.Add($"Moderate open tickets ({openTickets})");
        }

        // Factor 2: SLA breaches (deduct points)
        var slaBreaches = await _context.Tickets
            .CountAsync(t => t.CustomerId == customerId && t.SlaBreached, cancellationToken);

        if (slaBreaches > 3)
        {
            score -= 15;
            factors.Add($"Multiple SLA breaches ({slaBreaches})");
        }
        else if (slaBreaches > 0)
        {
            score -= 5;
            factors.Add($"Some SLA breaches ({slaBreaches})");
        }

        // Factor 3: Recent interactions (add points)
        var recentInteractions = await _context.Interactions
            .CountAsync(i => i.CustomerId == customerId && 
                           i.InteractionDate >= DateTime.UtcNow.AddDays(-30), 
                       cancellationToken);

        if (recentInteractions == 0)
        {
            score -= 15;
            factors.Add("No recent interactions");
        }
        else if (recentInteractions >= 5)
        {
            score += 10;
            factors.Add($"Highly engaged ({recentInteractions} interactions)");
        }

        // Factor 4: Active contracts
        var activeContracts = await _context.Contracts
            .CountAsync(c => c.CustomerId == customerId && 
                           c.EndDate >= DateTime.UtcNow, 
                       cancellationToken);

        if (activeContracts == 0)
        {
            score -= 10;
            factors.Add("No active contracts");
        }
        else if (activeContracts >= 2)
        {
            score += 15;
            factors.Add($"Multiple contracts ({activeContracts})");
        }

        // Factor 5: Contract expiring soon
        var expiringContracts = await _context.Contracts
            .CountAsync(c => c.CustomerId == customerId && 
                           c.EndDate >= DateTime.UtcNow &&
                           c.EndDate <= DateTime.UtcNow.AddDays(30), 
                       cancellationToken);

        if (expiringContracts > 0)
        {
            score -= 10;
            factors.Add($"Contracts expiring soon ({expiringContracts})");
        }

        // Factor 6: Won opportunities
        var wonOpportunities = await _context.Opportunities
            .CountAsync(o => o.CustomerId == customerId && o.ActualCloseDate.HasValue && o.Amount > 0, cancellationToken);

        if (wonOpportunities >= 3)
        {
            score += 10;
            factors.Add($"Strong sales history ({wonOpportunities} won)");
        }

        // Clamp score between 0-100
        score = Math.Max(0, Math.Min(100, score));

        var status = score >= 80 ? "Excellent" :
                    score >= 60 ? "Good" :
                    score >= 40 ? "Fair" :
                    score >= 20 ? "Poor" : "Critical";

        return new CustomerHealthScore
        {
            Score = score,
            Status = status,
            Factors = factors,
            CalculatedAt = DateTime.UtcNow
        };
    }
}

public class Customer360View
{
    public CustomerBasicInfo Customer { get; set; } = new();
    public List<ContactBasicInfo> Contacts { get; set; } = new();
    public CustomerMetrics Metrics { get; set; } = new();
    public List<TimelineEvent> Timeline { get; set; } = new();
    public List<OpportunitySummary> Opportunities { get; set; } = new();
    public List<TicketSummary> Tickets { get; set; } = new();
    public List<ActivitySummary> Activities { get; set; } = new();
    public List<ContractSummary> Contracts { get; set; } = new();
    public CustomerHealthScore HealthScore { get; set; } = new();
}

public class CustomerBasicInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? AssignedToUser { get; set; }
}

public class ContactBasicInfo
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public bool IsPrimary { get; set; }
}

public class CustomerMetrics
{
    public decimal TotalRevenue { get; set; }
    public decimal OpenOpportunitiesValue { get; set; }
    public int OpportunitiesCount { get; set; }
    public int OpenTicketsCount { get; set; }
    public int SlaBreachCount { get; set; }
    public int ActiveContractsCount { get; set; }
    public int InteractionsCount { get; set; }
    public int ActivitiesCount { get; set; }
}

public class TimelineEvent
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string? RelatedTo { get; set; }
    public Guid? RelatedId { get; set; }
}

public class OpportunitySummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Probability { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsWon { get; set; }
}

public class TicketSummary
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public bool SlaBreached { get; set; }
}

public class ActivitySummary
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}

public class ContractSummary
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Value { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CustomerHealthScore
{
    public int Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Factors { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
}
