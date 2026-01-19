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
public class LeadsController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public LeadsController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [HttpGet]
    [RequirePermission(Permissions.LeadView)]
    public async Task<ActionResult<ApiResponse<PagedResult<LeadResponse>>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] LeadStatus? status = null,
        [FromQuery] LeadRating? rating = null,
        [FromQuery] Guid? assignedTo = null)
    {
        var query = _db.Leads
            .AsNoTracking()
            .Include(l => l.AssignedToUser)
            .WhereIf(status.HasValue, l => l.Status == status!.Value)
            .WhereIf(rating.HasValue, l => l.Rating == rating!.Value)
            .WhereIf(assignedTo.HasValue, l => l.AssignedToUserId == assignedTo)
            .WhereIf(!string.IsNullOrEmpty(pagination.Search), l =>
                l.Title.Contains(pagination.Search!) ||
                l.Email!.Contains(pagination.Search!) ||
                l.CompanyName!.Contains(pagination.Search!))
            .ApplySorting(pagination.SortBy ?? "CreatedAt", pagination.SortDescending);

        var result = await query
            .Select(l => new LeadResponse
            {
                Id = l.Id,
                Title = l.Title,
                FullName = l.FullName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Status = l.Status.ToString(),
                Rating = l.Rating.ToString(),
                Source = l.Source.ToString(),
                Score = l.Score,
                EstimatedValue = l.EstimatedValue,
                AssignedToUserName = l.AssignedToUser != null ? l.AssignedToUser.FullName : null,
                CreatedAt = l.CreatedAt
            })
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.LeadView)]
    public async Task<ActionResult<ApiResponse<LeadDetailResponse>>> GetById(Guid id)
    {
        var lead = await _db.Leads
            .AsNoTracking()
            .Include(l => l.AssignedToUser)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lead == null)
        {
            return NotFoundResponse<LeadDetailResponse>($"Lead with id {id} not found");
        }

        return OkResponse(MapToDetailResponse(lead));
    }

    [HttpPost]
    [RequirePermission(Permissions.LeadCreate)]
    public async Task<ActionResult<ApiResponse<LeadResponse>>> Create([FromBody] CreateLeadRequest request)
    {
        var lead = new Lead
        {
            Title = request.Title,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Mobile = request.Mobile,
            CompanyName = request.CompanyName,
            JobTitle = request.JobTitle,
            Industry = request.Industry,
            EmployeeCount = request.EmployeeCount,
            AddressLine1 = request.AddressLine1,
            City = request.City,
            State = request.State,
            Country = request.Country,
            Source = request.Source,
            SourceDetail = request.SourceDetail,
            EstimatedValue = request.EstimatedValue,
            Description = request.Description,
            AssignedToUserId = request.AssignedToUserId ?? _currentUser.UserId,
            AssignedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Create, nameof(Lead), lead.Id, lead.Title);

        return CreatedResponse(new LeadResponse
        {
            Id = lead.Id,
            Title = lead.Title,
            FullName = lead.FullName,
            Email = lead.Email,
            Status = lead.Status.ToString(),
            Rating = lead.Rating.ToString(),
            Score = lead.Score,
            CreatedAt = lead.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.LeadUpdate)]
    public async Task<ActionResult<ApiResponse<LeadResponse>>> Update(Guid id, [FromBody] UpdateLeadRequest request)
    {
        var lead = await _db.Leads.FindAsync(id);

        if (lead == null)
        {
            return NotFoundResponse<LeadResponse>($"Lead with id {id} not found");
        }

        lead.Title = request.Title ?? lead.Title;
        lead.FirstName = request.FirstName ?? lead.FirstName;
        lead.LastName = request.LastName ?? lead.LastName;
        lead.Email = request.Email ?? lead.Email;
        lead.Phone = request.Phone ?? lead.Phone;
        lead.CompanyName = request.CompanyName ?? lead.CompanyName;
        lead.Status = request.Status ?? lead.Status;
        lead.Rating = request.Rating ?? lead.Rating;
        lead.Score = request.Score ?? lead.Score;
        lead.EstimatedValue = request.EstimatedValue ?? lead.EstimatedValue;
        lead.Description = request.Description ?? lead.Description;
        lead.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Lead), lead.Id, lead.Title);

        return OkResponse(new LeadResponse
        {
            Id = lead.Id,
            Title = lead.Title,
            FullName = lead.FullName,
            Email = lead.Email,
            Status = lead.Status.ToString(),
            Rating = lead.Rating.ToString(),
            Score = lead.Score,
            CreatedAt = lead.CreatedAt
        });
    }

    [HttpPost("{id:guid}/assign")]
    [RequirePermission(Permissions.LeadAssign)]
    public async Task<ActionResult<ApiResponse>> Assign(Guid id, [FromBody] AssignLeadRequest request)
    {
        var lead = await _db.Leads.FindAsync(id);

        if (lead == null)
        {
            return NotFoundResponse($"Lead with id {id} not found");
        }

        lead.AssignedToUserId = request.UserId;
        lead.AssignedAt = DateTime.UtcNow;
        lead.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Assign, nameof(Lead), lead.Id, lead.Title);

        return OkResponse("Lead assigned successfully");
    }

    [HttpPost("{id:guid}/convert")]
    [RequirePermission(Permissions.LeadConvert)]
    public async Task<ActionResult<ApiResponse<ConvertLeadResponse>>> Convert(Guid id, [FromBody] ConvertLeadRequest request)
    {
        var lead = await _db.Leads.FindAsync(id);

        if (lead == null)
        {
            return NotFoundResponse<ConvertLeadResponse>($"Lead with id {id} not found");
        }

        if (lead.Status == LeadStatus.Converted)
        {
            return BadRequestResponse<ConvertLeadResponse>("Lead is already converted");
        }

        // Create customer
        var customer = new Customer
        {
            Name = request.CustomerName ?? lead.CompanyName ?? lead.FullName,
            Type = string.IsNullOrEmpty(lead.CompanyName) ? CustomerType.Individual : CustomerType.Business,
            Email = lead.Email,
            Phone = lead.Phone,
            Mobile = lead.Mobile,
            FirstName = lead.FirstName,
            LastName = lead.LastName,
            CompanyName = lead.CompanyName,
            Industry = lead.Industry,
            AddressLine1 = lead.AddressLine1,
            City = lead.City,
            State = lead.State,
            Country = lead.Country,
            Source = (CustomerSource)(int)lead.Source,
            SourceDetail = lead.SourceDetail,
            Status = CustomerStatus.Active,
            CreatedBy = _currentUser.UserId
        };

        _db.Customers.Add(customer);

        // Create opportunity if requested
        Opportunity? opportunity = null;
        if (request.CreateOpportunity)
        {
            var defaultPipeline = await _db.Pipelines
                .Include(p => p.Stages)
                .FirstOrDefaultAsync(p => p.IsDefault);

            if (defaultPipeline != null)
            {
                var firstStage = defaultPipeline.Stages.OrderBy(s => s.SortOrder).FirstOrDefault();
                if (firstStage != null)
                {
                    opportunity = new Opportunity
                    {
                        Name = request.OpportunityName ?? $"Opportunity from {lead.Title}",
                        CustomerId = customer.Id,
                        PipelineId = defaultPipeline.Id,
                        StageId = firstStage.Id,
                        Amount = lead.EstimatedValue ?? 0,
                        Probability = firstStage.Probability,
                        SourceLeadId = lead.Id,
                        AssignedToUserId = lead.AssignedToUserId,
                        CreatedBy = _currentUser.UserId
                    };

                    _db.Opportunities.Add(opportunity);
                }
            }
        }

        // Update lead
        lead.Status = LeadStatus.Converted;
        lead.ConvertedToCustomerId = customer.Id;
        lead.ConvertedToOpportunityId = opportunity?.Id;
        lead.ConvertedAt = DateTime.UtcNow;
        lead.ConvertedByUserId = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Convert, nameof(Lead), lead.Id, lead.Title);

        return OkResponse(new ConvertLeadResponse
        {
            CustomerId = customer.Id,
            OpportunityId = opportunity?.Id
        });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.LeadDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var lead = await _db.Leads.FindAsync(id);

        if (lead == null)
        {
            return NotFoundResponse($"Lead with id {id} not found");
        }

        lead.IsDeleted = true;
        lead.DeletedAt = DateTime.UtcNow;
        lead.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.SoftDelete, nameof(Lead), lead.Id, lead.Title);

        return OkResponse("Lead deleted successfully");
    }

    private static LeadDetailResponse MapToDetailResponse(Lead l) => new()
    {
        Id = l.Id,
        Title = l.Title,
        FirstName = l.FirstName,
        LastName = l.LastName,
        FullName = l.FullName,
        Email = l.Email,
        Phone = l.Phone,
        Mobile = l.Mobile,
        CompanyName = l.CompanyName,
        JobTitle = l.JobTitle,
        Industry = l.Industry,
        EmployeeCount = l.EmployeeCount,
        AddressLine1 = l.AddressLine1,
        City = l.City,
        State = l.State,
        Country = l.Country,
        Status = l.Status.ToString(),
        Source = l.Source.ToString(),
        SourceDetail = l.SourceDetail,
        Rating = l.Rating.ToString(),
        Score = l.Score,
        EstimatedValue = l.EstimatedValue,
        Description = l.Description,
        AssignedToUserId = l.AssignedToUserId,
        AssignedToUserName = l.AssignedToUser?.FullName,
        AssignedAt = l.AssignedAt,
        ConvertedToCustomerId = l.ConvertedToCustomerId,
        ConvertedAt = l.ConvertedAt,
        CreatedAt = l.CreatedAt,
        UpdatedAt = l.UpdatedAt
    };
}

public class CreateLeadRequest
{
    public string Title { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public string? Industry { get; set; }
    public int? EmployeeCount { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public LeadSource Source { get; set; } = LeadSource.Website;
    public string? SourceDetail { get; set; }
    public decimal? EstimatedValue { get; set; }
    public string? Description { get; set; }
    public Guid? AssignedToUserId { get; set; }
}

public class UpdateLeadRequest
{
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public LeadStatus? Status { get; set; }
    public LeadRating? Rating { get; set; }
    public int? Score { get; set; }
    public decimal? EstimatedValue { get; set; }
    public string? Description { get; set; }
}

public class AssignLeadRequest
{
    public Guid UserId { get; set; }
}

public class ConvertLeadRequest
{
    public string? CustomerName { get; set; }
    public bool CreateOpportunity { get; set; } = true;
    public string? OpportunityName { get; set; }
}

public class LeadResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Rating { get; set; } = string.Empty;
    public string? Source { get; set; }
    public int Score { get; set; }
    public decimal? EstimatedValue { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LeadDetailResponse : LeadResponse
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Mobile { get; set; }
    public string? JobTitle { get; set; }
    public string? Industry { get; set; }
    public int? EmployeeCount { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? SourceDetail { get; set; }
    public string? Description { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public Guid? ConvertedToCustomerId { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ConvertLeadResponse
{
    public Guid CustomerId { get; set; }
    public Guid? OpportunityId { get; set; }
}
