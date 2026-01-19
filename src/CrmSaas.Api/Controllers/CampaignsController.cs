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
public class CampaignsController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public CampaignsController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [HttpGet]
    [RequirePermission(Permissions.CampaignView)]
    public async Task<ActionResult<ApiResponse<PagedResult<CampaignResponse>>>> GetAll(
        [FromQuery] CampaignFilterParams filters)
    {
        var query = _db.Campaigns
            .AsNoTracking()
            .WhereIf(!string.IsNullOrEmpty(filters.Status), c => c.Status.ToString() == filters.Status)
            .WhereIf(!string.IsNullOrEmpty(filters.Type), c => c.Type.ToString() == filters.Type)
            .WhereIf(!string.IsNullOrEmpty(filters.Name), c => c.Name.Contains(filters.Name!))
            .WhereIf(!string.IsNullOrEmpty(filters.Search), c =>
                c.Name.Contains(filters.Search!) || c.Description!.Contains(filters.Search!))
            .WhereIf(filters.StartDateFrom.HasValue, c => c.StartDate >= filters.StartDateFrom!.Value)
            .WhereIf(filters.StartDateTo.HasValue, c => c.StartDate <= filters.StartDateTo!.Value)
            .WhereIf(filters.MinBudget.HasValue, c => c.Budget >= filters.MinBudget!.Value)
            .WhereIf(filters.MaxBudget.HasValue, c => c.Budget <= filters.MaxBudget!.Value)
            .ApplySorting(filters.SortBy ?? "CreatedAt", filters.SortDescending);

        var result = await query
            .Select(c => new CampaignResponse
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type.ToString(),
                Status = c.Status.ToString(),
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Budget = c.Budget ?? 0,
                ActualCost = c.ActualCost ?? 0,
                ExpectedRevenue = c.ExpectedRevenue ?? 0,
                ActualRevenue = c.ActualRevenue ?? 0,
                TotalLeadsGenerated = c.TotalLeadsGenerated,
                TotalConversions = c.TotalConversions,
                TotalSent = c.TotalSent,
                CreatedAt = c.CreatedAt
            })
            .ToPagedResultAsync(filters.Page, filters.PageSize);

        return OkResponse(result);
    }

    [HttpGet("active")]
    [RequirePermission(Permissions.CampaignView)]
    public async Task<ActionResult<ApiResponse<List<CampaignResponse>>>> GetActive()
    {
        var now = DateTime.UtcNow;

        var campaigns = await _db.Campaigns
            .AsNoTracking()
            .Where(c => c.Status == CampaignStatus.InProgress)
            .Where(c => c.StartDate <= now && (c.EndDate == null || c.EndDate >= now))
            .OrderBy(c => c.Name)
            .Select(c => new CampaignResponse
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type.ToString(),
                Status = c.Status.ToString(),
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Budget = c.Budget ?? 0,
                ActualCost = c.ActualCost ?? 0,
                TotalLeadsGenerated = c.TotalLeadsGenerated,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return OkResponse(campaigns);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.CampaignView)]
    public async Task<ActionResult<ApiResponse<CampaignDetailResponse>>> GetById(Guid id)
    {
        var campaign = await _db.Campaigns
            .AsNoTracking()
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (campaign == null)
        {
            return NotFoundResponse<CampaignDetailResponse>($"Campaign with id {id} not found");
        }

        return OkResponse(MapToDetailResponse(campaign));
    }

    [HttpPost]
    [RequirePermission(Permissions.CampaignCreate)]
    public async Task<ActionResult<ApiResponse<CampaignResponse>>> Create([FromBody] CreateCampaignRequest request)
    {
        var campaign = new Campaign
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Budget = request.Budget,
            Currency = request.Currency ?? "USD",
            ExpectedRevenue = request.ExpectedRevenue,
            ExpectedLeads = request.ExpectedLeads,
            ExpectedConversions = request.ExpectedConversions,
            TargetAudience = request.TargetAudience,
            Tags = request.Tags,
            OwnerId = request.OwnerId ?? _currentUser.UserId,
            CreatedBy = _currentUser.UserId
        };

        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Create, nameof(Campaign), campaign.Id, campaign.Name);

        return CreatedResponse(new CampaignResponse
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Type = campaign.Type.ToString(),
            Status = campaign.Status.ToString(),
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            Budget = campaign.Budget,
            CreatedAt = campaign.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.CampaignUpdate)]
    public async Task<ActionResult<ApiResponse<CampaignResponse>>> Update(Guid id, [FromBody] UpdateCampaignRequest request)
    {
        var campaign = await _db.Campaigns.FindAsync(id);

        if (campaign == null)
        {
            return NotFoundResponse<CampaignResponse>($"Campaign with id {id} not found");
        }

        campaign.Name = request.Name ?? campaign.Name;
        campaign.Description = request.Description ?? campaign.Description;
        campaign.StartDate = request.StartDate ?? campaign.StartDate;
        campaign.EndDate = request.EndDate ?? campaign.EndDate;
        campaign.Budget = request.Budget ?? campaign.Budget;
        campaign.ExpectedRevenue = request.ExpectedRevenue ?? campaign.ExpectedRevenue;
        campaign.ExpectedLeads = request.ExpectedLeads ?? campaign.ExpectedLeads;
        campaign.ExpectedConversions = request.ExpectedConversions ?? campaign.ExpectedConversions;
        campaign.TargetAudience = request.TargetAudience ?? campaign.TargetAudience;
        campaign.Tags = request.Tags ?? campaign.Tags;
        campaign.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Campaign), campaign.Id, campaign.Name);

        return OkResponse(new CampaignResponse
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Type = campaign.Type.ToString(),
            Status = campaign.Status.ToString(),
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            Budget = campaign.Budget,
            CreatedAt = campaign.CreatedAt
        });
    }

    [HttpPost("{id:guid}/activate")]
    [RequirePermission(Permissions.CampaignUpdate)]
    public async Task<ActionResult<ApiResponse>> Activate(Guid id)
    {
        var campaign = await _db.Campaigns.FindAsync(id);

        if (campaign == null)
        {
            return NotFoundResponse($"Campaign with id {id} not found");
        }

        campaign.Status = CampaignStatus.InProgress;
        campaign.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Campaign), campaign.Id, campaign.Name);

        return OkResponse("Campaign activated");
    }

    [HttpPost("{id:guid}/pause")]
    [RequirePermission(Permissions.CampaignUpdate)]
    public async Task<ActionResult<ApiResponse>> Pause(Guid id)
    {
        var campaign = await _db.Campaigns.FindAsync(id);

        if (campaign == null)
        {
            return NotFoundResponse($"Campaign with id {id} not found");
        }

        campaign.Status = CampaignStatus.Paused;
        campaign.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Campaign), campaign.Id, campaign.Name);

        return OkResponse("Campaign paused");
    }

    [HttpPost("{id:guid}/complete")]
    [RequirePermission(Permissions.CampaignUpdate)]
    public async Task<ActionResult<ApiResponse>> Complete(Guid id)
    {
        var campaign = await _db.Campaigns.FindAsync(id);

        if (campaign == null)
        {
            return NotFoundResponse($"Campaign with id {id} not found");
        }

        campaign.Status = CampaignStatus.Completed;
        campaign.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Campaign), campaign.Id, campaign.Name);

        return OkResponse("Campaign completed");
    }

    [HttpPost("{id:guid}/update-metrics")]
    [RequirePermission(Permissions.CampaignUpdate)]
    public async Task<ActionResult<ApiResponse>> UpdateMetrics(Guid id, [FromBody] UpdateCampaignMetricsRequest request)
    {
        var campaign = await _db.Campaigns.FindAsync(id);

        if (campaign == null)
        {
            return NotFoundResponse($"Campaign with id {id} not found");
        }

        campaign.ActualCost = request.ActualCost ?? campaign.ActualCost;
        campaign.ActualRevenue = request.ActualRevenue ?? campaign.ActualRevenue;
        campaign.TotalSent = request.TotalSent ?? campaign.TotalSent;
        campaign.TotalDelivered = request.TotalDelivered ?? campaign.TotalDelivered;
        campaign.TotalOpened = request.TotalOpened ?? campaign.TotalOpened;
        campaign.TotalClicked = request.TotalClicked ?? campaign.TotalClicked;
        campaign.TotalBounced = request.TotalBounced ?? campaign.TotalBounced;
        campaign.TotalUnsubscribed = request.TotalUnsubscribed ?? campaign.TotalUnsubscribed;
        campaign.TotalLeadsGenerated = request.TotalLeadsGenerated ?? campaign.TotalLeadsGenerated;
        campaign.TotalConversions = request.TotalConversions ?? campaign.TotalConversions;
        campaign.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Campaign), campaign.Id, campaign.Name);

        return OkResponse("Metrics updated");
    }

    [HttpGet("{id:guid}/performance")]
    [RequirePermission(Permissions.CampaignView)]
    public async Task<ActionResult<ApiResponse<CampaignPerformanceResponse>>> GetPerformance(Guid id)
    {
        var campaign = await _db.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (campaign == null)
        {
            return NotFoundResponse<CampaignPerformanceResponse>($"Campaign with id {id} not found");
        }

        var response = new CampaignPerformanceResponse
        {
            CampaignId = campaign.Id,
            CampaignName = campaign.Name,
            Status = campaign.Status.ToString(),
            Budget = campaign.Budget ?? 0,
            ActualCost = campaign.ActualCost ?? 0,
            ExpectedRevenue = campaign.ExpectedRevenue ?? 0,
            ActualRevenue = campaign.ActualRevenue ?? 0,
            Roi = campaign.ActualCost > 0 ? ((campaign.ActualRevenue ?? 0) - (campaign.ActualCost ?? 0)) / (campaign.ActualCost ?? 1) * 100 : 0,
            TotalSent = campaign.TotalSent,
            TotalDelivered = campaign.TotalDelivered,
            TotalOpened = campaign.TotalOpened,
            TotalClicked = campaign.TotalClicked,
            TotalLeadsGenerated = campaign.TotalLeadsGenerated,
            TotalConversions = campaign.TotalConversions,
            OpenRate = campaign.TotalDelivered > 0 ? (decimal)campaign.TotalOpened / campaign.TotalDelivered * 100 : 0,
            ClickRate = campaign.TotalOpened > 0 ? (decimal)campaign.TotalClicked / campaign.TotalOpened * 100 : 0,
            ConversionRate = campaign.TotalLeadsGenerated > 0 ? (decimal)campaign.TotalConversions / campaign.TotalLeadsGenerated * 100 : 0
        };

        return OkResponse(response);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.CampaignDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var campaign = await _db.Campaigns.FindAsync(id);

        if (campaign == null)
        {
            return NotFoundResponse($"Campaign with id {id} not found");
        }

        campaign.IsDeleted = true;
        campaign.DeletedAt = DateTime.UtcNow;
        campaign.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.SoftDelete, nameof(Campaign), campaign.Id, campaign.Name);

        return OkResponse("Campaign deleted");
    }

    private static CampaignDetailResponse MapToDetailResponse(Campaign campaign)
    {
        return new CampaignDetailResponse
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            Type = campaign.Type.ToString(),
            Status = campaign.Status.ToString(),
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            Budget = campaign.Budget ?? 0,
            ActualCost = campaign.ActualCost ?? 0,
            Currency = campaign.Currency ?? "USD",
            ExpectedRevenue = campaign.ExpectedRevenue ?? 0,
            ActualRevenue = campaign.ActualRevenue ?? 0,
            ExpectedLeads = campaign.ExpectedLeads ?? 0,
            ExpectedConversions = campaign.ExpectedConversions ?? 0,
            TotalSent = campaign.TotalSent,
            TotalDelivered = campaign.TotalDelivered,
            TotalOpened = campaign.TotalOpened,
            TotalClicked = campaign.TotalClicked,
            TotalBounced = campaign.TotalBounced,
            TotalUnsubscribed = campaign.TotalUnsubscribed,
            TotalLeadsGenerated = campaign.TotalLeadsGenerated,
            TotalConversions = campaign.TotalConversions,
            OwnerName = campaign.Owner?.FullName,
            TargetAudience = campaign.TargetAudience,
            Tags = campaign.Tags,
            CreatedAt = campaign.CreatedAt
        };
    }
}

// Request/Response DTOs
public class CreateCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CampaignType Type { get; set; } = CampaignType.Email;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Budget { get; set; }
    public string? Currency { get; set; }
    public decimal? ExpectedRevenue { get; set; }
    public int? ExpectedLeads { get; set; }
    public int? ExpectedConversions { get; set; }
    public string? TargetAudience { get; set; }
    public string? Tags { get; set; }
    public Guid? OwnerId { get; set; }
}

public class UpdateCampaignRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Budget { get; set; }
    public decimal? ExpectedRevenue { get; set; }
    public int? ExpectedLeads { get; set; }
    public int? ExpectedConversions { get; set; }
    public string? TargetAudience { get; set; }
    public string? Tags { get; set; }
}

public class UpdateCampaignMetricsRequest
{
    public decimal? ActualCost { get; set; }
    public decimal? ActualRevenue { get; set; }
    public int? TotalSent { get; set; }
    public int? TotalDelivered { get; set; }
    public int? TotalOpened { get; set; }
    public int? TotalClicked { get; set; }
    public int? TotalBounced { get; set; }
    public int? TotalUnsubscribed { get; set; }
    public int? TotalLeadsGenerated { get; set; }
    public int? TotalConversions { get; set; }
}

public class CampaignResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Budget { get; set; }
    public decimal? ActualCost { get; set; }
    public decimal? ExpectedRevenue { get; set; }
    public decimal? ActualRevenue { get; set; }
    public int TotalLeadsGenerated { get; set; }
    public int TotalConversions { get; set; }
    public int TotalSent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CampaignDetailResponse : CampaignResponse
{
    public string? Description { get; set; }
    public string? Currency { get; set; }
    public int? ExpectedLeads { get; set; }
    public int? ExpectedConversions { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalOpened { get; set; }
    public int TotalClicked { get; set; }
    public int TotalBounced { get; set; }
    public int TotalUnsubscribed { get; set; }
    public string? OwnerName { get; set; }
    public string? TargetAudience { get; set; }
    public string? Tags { get; set; }
}

public class CampaignPerformanceResponse
{
    public Guid CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public decimal ActualCost { get; set; }
    public decimal ExpectedRevenue { get; set; }
    public decimal ActualRevenue { get; set; }
    public decimal Roi { get; set; }
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalOpened { get; set; }
    public int TotalClicked { get; set; }
    public int TotalLeadsGenerated { get; set; }
    public int TotalConversions { get; set; }
    public decimal OpenRate { get; set; }
    public decimal ClickRate { get; set; }
    public decimal ConversionRate { get; set; }
}

public class CampaignFilterParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    // Pagination
    public int Page { get; set; } = 1;
    
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    // Sorting
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;

    // Filters
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Search { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public decimal? MinBudget { get; set; }
    public decimal? MaxBudget { get; set; }
}
