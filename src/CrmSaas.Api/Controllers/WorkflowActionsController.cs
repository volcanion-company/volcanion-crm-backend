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
[Route("api/v{version:apiVersion}/workflows/{workflowId}/rules/{ruleId}/actions")]
public class WorkflowActionsController : ControllerBase
{
    private readonly TenantDbContext _context;
    private readonly ILogger<WorkflowActionsController> _logger;

    public WorkflowActionsController(
        TenantDbContext context,
        ILogger<WorkflowActionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all actions for a rule
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.WorkflowView)]
    public async Task<IActionResult> GetActions(
        Guid workflowId,
        Guid ruleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rule = await _context.WorkflowRules
                .FirstOrDefaultAsync(r => r.Id == ruleId && r.WorkflowId == workflowId, cancellationToken);

            if (rule == null)
            {
                return NotFound(ApiResponse.Fail("Rule not found"));
            }

            var actions = await _context.WorkflowActions
                .Where(a => a.WorkflowRuleId == ruleId)
                .OrderBy(a => a.Order)
                .ToListAsync(cancellationToken);

            return Ok(ApiResponse.Ok(actions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow actions");
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve actions"));
        }
    }

    /// <summary>
    /// Get action by ID
    /// </summary>
    [HttpGet("{actionId}")]
    [RequirePermission(Permissions.WorkflowView)]
    public async Task<IActionResult> GetAction(
        Guid workflowId,
        Guid ruleId,
        Guid actionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var action = await _context.WorkflowActions
                .Include(a => a.WorkflowRule)
                .FirstOrDefaultAsync(
                    a => a.Id == actionId && 
                    a.WorkflowRuleId == ruleId && 
                    a.WorkflowRule!.WorkflowId == workflowId,
                    cancellationToken);

            if (action == null)
            {
                return NotFound(ApiResponse.Fail("Action not found"));
            }

            return Ok(ApiResponse.Ok(action));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting action {ActionId}", actionId);
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve action"));
        }
    }

    /// <summary>
    /// Create new action
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.WorkflowCreate)]
    public async Task<IActionResult> CreateAction(
        Guid workflowId,
        Guid ruleId,
        [FromBody] CreateWorkflowActionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rule = await _context.WorkflowRules
                .FirstOrDefaultAsync(r => r.Id == ruleId && r.WorkflowId == workflowId, cancellationToken);

            if (rule == null)
            {
                return NotFound(ApiResponse.Fail("Rule not found"));
            }

            var action = new WorkflowAction
            {
                WorkflowRuleId = ruleId,
                ActionType = request.ActionType,
                ActionConfig = request.ActionConfig ?? "{}",
                DelayMinutes = request.DelayMinutes ?? 0,
                IsActive = request.IsActive ?? true,
                Order = request.Order ?? 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkflowActions.Add(action);
            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetAction),
                new { workflowId, ruleId, actionId = action.Id },
                ApiResponse.Ok(action, "Action created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating action");
            return StatusCode(500, ApiResponse.Fail("Failed to create action"));
        }
    }

    /// <summary>
    /// Update action
    /// </summary>
    [HttpPut("{actionId}")]
    [RequirePermission(Permissions.WorkflowEdit)]
    public async Task<IActionResult> UpdateAction(
        Guid workflowId,
        Guid ruleId,
        Guid actionId,
        [FromBody] UpdateWorkflowActionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var action = await _context.WorkflowActions
                .Include(a => a.WorkflowRule)
                .FirstOrDefaultAsync(
                    a => a.Id == actionId &&
                    a.WorkflowRuleId == ruleId &&
                    a.WorkflowRule!.WorkflowId == workflowId,
                    cancellationToken);

            if (action == null)
            {
                return NotFound(ApiResponse.Fail("Action not found"));
            }

            if (request.ActionType.HasValue) action.ActionType = request.ActionType.Value;
            if (request.ActionConfig != null) action.ActionConfig = request.ActionConfig;
            if (request.DelayMinutes.HasValue) action.DelayMinutes = request.DelayMinutes.Value;
            if (request.IsActive.HasValue) action.IsActive = request.IsActive.Value;
            if (request.Order.HasValue) action.Order = request.Order.Value;

            action.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ApiResponse.Ok(action, "Action updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating action {ActionId}", actionId);
            return StatusCode(500, ApiResponse.Fail("Failed to update action"));
        }
    }

    /// <summary>
    /// Delete action
    /// </summary>
    [HttpDelete("{actionId}")]
    [RequirePermission(Permissions.WorkflowDelete)]
    public async Task<IActionResult> DeleteAction(
        Guid workflowId,
        Guid ruleId,
        Guid actionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var action = await _context.WorkflowActions
                .Include(a => a.WorkflowRule)
                .FirstOrDefaultAsync(
                    a => a.Id == actionId &&
                    a.WorkflowRuleId == ruleId &&
                    a.WorkflowRule!.WorkflowId == workflowId,
                    cancellationToken);

            if (action == null)
            {
                return NotFound(ApiResponse.Fail("Action not found"));
            }

            _context.WorkflowActions.Remove(action);
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ApiResponse.Ok("Action deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting action {ActionId}", actionId);
            return StatusCode(500, ApiResponse.Fail("Failed to delete action"));
        }
    }

    /// <summary>
    /// Toggle action active status
    /// </summary>
    [HttpPut("{actionId}/toggle")]
    [RequirePermission(Permissions.WorkflowEdit)]
    public async Task<IActionResult> ToggleAction(
        Guid workflowId,
        Guid ruleId,
        Guid actionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var action = await _context.WorkflowActions
                .Include(a => a.WorkflowRule)
                .FirstOrDefaultAsync(
                    a => a.Id == actionId &&
                    a.WorkflowRuleId == ruleId &&
                    a.WorkflowRule!.WorkflowId == workflowId,
                    cancellationToken);

            if (action == null)
            {
                return NotFound(ApiResponse.Fail("Action not found"));
            }

            action.IsActive = !action.IsActive;
            action.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ApiResponse.Ok(action, $"Action {(action.IsActive ? "activated" : "deactivated")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling action {ActionId}", actionId);
            return StatusCode(500, ApiResponse.Fail("Failed to toggle action"));
        }
    }
}

#region Request Models

public class CreateWorkflowActionRequest
{
    public WorkflowActionType ActionType { get; set; }
    public string? ActionConfig { get; set; }
    public int? DelayMinutes { get; set; }
    public bool? IsActive { get; set; }
    public int? Order { get; set; }
}

public class UpdateWorkflowActionRequest
{
    public WorkflowActionType? ActionType { get; set; }
    public string? ActionConfig { get; set; }
    public int? DelayMinutes { get; set; }
    public bool? IsActive { get; set; }
    public int? Order { get; set; }
}

#endregion
