using CrmSaas.Api.Authorization;
using CrmSaas.Api.Common;
using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using CrmSaas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Controllers;

[Authorize]
public class TicketsController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly ISlaAutomationService? _slaAutomationService;
    private readonly ITicketAutomationService? _ticketAutomationService;

    public TicketsController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService,
        ISlaAutomationService? slaAutomationService = null,
        ITicketAutomationService? ticketAutomationService = null)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
        _slaAutomationService = slaAutomationService;
        _ticketAutomationService = ticketAutomationService;
    }

    [HttpGet]
    [RequirePermission(Permissions.TicketView)]
    public async Task<ActionResult<ApiResponse<PagedResult<TicketResponse>>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] Guid? assignedTo = null,
        [FromQuery] Guid? customerId = null)
    {
        var query = _db.Tickets
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.AssignedToUser)
            .Include(t => t.Sla)
            .WhereIf(status.HasValue, t => t.Status == status!.Value)
            .WhereIf(priority.HasValue, t => t.Priority == priority!.Value)
            .WhereIf(assignedTo.HasValue, t => t.AssignedToUserId == assignedTo)
            .WhereIf(customerId.HasValue, t => t.CustomerId == customerId)
            .WhereIf(!string.IsNullOrEmpty(pagination.Search), t =>
                t.TicketNumber.Contains(pagination.Search!) ||
                t.Subject.Contains(pagination.Search!))
            .ApplySorting(pagination.SortBy ?? "CreatedAt", pagination.SortDescending);

        var result = await query
            .Select(t => new TicketResponse
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Subject = t.Subject,
                CustomerName = t.Customer != null ? t.Customer.Name : null,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                Type = t.Type.ToString(),
                Channel = t.Channel,
                AssignedToUserName = t.AssignedToUser != null ? t.AssignedToUser.FullName : null,
                SlaName = t.Sla != null ? t.Sla.Name : null,
                DueDate = t.DueDate,
                SlaBreached = t.SlaBreached,
                CreatedAt = t.CreatedAt
            })
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    [HttpGet("my-tickets")]
    [RequirePermission(Permissions.TicketView)]
    public async Task<ActionResult<ApiResponse<PagedResult<TicketResponse>>>> GetMyTickets(
        [FromQuery] PaginationParams pagination,
        [FromQuery] TicketStatus? status = null)
    {
        var query = _db.Tickets
            .AsNoTracking()
            .Include(t => t.Customer)
            .Where(t => t.AssignedToUserId == _currentUser.UserId)
            .WhereIf(status.HasValue, t => t.Status == status!.Value)
            .ApplySorting(pagination.SortBy ?? "CreatedAt", pagination.SortDescending);

        var result = await query
            .Select(t => new TicketResponse
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Subject = t.Subject,
                CustomerName = t.Customer != null ? t.Customer.Name : null,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                Type = t.Type.ToString(),
                DueDate = t.DueDate,
                SlaBreached = t.SlaBreached,
                CreatedAt = t.CreatedAt
            })
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    [HttpGet("overdue")]
    [RequirePermission(Permissions.TicketView)]
    public async Task<ActionResult<ApiResponse<List<TicketResponse>>>> GetOverdueTickets()
    {
        var now = DateTime.UtcNow;
        
        var tickets = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.AssignedToUser)
            .Where(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved)
            .Where(t => t.DueDate < now)
            .OrderBy(t => t.DueDate)
            .Select(t => new TicketResponse
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Subject = t.Subject,
                CustomerName = t.Customer != null ? t.Customer.Name : null,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                AssignedToUserName = t.AssignedToUser != null ? t.AssignedToUser.FullName : null,
                DueDate = t.DueDate,
                SlaBreached = t.SlaBreached,
                CreatedAt = t.CreatedAt
            })
            .Take(50)
            .ToListAsync();

        return OkResponse(tickets);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.TicketView)]
    public async Task<ActionResult<ApiResponse<TicketDetailResponse>>> GetById(Guid id)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.Contact)
            .Include(t => t.AssignedToUser)
            .Include(t => t.Sla)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFoundResponse<TicketDetailResponse>($"Ticket with id {id} not found");
        }

        return OkResponse(MapToDetailResponse(ticket));
    }

    [HttpPost]
    [RequirePermission(Permissions.TicketCreate)]
    public async Task<ActionResult<ApiResponse<TicketResponse>>> Create([FromBody] CreateTicketRequest request)
    {
        var ticketNumber = await GenerateTicketNumber();

        var ticket = new Ticket
        {
            TicketNumber = ticketNumber,
            Subject = request.Subject,
            Description = request.Description,
            CustomerId = request.CustomerId,
            ContactId = request.ContactId,
            Type = request.Type,
            Priority = request.Priority,
            Channel = request.Channel,
            SlaId = request.SlaId,
            AssignedToUserId = request.AssignedToUserId,
            Tags = request.Tags,
            DueDate = request.DueDate,
            CreatedBy = _currentUser.UserId
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        // Initialize SLA tracking
        if (_slaAutomationService != null)
        {
            await _slaAutomationService.InitializeSlaForTicketAsync(ticket);
        }

        // Auto-categorize and auto-assign if not manually assigned
        if (_ticketAutomationService != null)
        {
            await _ticketAutomationService.AutoCategorizeTicketAsync(ticket);
            
            if (!ticket.AssignedToUserId.HasValue)
            {
                await _ticketAutomationService.AutoAssignTicketAsync(ticket);
            }
        }

        await _auditService.LogAsync(AuditActions.Create, nameof(Ticket), ticket.Id, ticket.TicketNumber);

        return CreatedResponse(new TicketResponse
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Subject = ticket.Subject,
            Status = ticket.Status.ToString(),
            Priority = ticket.Priority.ToString(),
            CreatedAt = ticket.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.TicketUpdate)]
    public async Task<ActionResult<ApiResponse<TicketResponse>>> Update(Guid id, [FromBody] UpdateTicketRequest request)
    {
        var ticket = await _db.Tickets.FindAsync(id);

        if (ticket == null)
        {
            return NotFoundResponse<TicketResponse>($"Ticket with id {id} not found");
        }

        ticket.Subject = request.Subject ?? ticket.Subject;
        ticket.Description = request.Description ?? ticket.Description;
        ticket.Priority = request.Priority ?? ticket.Priority;
        ticket.Type = request.Type ?? ticket.Type;
        ticket.Tags = request.Tags ?? ticket.Tags;
        ticket.DueDate = request.DueDate ?? ticket.DueDate;
        ticket.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Ticket), ticket.Id, ticket.TicketNumber);

        return OkResponse(new TicketResponse
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Subject = ticket.Subject,
            Status = ticket.Status.ToString(),
            Priority = ticket.Priority.ToString(),
            CreatedAt = ticket.CreatedAt
        });
    }

    [HttpPost("{id:guid}/assign")]
    [RequirePermission(Permissions.TicketAssign)]
    public async Task<ActionResult<ApiResponse>> Assign(Guid id, [FromBody] AssignTicketRequest request)
    {
        var ticket = await _db.Tickets.FindAsync(id);

        if (ticket == null)
        {
            return NotFoundResponse($"Ticket with id {id} not found");
        }

        ticket.AssignedToUserId = request.UserId;
        ticket.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Assign, nameof(Ticket), ticket.Id, ticket.TicketNumber);

        return OkResponse("Ticket assigned successfully");
    }

    [HttpPost("{id:guid}/resolve")]
    [RequirePermission(Permissions.TicketUpdate)]
    public async Task<ActionResult<ApiResponse>> Resolve(Guid id)
    {
        var ticket = await _db.Tickets.FindAsync(id);

        if (ticket == null)
        {
            return NotFoundResponse($"Ticket with id {id} not found");
        }

        ticket.Status = TicketStatus.Resolved;
        ticket.ResolvedDate = DateTime.UtcNow;
        ticket.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Ticket), ticket.Id, ticket.TicketNumber);

        return OkResponse("Ticket resolved");
    }

    [HttpPost("{id:guid}/close")]
    [RequirePermission(Permissions.TicketUpdate)]
    public async Task<ActionResult<ApiResponse>> Close(Guid id)
    {
        var ticket = await _db.Tickets.FindAsync(id);

        if (ticket == null)
        {
            return NotFoundResponse($"Ticket with id {id} not found");
        }

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedDate = DateTime.UtcNow;
        ticket.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Ticket), ticket.Id, ticket.TicketNumber);

        return OkResponse("Ticket closed");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.TicketDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var ticket = await _db.Tickets.FindAsync(id);

        if (ticket == null)
        {
            return NotFoundResponse($"Ticket with id {id} not found");
        }

        ticket.IsDeleted = true;
        ticket.DeletedAt = DateTime.UtcNow;
        ticket.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.SoftDelete, nameof(Ticket), ticket.Id, ticket.TicketNumber);

        return OkResponse("Ticket deleted");
    }

    private async Task<string> GenerateTicketNumber()
    {
        var count = await _db.Tickets.CountAsync() + 1;
        return $"TKT-{count:D6}";
    }

    [HttpPost("{id}/pause-sla")]
    [RequirePermission(Permissions.TicketUpdate)]
    public async Task<ActionResult<ApiResponse<object>>> PauseSla(Guid id, [FromBody] PauseSlaRequest request)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFoundResponse<object>($"Ticket with id {id} not found");
        }

        if (ticket.SlaPaused)
        {
            return BadRequestResponse<object>("SLA is already paused");
        }

        ticket.SlaPaused = true;
        ticket.SlaPausedAt = DateTime.UtcNow;
        ticket.SlaPauseReason = request.Reason;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.Update, nameof(Ticket), ticket.Id, $"SLA paused: {request.Reason}");

        return OkResponse((object)new { message = "SLA paused successfully" });
    }

    [HttpPost("{id}/resume-sla")]
    [RequirePermission(Permissions.TicketUpdate)]
    public async Task<ActionResult<ApiResponse<object>>> ResumeSla(Guid id)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFoundResponse<object>($"Ticket with id {id} not found");
        }

        if (!ticket.SlaPaused)
        {
            return BadRequestResponse<object>("SLA is not paused");
        }

        if (ticket.SlaPausedAt.HasValue)
        {
            var pauseDuration = (int)(DateTime.UtcNow - ticket.SlaPausedAt.Value).TotalMinutes;
            ticket.SlaPausedMinutes = ticket.SlaPausedMinutes + pauseDuration;
        }

        ticket.SlaPaused = false;
        ticket.SlaPausedAt = null;
        ticket.SlaPauseReason = null;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.Update, nameof(Ticket), ticket.Id, "SLA resumed");

        return OkResponse((object)new { message = "SLA resumed successfully", totalPausedMinutes = ticket.SlaPausedMinutes });
    }

    [HttpPost("{id}/escalate")]
    [RequirePermission(Permissions.TicketUpdate)]
    public async Task<ActionResult<ApiResponse<object>>> Escalate(Guid id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFoundResponse<object>($"Ticket with id {id} not found");
        }

        // Find senior user for escalation
        var seniorUser = await _db.Users
            .Where(u => u.Status == UserStatus.Active && u.Id != ticket.AssignedToUserId)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync();

        if (seniorUser == null)
        {
            return BadRequestResponse<object>("No available users for escalation");
        }

        // Increase priority
        if (ticket.Priority == TicketPriority.Low)
            ticket.Priority = TicketPriority.Medium;
        else if (ticket.Priority == TicketPriority.Medium)
            ticket.Priority = TicketPriority.High;
        else if (ticket.Priority == TicketPriority.High)
            ticket.Priority = TicketPriority.Critical;

        ticket.EscalationCount = ticket.EscalationCount + 1;
        ticket.LastEscalatedAt = DateTime.UtcNow;
        ticket.EscalatedToUserId = seniorUser.Id;
        ticket.AssignedToUserId = seniorUser.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _auditService.LogAsync(AuditActions.Update, nameof(Ticket), ticket.Id, $"Escalated to {seniorUser.FullName}");

        return OkResponse((object)new
        {
            message = "Ticket escalated successfully",
            escalatedTo = seniorUser.FullName,
            newPriority = ticket.Priority.ToString(),
            escalationCount = ticket.EscalationCount
        });
    }

    private static TicketDetailResponse MapToDetailResponse(Ticket ticket)
    {
        return new TicketDetailResponse
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Subject = ticket.Subject,
            Description = ticket.Description,
            CustomerName = ticket.Customer?.Name,
            ContactName = ticket.Contact != null ? $"{ticket.Contact.FirstName} {ticket.Contact.LastName}" : null,
            Status = ticket.Status.ToString(),
            Priority = ticket.Priority.ToString(),
            Type = ticket.Type.ToString(),
            Channel = ticket.Channel,
            Category = ticket.Category,
            SubCategory = ticket.SubCategory,
            AssignedToUserName = ticket.AssignedToUser?.FullName,
            SlaName = ticket.Sla?.Name,
            DueDate = ticket.DueDate,
            FirstResponseDate = ticket.FirstResponseDate,
            ResolvedDate = ticket.ResolvedDate,
            ClosedDate = ticket.ClosedDate,
            SatisfactionRating = ticket.SatisfactionRating,
            SatisfactionComment = ticket.SatisfactionComment,
            SlaBreached = ticket.SlaBreached,
            SlaPaused = ticket.SlaPaused,
            SlaPausedMinutes = ticket.SlaPausedMinutes,
            EscalationCount = ticket.EscalationCount,
            Tags = ticket.Tags,
            CreatedAt = ticket.CreatedAt
        };
    }
}

// Request/Response DTOs
public class CreateTicketRequest
{
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ContactId { get; set; }
    public TicketType Type { get; set; } = TicketType.Question;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public string? Channel { get; set; }
    public Guid? SlaId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Tags { get; set; }
    public DateTime? DueDate { get; set; }
}

public class UpdateTicketRequest
{
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public TicketPriority? Priority { get; set; }
    public TicketType? Type { get; set; }
    public string? Tags { get; set; }
    public DateTime? DueDate { get; set; }
}

public class AssignTicketRequest
{
    public Guid UserId { get; set; }
}

public class PauseSlaRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class TicketResponse
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? SlaName { get; set; }
    public DateTime? DueDate { get; set; }
    public bool SlaBreached { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TicketDetailResponse : TicketResponse
{
    public string? Description { get; set; }
    public string? ContactName { get; set; }
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public DateTime? FirstResponseDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public int? SatisfactionRating { get; set; }
    public string? SatisfactionComment { get; set; }
    public string? Tags { get; set; }
    public bool SlaPaused { get; set; }
    public int? SlaPausedMinutes { get; set; }
    public int EscalationCount { get; set; }
}
