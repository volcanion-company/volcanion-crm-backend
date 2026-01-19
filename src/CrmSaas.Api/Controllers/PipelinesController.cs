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
public class PipelinesController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public PipelinesController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [HttpGet]
    [RequirePermission(Permissions.PipelineView)]
    public async Task<ActionResult<ApiResponse<List<PipelineResponse>>>> GetAll()
    {
        var pipelines = await _db.Pipelines
            .AsNoTracking()
            .Include(p => p.Stages.OrderBy(s => s.SortOrder))
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var response = pipelines.Select(p => new PipelineResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            IsDefault = p.IsDefault,
            IsActive = p.IsActive,
            Stages = p.Stages.Select(s => new PipelineStageResponse
            {
                Id = s.Id,
                Name = s.Name,
                SortOrder = s.SortOrder,
                Probability = s.Probability,
                Color = s.Color,
                IsWon = s.IsWon,
                IsLost = s.IsLost
            }).ToList()
        }).ToList();

        return OkResponse(response);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.PipelineView)]
    public async Task<ActionResult<ApiResponse<PipelineResponse>>> GetById(Guid id)
    {
        var pipeline = await _db.Pipelines
            .AsNoTracking()
            .Include(p => p.Stages.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pipeline == null)
        {
            return NotFoundResponse<PipelineResponse>($"Pipeline with id {id} not found");
        }

        return OkResponse(new PipelineResponse
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            Description = pipeline.Description,
            IsDefault = pipeline.IsDefault,
            IsActive = pipeline.IsActive,
            Stages = pipeline.Stages.Select(s => new PipelineStageResponse
            {
                Id = s.Id,
                Name = s.Name,
                SortOrder = s.SortOrder,
                Probability = s.Probability,
                Color = s.Color,
                IsWon = s.IsWon,
                IsLost = s.IsLost
            }).ToList()
        });
    }

    [HttpPost]
    [RequirePermission(Permissions.PipelineCreate)]
    public async Task<ActionResult<ApiResponse<PipelineResponse>>> Create([FromBody] CreatePipelineRequest request)
    {
        if (request.IsDefault)
        {
            // Remove default from other pipelines
            var existingDefault = await _db.Pipelines.Where(p => p.IsDefault).ToListAsync();
            foreach (var p in existingDefault)
            {
                p.IsDefault = false;
            }
        }

        var pipeline = new Pipeline
        {
            Name = request.Name,
            Description = request.Description,
            IsDefault = request.IsDefault,
            IsActive = true,
            CreatedBy = _currentUser.UserId
        };

        // Add stages
        var sortOrder = 0;
        foreach (var stageRequest in request.Stages)
        {
            pipeline.Stages.Add(new PipelineStage
            {
                Name = stageRequest.Name,
                SortOrder = sortOrder++,
                Probability = stageRequest.Probability,
                Color = stageRequest.Color,
                IsWon = stageRequest.IsWon,
                IsLost = stageRequest.IsLost,
                CreatedBy = _currentUser.UserId
            });
        }

        _db.Pipelines.Add(pipeline);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Create, nameof(Pipeline), pipeline.Id, pipeline.Name);

        return CreatedResponse(new PipelineResponse
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            Description = pipeline.Description,
            IsDefault = pipeline.IsDefault,
            IsActive = pipeline.IsActive,
            Stages = pipeline.Stages.Select(s => new PipelineStageResponse
            {
                Id = s.Id,
                Name = s.Name,
                SortOrder = s.SortOrder,
                Probability = s.Probability,
                Color = s.Color,
                IsWon = s.IsWon,
                IsLost = s.IsLost
            }).ToList()
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.PipelineUpdate)]
    public async Task<ActionResult<ApiResponse<PipelineResponse>>> Update(Guid id, [FromBody] UpdatePipelineRequest request)
    {
        var pipeline = await _db.Pipelines
            .Include(p => p.Stages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pipeline == null)
        {
            return NotFoundResponse<PipelineResponse>($"Pipeline with id {id} not found");
        }

        pipeline.Name = request.Name ?? pipeline.Name;
        pipeline.Description = request.Description ?? pipeline.Description;
        pipeline.UpdatedBy = _currentUser.UserId;

        if (request.IsDefault == true && !pipeline.IsDefault)
        {
            var existingDefault = await _db.Pipelines.Where(p => p.IsDefault && p.Id != id).ToListAsync();
            foreach (var p in existingDefault)
            {
                p.IsDefault = false;
            }
            pipeline.IsDefault = true;
        }

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Pipeline), pipeline.Id, pipeline.Name);

        return OkResponse(new PipelineResponse
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            Description = pipeline.Description,
            IsDefault = pipeline.IsDefault,
            IsActive = pipeline.IsActive
        });
    }

    [HttpPost("{pipelineId:guid}/stages")]
    [RequirePermission(Permissions.PipelineUpdate)]
    public async Task<ActionResult<ApiResponse<PipelineStageResponse>>> AddStage(Guid pipelineId, [FromBody] CreateStageRequest request)
    {
        var pipeline = await _db.Pipelines
            .Include(p => p.Stages)
            .FirstOrDefaultAsync(p => p.Id == pipelineId);

        if (pipeline == null)
        {
            return NotFoundResponse<PipelineStageResponse>($"Pipeline with id {pipelineId} not found");
        }

        var maxSortOrder = pipeline.Stages.Any() ? pipeline.Stages.Max(s => s.SortOrder) : -1;

        var stage = new PipelineStage
        {
            PipelineId = pipelineId,
            Name = request.Name,
            SortOrder = request.SortOrder ?? maxSortOrder + 1,
            Probability = request.Probability,
            Color = request.Color,
            IsWon = request.IsWon,
            IsLost = request.IsLost,
            CreatedBy = _currentUser.UserId
        };

        _db.PipelineStages.Add(stage);
        await _db.SaveChangesAsync();

        return CreatedResponse(new PipelineStageResponse
        {
            Id = stage.Id,
            Name = stage.Name,
            SortOrder = stage.SortOrder,
            Probability = stage.Probability,
            Color = stage.Color,
            IsWon = stage.IsWon,
            IsLost = stage.IsLost
        });
    }

    [HttpPut("stages/{stageId:guid}")]
    [RequirePermission(Permissions.PipelineUpdate)]
    public async Task<ActionResult<ApiResponse<PipelineStageResponse>>> UpdateStage(Guid stageId, [FromBody] UpdateStageRequest request)
    {
        var stage = await _db.PipelineStages.FindAsync(stageId);

        if (stage == null)
        {
            return NotFoundResponse<PipelineStageResponse>($"Stage with id {stageId} not found");
        }

        stage.Name = request.Name ?? stage.Name;
        stage.SortOrder = request.SortOrder ?? stage.SortOrder;
        stage.Probability = request.Probability ?? stage.Probability;
        stage.Color = request.Color ?? stage.Color;
        stage.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        return OkResponse(new PipelineStageResponse
        {
            Id = stage.Id,
            Name = stage.Name,
            SortOrder = stage.SortOrder,
            Probability = stage.Probability,
            Color = stage.Color,
            IsWon = stage.IsWon,
            IsLost = stage.IsLost
        });
    }

    [HttpDelete("stages/{stageId:guid}")]
    [RequirePermission(Permissions.PipelineDelete)]
    public async Task<ActionResult<ApiResponse>> DeleteStage(Guid stageId)
    {
        var stage = await _db.PipelineStages
            .Include(s => s.Opportunities)
            .FirstOrDefaultAsync(s => s.Id == stageId);

        if (stage == null)
        {
            return NotFoundResponse($"Stage with id {stageId} not found");
        }

        if (stage.Opportunities.Any())
        {
            return BadRequestResponse("Cannot delete stage with existing opportunities");
        }

        _db.PipelineStages.Remove(stage);
        await _db.SaveChangesAsync();

        return OkResponse("Stage deleted successfully");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.PipelineDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var pipeline = await _db.Pipelines
            .Include(p => p.Stages)
                .ThenInclude(s => s.Opportunities)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pipeline == null)
        {
            return NotFoundResponse($"Pipeline with id {id} not found");
        }

        if (pipeline.IsDefault)
        {
            return BadRequestResponse("Cannot delete default pipeline");
        }

        if (pipeline.Stages.Any(s => s.Opportunities.Any()))
        {
            return BadRequestResponse("Cannot delete pipeline with existing opportunities");
        }

        pipeline.IsActive = false;
        pipeline.IsDeleted = true;
        pipeline.DeletedAt = DateTime.UtcNow;
        pipeline.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.SoftDelete, nameof(Pipeline), pipeline.Id, pipeline.Name);

        return OkResponse("Pipeline deleted successfully");
    }
}

public class CreatePipelineRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public List<CreateStageRequest> Stages { get; set; } = [];
}

public class UpdatePipelineRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsDefault { get; set; }
}

public class CreateStageRequest
{
    public string Name { get; set; } = string.Empty;
    public int? SortOrder { get; set; }
    public int Probability { get; set; }
    public string? Color { get; set; }
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }
}

public class UpdateStageRequest
{
    public string? Name { get; set; }
    public int? SortOrder { get; set; }
    public int? Probability { get; set; }
    public string? Color { get; set; }
}

public class PipelineResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public List<PipelineStageResponse> Stages { get; set; } = [];
}

public class PipelineStageResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Probability { get; set; }
    public string? Color { get; set; }
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }
}
