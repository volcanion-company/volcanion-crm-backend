using Asp.Versioning;
using CrmSaas.Api.Authorization;
using CrmSaas.Api.Common;
using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class WorkflowsController : ControllerBase
{
    private readonly TenantDbContext _context;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        TenantDbContext context,
        ILogger<WorkflowsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all workflows
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.WorkflowView)]
    public async Task<IActionResult> GetWorkflows(
        [FromQuery] string? entityType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Workflows
                .Include(w => w.Rules.Where(r => r.IsActive))
                .AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(w => w.EntityType == entityType);
            }

            if (isActive.HasValue)
            {
                query = query.Where(w => w.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var workflows = await query
                .OrderBy(w => w.ExecutionOrder)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(ApiResponse.Ok(new
            {
                items = workflows,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows");
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve workflows"));
        }
    }

    /// <summary>
    /// Get workflow by ID
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission(Permissions.WorkflowView)]
    public async Task<IActionResult> GetWorkflow(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var workflow = await _context.Workflows
                .Include(w => w.Rules.Where(r => r.IsActive))
                    .ThenInclude(r => r.Actions.Where(a => a.IsActive))
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

            if (workflow == null)
            {
                return NotFound(ApiResponse.Fail("Workflow not found"));
            }

            return Ok(ApiResponse.Ok(workflow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow {WorkflowId}", id);
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve workflow"));
        }
    }

    /// <summary>
    /// Create new workflow
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.WorkflowCreate)]
    public async Task<IActionResult> CreateWorkflow(
        [FromBody] CreateWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workflow = new Workflow
            {
                Name = request.Name,
                Description = request.Description,
                EntityType = request.EntityType,
                TriggerType = request.TriggerType,
                TriggerFields = request.TriggerFields,
                ScheduleExpression = request.ScheduleExpression,
                IsActive = request.IsActive ?? true,
                ExecutionOrder = request.ExecutionOrder ?? 0,
                StopOnMatch = request.StopOnMatch ?? false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Workflows.Add(workflow);
            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetWorkflow),
                new { id = workflow.Id },
                ApiResponse.Ok(workflow, "Workflow created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow");
            return StatusCode(500, ApiResponse.Fail("Failed to create workflow"));
        }
    }

    /// <summary>
    /// Update workflow
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission(Permissions.WorkflowEdit)]
    public async Task<IActionResult> UpdateWorkflow(
        Guid id,
        [FromBody] UpdateWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workflow = await _context.Workflows.FindAsync(new object[] { id }, cancellationToken);
            if (workflow == null)
            {
                return NotFound(ApiResponse.Fail("Workflow not found"));
            }

            if (request.Name != null) workflow.Name = request.Name;
            if (request.Description != null) workflow.Description = request.Description;
            if (request.TriggerType.HasValue) workflow.TriggerType = request.TriggerType.Value;
            if (request.TriggerFields != null) workflow.TriggerFields = request.TriggerFields;
            if (request.ScheduleExpression != null) workflow.ScheduleExpression = request.ScheduleExpression;
            if (request.IsActive.HasValue) workflow.IsActive = request.IsActive.Value;
            if (request.ExecutionOrder.HasValue) workflow.ExecutionOrder = request.ExecutionOrder.Value;
            if (request.StopOnMatch.HasValue) workflow.StopOnMatch = request.StopOnMatch.Value;

            workflow.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ApiResponse.Ok(workflow, "Workflow updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow {WorkflowId}", id);
            return StatusCode(500, ApiResponse.Fail("Failed to update workflow"));
        }
    }

    /// <summary>
    /// Delete workflow
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission(Permissions.WorkflowDelete)]
    public async Task<IActionResult> DeleteWorkflow(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var workflow = await _context.Workflows.FindAsync(new object[] { id }, cancellationToken);
            if (workflow == null)
            {
                return NotFound(ApiResponse.Fail("Workflow not found"));
            }

            _context.Workflows.Remove(workflow);
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ApiResponse.Ok("Workflow deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow {WorkflowId}", id);
            return StatusCode(500, ApiResponse.Fail("Failed to delete workflow"));
        }
    }

    /// <summary>
    /// Toggle workflow active status
    /// </summary>
    [HttpPut("{id}/toggle")]
    [RequirePermission(Permissions.WorkflowEdit)]
    public async Task<IActionResult> ToggleWorkflow(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var workflow = await _context.Workflows.FindAsync(new object[] { id }, cancellationToken);
            if (workflow == null)
            {
                return NotFound(ApiResponse.Fail("Workflow not found"));
            }

            workflow.IsActive = !workflow.IsActive;
            workflow.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ApiResponse.Ok(workflow, $"Workflow {(workflow.IsActive ? "activated" : "deactivated")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling workflow {WorkflowId}", id);
            return StatusCode(500, ApiResponse.Fail("Failed to toggle workflow"));
        }
    }

    /// <summary>
    /// Get workflow execution logs
    /// </summary>
    [HttpGet("{id}/logs")]
    [RequirePermission(Permissions.WorkflowView)]
    public async Task<IActionResult> GetWorkflowLogs(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.WorkflowExecutionLogs
                .Where(l => l.WorkflowId == id);

            var totalCount = await query.CountAsync(cancellationToken);
            var logs = await query
                .OrderByDescending(l => l.ExecutedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(ApiResponse.Ok(new
            {
                items = logs,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow logs");
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve logs"));
        }
    }
}

#region Request Models

public class CreateWorkflowRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public WorkflowTriggerType TriggerType { get; set; }
    public string? TriggerFields { get; set; }
    public string? ScheduleExpression { get; set; }
    public bool? IsActive { get; set; }
    public int? ExecutionOrder { get; set; }
    public bool? StopOnMatch { get; set; }
}

public class UpdateWorkflowRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public WorkflowTriggerType? TriggerType { get; set; }
    public string? TriggerFields { get; set; }
    public string? ScheduleExpression { get; set; }
    public bool? IsActive { get; set; }
    public int? ExecutionOrder { get; set; }
    public bool? StopOnMatch { get; set; }
}

#endregion
