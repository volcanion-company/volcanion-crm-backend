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
public class OpportunitiesController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public OpportunitiesController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [HttpGet]
    [RequirePermission(Permissions.OpportunityView)]
    public async Task<ActionResult<ApiResponse<PagedResult<OpportunityResponse>>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] OpportunityStatus? status = null,
        [FromQuery] Guid? pipelineId = null,
        [FromQuery] Guid? stageId = null,
        [FromQuery] Guid? customerId = null)
    {
        var query = _db.Opportunities
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Stage)
            .Include(o => o.AssignedToUser)
            .WhereIf(status.HasValue, o => o.Status == status!.Value)
            .WhereIf(pipelineId.HasValue, o => o.PipelineId == pipelineId)
            .WhereIf(stageId.HasValue, o => o.StageId == stageId)
            .WhereIf(customerId.HasValue, o => o.CustomerId == customerId)
            .WhereIf(!string.IsNullOrEmpty(pagination.Search), o =>
                o.Name.Contains(pagination.Search!))
            .ApplySorting(pagination.SortBy ?? "CreatedAt", pagination.SortDescending);

        var result = await query
            .Select(o => new OpportunityResponse
            {
                Id = o.Id,
                Name = o.Name,
                CustomerName = o.Customer != null ? o.Customer.Name : null,
                StageName = o.Stage != null ? o.Stage.Name : null,
                Status = o.Status.ToString(),
                Amount = o.Amount,
                Currency = o.Currency,
                Probability = o.Probability,
                WeightedAmount = o.WeightedAmount,
                ExpectedCloseDate = o.ExpectedCloseDate,
                AssignedToUserName = o.AssignedToUser != null ? o.AssignedToUser.FullName : null,
                CreatedAt = o.CreatedAt
            })
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.OpportunityView)]
    public async Task<ActionResult<ApiResponse<OpportunityDetailResponse>>> GetById(Guid id)
    {
        var opportunity = await _db.Opportunities
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Pipeline)
            .Include(o => o.Stage)
            .Include(o => o.AssignedToUser)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (opportunity == null)
        {
            return NotFoundResponse<OpportunityDetailResponse>($"Opportunity with id {id} not found");
        }

        return OkResponse(MapToDetailResponse(opportunity));
    }

    [HttpPost]
    [RequirePermission(Permissions.OpportunityCreate)]
    public async Task<ActionResult<ApiResponse<OpportunityResponse>>> Create([FromBody] CreateOpportunityRequest request)
    {
        var stage = await _db.PipelineStages.FindAsync(request.StageId);
        
        var opportunity = new Opportunity
        {
            Name = request.Name,
            CustomerId = request.CustomerId,
            PrimaryContactId = request.PrimaryContactId,
            PipelineId = request.PipelineId,
            StageId = request.StageId,
            Amount = request.Amount,
            Currency = request.Currency ?? "USD",
            Probability = request.Probability ?? stage?.Probability ?? 0,
            ExpectedCloseDate = request.ExpectedCloseDate,
            Type = request.Type,
            Priority = request.Priority,
            Description = request.Description,
            AssignedToUserId = request.AssignedToUserId ?? _currentUser.UserId,
            CreatedBy = _currentUser.UserId
        };

        _db.Opportunities.Add(opportunity);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Create, nameof(Opportunity), opportunity.Id, opportunity.Name);

        return CreatedResponse(new OpportunityResponse
        {
            Id = opportunity.Id,
            Name = opportunity.Name,
            Status = opportunity.Status.ToString(),
            Amount = opportunity.Amount,
            Probability = opportunity.Probability,
            CreatedAt = opportunity.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.OpportunityUpdate)]
    public async Task<ActionResult<ApiResponse<OpportunityResponse>>> Update(Guid id, [FromBody] UpdateOpportunityRequest request)
    {
        var opportunity = await _db.Opportunities.FindAsync(id);

        if (opportunity == null)
        {
            return NotFoundResponse<OpportunityResponse>($"Opportunity with id {id} not found");
        }

        opportunity.Name = request.Name ?? opportunity.Name;
        opportunity.StageId = request.StageId ?? opportunity.StageId;
        opportunity.Amount = request.Amount ?? opportunity.Amount;
        opportunity.Probability = request.Probability ?? opportunity.Probability;
        opportunity.ExpectedCloseDate = request.ExpectedCloseDate ?? opportunity.ExpectedCloseDate;
        opportunity.Description = request.Description ?? opportunity.Description;
        opportunity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Opportunity), opportunity.Id, opportunity.Name);

        return OkResponse(new OpportunityResponse
        {
            Id = opportunity.Id,
            Name = opportunity.Name,
            Status = opportunity.Status.ToString(),
            Amount = opportunity.Amount,
            Probability = opportunity.Probability,
            CreatedAt = opportunity.CreatedAt
        });
    }

    [HttpPost("{id:guid}/move-stage")]
    [RequirePermission(Permissions.OpportunityUpdate)]
    public async Task<ActionResult<ApiResponse>> MoveStage(Guid id, [FromBody] MoveStageRequest request)
    {
        var opportunity = await _db.Opportunities.FindAsync(id);

        if (opportunity == null)
        {
            return NotFoundResponse($"Opportunity with id {id} not found");
        }

        var stage = await _db.PipelineStages.FindAsync(request.StageId);
        
        if (stage == null)
        {
            return BadRequestResponse($"Stage with id {request.StageId} not found");
        }

        opportunity.StageId = stage.Id;
        opportunity.Probability = stage.Probability;

        if (stage.IsWon)
        {
            opportunity.Status = OpportunityStatus.Won;
            opportunity.ActualCloseDate = DateTime.UtcNow;
            opportunity.WinReason = request.Reason;
        }
        else if (stage.IsLost)
        {
            opportunity.Status = OpportunityStatus.Lost;
            opportunity.ActualCloseDate = DateTime.UtcNow;
            opportunity.LossReason = request.Reason;
        }

        opportunity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Opportunity), opportunity.Id, opportunity.Name);

        return OkResponse("Stage updated successfully");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.OpportunityDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var opportunity = await _db.Opportunities.FindAsync(id);

        if (opportunity == null)
        {
            return NotFoundResponse($"Opportunity with id {id} not found");
        }

        opportunity.IsDeleted = true;
        opportunity.DeletedAt = DateTime.UtcNow;
        opportunity.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.SoftDelete, nameof(Opportunity), opportunity.Id, opportunity.Name);

        return OkResponse("Opportunity deleted successfully");
    }

    [HttpGet("forecast")]
    [RequirePermission(Permissions.OpportunityView)]
    public async Task<ActionResult<ApiResponse<ForecastResponse>>> GetForecast(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow.AddMonths(3);

        var opportunities = await _db.Opportunities
            .AsNoTracking()
            .Where(o => o.Status == OpportunityStatus.Open)
            .Where(o => o.ExpectedCloseDate >= start && o.ExpectedCloseDate <= end)
            .ToListAsync();

        var forecast = new ForecastResponse
        {
            TotalPipeline = opportunities.Sum(o => o.Amount),
            WeightedPipeline = opportunities.Sum(o => o.WeightedAmount),
            OpportunityCount = opportunities.Count,
            ByMonth = opportunities
                .Where(o => o.ExpectedCloseDate.HasValue)
                .GroupBy(o => new { o.ExpectedCloseDate!.Value.Year, o.ExpectedCloseDate!.Value.Month })
                .Select(g => new MonthlyForecast
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Sum(o => o.Amount),
                    Weighted = g.Sum(o => o.WeightedAmount),
                    Count = g.Count()
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList()
        };

        return OkResponse(forecast);
    }

    private static OpportunityDetailResponse MapToDetailResponse(Opportunity o) => new()
    {
        Id = o.Id,
        Name = o.Name,
        CustomerId = o.CustomerId,
        CustomerName = o.Customer?.Name,
        PipelineId = o.PipelineId,
        PipelineName = o.Pipeline?.Name,
        StageId = o.StageId,
        StageName = o.Stage?.Name,
        Status = o.Status.ToString(),
        Amount = o.Amount,
        Currency = o.Currency,
        Probability = o.Probability,
        WeightedAmount = o.WeightedAmount,
        ExpectedCloseDate = o.ExpectedCloseDate,
        ActualCloseDate = o.ActualCloseDate,
        Type = o.Type.ToString(),
        Priority = o.Priority.ToString(),
        LossReason = o.LossReason,
        WinReason = o.WinReason,
        Description = o.Description,
        AssignedToUserId = o.AssignedToUserId,
        AssignedToUserName = o.AssignedToUser?.FullName,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt
    };
}

public class CreateOpportunityRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Guid? PrimaryContactId { get; set; }
    public Guid PipelineId { get; set; }
    public Guid StageId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public int? Probability { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public OpportunityType Type { get; set; } = OpportunityType.NewBusiness;
    public OpportunityPriority Priority { get; set; } = OpportunityPriority.Medium;
    public string? Description { get; set; }
    public Guid? AssignedToUserId { get; set; }
}

public class UpdateOpportunityRequest
{
    public string? Name { get; set; }
    public Guid? StageId { get; set; }
    public decimal? Amount { get; set; }
    public int? Probability { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public string? Description { get; set; }
}

public class MoveStageRequest
{
    public Guid StageId { get; set; }
    public string? Reason { get; set; }
}

public class OpportunityResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? StageName { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public int Probability { get; set; }
    public decimal WeightedAmount { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OpportunityDetailResponse : OpportunityResponse
{
    public Guid? CustomerId { get; set; }
    public Guid PipelineId { get; set; }
    public string? PipelineName { get; set; }
    public Guid StageId { get; set; }
    public DateTime? ActualCloseDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? LossReason { get; set; }
    public string? WinReason { get; set; }
    public string? Description { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ForecastResponse
{
    public decimal TotalPipeline { get; set; }
    public decimal WeightedPipeline { get; set; }
    public int OpportunityCount { get; set; }
    public List<MonthlyForecast> ByMonth { get; set; } = [];
}

public class MonthlyForecast
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Total { get; set; }
    public decimal Weighted { get; set; }
    public int Count { get; set; }
}
