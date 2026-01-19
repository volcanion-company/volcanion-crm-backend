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
public class ActivitiesController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public ActivitiesController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [HttpGet]
    [RequirePermission(Permissions.ActivityView)]
    public async Task<ActionResult<ApiResponse<PagedResult<ActivityResponse>>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] ActivityType? type = null,
        [FromQuery] ActivityStatus? status = null,
        [FromQuery] Guid? assignedTo = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = _db.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.AssignedToUser)
            .WhereIf(type.HasValue, a => a.Type == type!.Value)
            .WhereIf(status.HasValue, a => a.Status == status!.Value)
            .WhereIf(assignedTo.HasValue, a => a.AssignedToUserId == assignedTo)
            .WhereIf(customerId.HasValue, a => a.CustomerId == customerId)
            .WhereIf(startDate.HasValue, a => a.DueDate >= startDate)
            .WhereIf(endDate.HasValue, a => a.DueDate <= endDate)
            .WhereIf(!string.IsNullOrEmpty(pagination.Search), a =>
                a.Subject.Contains(pagination.Search!))
            .ApplySorting(pagination.SortBy ?? "DueDate", pagination.SortDescending);

        var now = DateTime.UtcNow;
        var result = await query
            .Select(a => new ActivityResponse
            {
                Id = a.Id,
                Subject = a.Subject,
                Type = a.Type.ToString(),
                Status = a.Status.ToString(),
                Priority = a.Priority.ToString(),
                CustomerName = a.Customer != null ? a.Customer.Name : null,
                AssignedToUserName = a.AssignedToUser != null ? a.AssignedToUser.FullName : null,
                StartDate = a.StartDate,
                DueDate = a.DueDate,
                DurationMinutes = a.DurationMinutes,
                IsOverdue = a.DueDate.HasValue && a.DueDate < now && a.Status != ActivityStatus.Completed,
                CreatedAt = a.CreatedAt
            })
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    [HttpGet("my-activities")]
    [RequirePermission(Permissions.ActivityView)]
    public async Task<ActionResult<ApiResponse<PagedResult<ActivityResponse>>>> GetMyActivities(
        [FromQuery] PaginationParams pagination,
        [FromQuery] ActivityStatus? status = null,
        [FromQuery] bool? overdue = null)
    {
        var now = DateTime.UtcNow;

        var query = _db.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Where(a => a.AssignedToUserId == _currentUser.UserId)
            .WhereIf(status.HasValue, a => a.Status == status!.Value)
            .WhereIf(overdue == true, a => a.DueDate < now && a.Status != ActivityStatus.Completed)
            .ApplySorting(pagination.SortBy ?? "DueDate", false);

        var result = await query
            .Select(a => new ActivityResponse
            {
                Id = a.Id,
                Subject = a.Subject,
                Type = a.Type.ToString(),
                Status = a.Status.ToString(),
                Priority = a.Priority.ToString(),
                CustomerName = a.Customer != null ? a.Customer.Name : null,
                StartDate = a.StartDate,
                DueDate = a.DueDate,
                DurationMinutes = a.DurationMinutes,
                IsOverdue = a.DueDate.HasValue && a.DueDate < now && a.Status != ActivityStatus.Completed,
                CreatedAt = a.CreatedAt
            })
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    [HttpGet("today")]
    [RequirePermission(Permissions.ActivityView)]
    public async Task<ActionResult<ApiResponse<List<ActivityResponse>>>> GetTodayActivities()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var now = DateTime.UtcNow;

        var activities = await _db.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Where(a => a.AssignedToUserId == _currentUser.UserId)
            .Where(a => a.DueDate >= today && a.DueDate < tomorrow)
            .OrderBy(a => a.StartDate ?? a.DueDate)
            .Select(a => new ActivityResponse
            {
                Id = a.Id,
                Subject = a.Subject,
                Type = a.Type.ToString(),
                Status = a.Status.ToString(),
                Priority = a.Priority.ToString(),
                CustomerName = a.Customer != null ? a.Customer.Name : null,
                StartDate = a.StartDate,
                DueDate = a.DueDate,
                DurationMinutes = a.DurationMinutes,
                IsOverdue = a.DueDate.HasValue && a.DueDate < now && a.Status != ActivityStatus.Completed,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return OkResponse(activities);
    }

    [HttpGet("upcoming")]
    [RequirePermission(Permissions.ActivityView)]
    public async Task<ActionResult<ApiResponse<List<ActivityResponse>>>> GetUpcoming([FromQuery] int days = 7)
    {
        var now = DateTime.UtcNow;
        var endDate = now.AddDays(days);

        var activities = await _db.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Where(a => a.AssignedToUserId == _currentUser.UserId)
            .Where(a => a.DueDate >= now && a.DueDate <= endDate)
            .Where(a => a.Status != ActivityStatus.Completed && a.Status != ActivityStatus.Cancelled)
            .OrderBy(a => a.DueDate)
            .Select(a => new ActivityResponse
            {
                Id = a.Id,
                Subject = a.Subject,
                Type = a.Type.ToString(),
                Status = a.Status.ToString(),
                Priority = a.Priority.ToString(),
                CustomerName = a.Customer != null ? a.Customer.Name : null,
                StartDate = a.StartDate,
                DueDate = a.DueDate,
                DurationMinutes = a.DurationMinutes,
                IsOverdue = false,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return OkResponse(activities);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.ActivityView)]
    public async Task<ActionResult<ApiResponse<ActivityDetailResponse>>> GetById(Guid id)
    {
        var now = DateTime.UtcNow;
        var activity = await _db.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Contact)
            .Include(a => a.Lead)
            .Include(a => a.Opportunity)
            .Include(a => a.Ticket)
            .Include(a => a.AssignedToUser)
            .Include(a => a.Reminders)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (activity == null)
        {
            return NotFoundResponse<ActivityDetailResponse>($"Activity with id {id} not found");
        }

        return OkResponse(new ActivityDetailResponse
        {
            Id = activity.Id,
            Subject = activity.Subject,
            Description = activity.Description,
            Type = activity.Type.ToString(),
            Status = activity.Status.ToString(),
            Priority = activity.Priority.ToString(),
            CustomerId = activity.CustomerId,
            CustomerName = activity.Customer?.Name,
            ContactId = activity.ContactId,
            ContactName = activity.Contact != null ? $"{activity.Contact.FirstName} {activity.Contact.LastName}" : null,
            LeadId = activity.LeadId,
            LeadName = activity.Lead?.CompanyName,
            OpportunityId = activity.OpportunityId,
            OpportunityName = activity.Opportunity?.Name,
            TicketId = activity.TicketId,
            TicketNumber = activity.Ticket?.TicketNumber,
            AssignedToUserId = activity.AssignedToUserId,
            AssignedToUserName = activity.AssignedToUser?.FullName,
            StartDate = activity.StartDate,
            DueDate = activity.DueDate,
            DurationMinutes = activity.DurationMinutes,
            CompletedDate = activity.CompletedDate,
            IsOverdue = activity.DueDate.HasValue && activity.DueDate < now && activity.Status != ActivityStatus.Completed,
            IsRecurring = activity.IsRecurring,
            RecurrencePattern = activity.RecurrencePattern?.ToString(),
            RecurrenceInterval = activity.RecurrenceInterval,
            RecurrenceEndDate = activity.RecurrenceEndDate,
            Tags = activity.Tags,
            Reminders = activity.Reminders.Select(r => new ReminderResponse
            {
                Id = r.Id,
                Subject = r.Subject,
                Message = r.Message,
                ReminderDate = r.ReminderDate,
                Method = r.Method.ToString(),
                IsSent = r.IsSent
            }).ToList(),
            CreatedAt = activity.CreatedAt,
            UpdatedAt = activity.UpdatedAt
        });
    }

    [HttpPost]
    [RequirePermission(Permissions.ActivityCreate)]
    public async Task<ActionResult<ApiResponse<ActivityResponse>>> Create([FromBody] CreateActivityRequest request)
    {
        var now = DateTime.UtcNow;
        var activity = new Activity
        {
            Subject = request.Subject,
            Description = request.Description,
            Type = request.Type,
            Status = ActivityStatus.NotStarted,
            Priority = request.Priority,
            StartDate = request.StartDate,
            DueDate = request.DueDate,
            DurationMinutes = request.DurationMinutes,
            AssignedToUserId = request.AssignedToUserId ?? _currentUser.UserId,
            CustomerId = request.CustomerId,
            ContactId = request.ContactId,
            LeadId = request.LeadId,
            OpportunityId = request.OpportunityId,
            TicketId = request.TicketId,
            IsRecurring = request.IsRecurring,
            RecurrencePattern = request.RecurrencePattern,
            RecurrenceInterval = request.RecurrenceInterval,
            RecurrenceEndDate = request.RecurrenceEndDate,
            Tags = request.Tags,
            CreatedBy = _currentUser.UserId
        };

        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        // Create reminders if provided
        if (request.Reminders != null && request.Reminders.Any())
        {
            foreach (var reminderReq in request.Reminders)
            {
                var reminder = new Reminder
                {
                    ActivityId = activity.Id,
                    Subject = reminderReq.Subject ?? activity.Subject,
                    Message = reminderReq.Message,
                    ReminderDate = reminderReq.ReminderDate,
                    Method = reminderReq.Method,
                    UserId = activity.AssignedToUserId,
                    CreatedBy = _currentUser.UserId
                };
                _db.Set<Reminder>().Add(reminder);
            }
            await _db.SaveChangesAsync();
        }

        await _auditService.LogAsync(AuditActions.Create, nameof(Activity), activity.Id, activity.Subject);

        return CreatedResponse(new ActivityResponse
        {
            Id = activity.Id,
            Subject = activity.Subject,
            Type = activity.Type.ToString(),
            Status = activity.Status.ToString(),
            Priority = activity.Priority.ToString(),
            StartDate = activity.StartDate,
            DueDate = activity.DueDate,
            DurationMinutes = activity.DurationMinutes,
            IsOverdue = activity.DueDate.HasValue && activity.DueDate < now && activity.Status != ActivityStatus.Completed,
            CreatedAt = activity.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.ActivityUpdate)]
    public async Task<ActionResult<ApiResponse<ActivityResponse>>> Update(Guid id, [FromBody] UpdateActivityRequest request)
    {
        var activity = await _db.Activities.FindAsync(id);

        if (activity == null)
        {
            return NotFoundResponse<ActivityResponse>($"Activity with id {id} not found");
        }

        activity.Subject = request.Subject ?? activity.Subject;
        activity.Description = request.Description ?? activity.Description;
        activity.Type = request.Type ?? activity.Type;
        activity.Priority = request.Priority ?? activity.Priority;
        activity.StartDate = request.StartDate ?? activity.StartDate;
        activity.DueDate = request.DueDate ?? activity.DueDate;
        activity.DurationMinutes = request.DurationMinutes ?? activity.DurationMinutes;
        activity.AssignedToUserId = request.AssignedToUserId ?? activity.AssignedToUserId;
        activity.Tags = request.Tags ?? activity.Tags;
        activity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Activity), activity.Id, activity.Subject);

        var now = DateTime.UtcNow;
        return OkResponse(new ActivityResponse
        {
            Id = activity.Id,
            Subject = activity.Subject,
            Type = activity.Type.ToString(),
            Status = activity.Status.ToString(),
            Priority = activity.Priority.ToString(),
            StartDate = activity.StartDate,
            DueDate = activity.DueDate,
            DurationMinutes = activity.DurationMinutes,
            IsOverdue = activity.DueDate.HasValue && activity.DueDate < now && activity.Status != ActivityStatus.Completed,
            CreatedAt = activity.CreatedAt
        });
    }

    [HttpPost("{id:guid}/start")]
    [RequirePermission(Permissions.ActivityUpdate)]
    public async Task<ActionResult<ApiResponse>> Start(Guid id)
    {
        var activity = await _db.Activities.FindAsync(id);

        if (activity == null)
        {
            return NotFoundResponse($"Activity with id {id} not found");
        }

        if (activity.Status != ActivityStatus.NotStarted)
        {
            return BadRequestResponse("Activity cannot be started");
        }

        activity.Status = ActivityStatus.InProgress;
        activity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Activity), activity.Id, $"{activity.Subject} started");

        return OkResponse("Activity started");
    }

    [HttpPost("{id:guid}/complete")]
    [RequirePermission(Permissions.ActivityUpdate)]
    public async Task<ActionResult<ApiResponse>> Complete(Guid id)
    {
        var activity = await _db.Activities.FindAsync(id);

        if (activity == null)
        {
            return NotFoundResponse($"Activity with id {id} not found");
        }

        activity.Status = ActivityStatus.Completed;
        activity.CompletedDate = DateTime.UtcNow;
        activity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Activity), activity.Id, $"{activity.Subject} completed");

        return OkResponse("Activity completed");
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(Permissions.ActivityUpdate)]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid id)
    {
        var activity = await _db.Activities.FindAsync(id);

        if (activity == null)
        {
            return NotFoundResponse($"Activity with id {id} not found");
        }

        activity.Status = ActivityStatus.Cancelled;
        activity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Activity), activity.Id, $"{activity.Subject} cancelled");

        return OkResponse("Activity cancelled");
    }

    [HttpPost("{id:guid}/reschedule")]
    [RequirePermission(Permissions.ActivityUpdate)]
    public async Task<ActionResult<ApiResponse>> Reschedule(Guid id, [FromBody] RescheduleRequest request)
    {
        var activity = await _db.Activities.FindAsync(id);

        if (activity == null)
        {
            return NotFoundResponse($"Activity with id {id} not found");
        }

        activity.StartDate = request.StartDate;
        activity.DueDate = request.DueDate;
        activity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Activity), activity.Id, $"{activity.Subject} rescheduled");

        return OkResponse("Activity rescheduled");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.ActivityDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var activity = await _db.Activities.FindAsync(id);

        if (activity == null)
        {
            return NotFoundResponse($"Activity with id {id} not found");
        }

        _db.Activities.Remove(activity);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Delete, nameof(Activity), activity.Id, activity.Subject);

        return OkResponse("Activity deleted");
    }
}

// DTOs
public class ActivityResponse
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsOverdue { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ActivityDetailResponse : ActivityResponse
{
    public string? Description { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public Guid? LeadId { get; set; }
    public string? LeadName { get; set; }
    public Guid? OpportunityId { get; set; }
    public string? OpportunityName { get; set; }
    public Guid? TicketId { get; set; }
    public string? TicketNumber { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime? CompletedDate { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public int? RecurrenceInterval { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public string? Tags { get; set; }
    public List<ReminderResponse> Reminders { get; set; } = [];
    public DateTime? UpdatedAt { get; set; }
}

public class ReminderResponse
{
    public Guid Id { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public DateTime ReminderDate { get; set; }
    public string Method { get; set; } = string.Empty;
    public bool IsSent { get; set; }
}

public class CreateActivityRequest
{
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ActivityType Type { get; set; } = ActivityType.Task;
    public ActivityPriority Priority { get; set; } = ActivityPriority.Medium;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int? DurationMinutes { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? OpportunityId { get; set; }
    public Guid? TicketId { get; set; }
    public bool IsRecurring { get; set; }
    public RecurrencePattern? RecurrencePattern { get; set; }
    public int? RecurrenceInterval { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public string? Tags { get; set; }
    public List<CreateReminderRequest>? Reminders { get; set; }
}

public class UpdateActivityRequest
{
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public ActivityType? Type { get; set; }
    public ActivityPriority? Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int? DurationMinutes { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Tags { get; set; }
}

public class CreateReminderRequest
{
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public DateTime ReminderDate { get; set; }
    public ReminderMethod Method { get; set; } = ReminderMethod.InApp;
}

public class RescheduleRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}
