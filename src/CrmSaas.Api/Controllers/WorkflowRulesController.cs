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
[Route("api/v{version:apiVersion}/workflows/{workflowId}/rules")]
public class WorkflowRulesController : ControllerBase
{
    private readonly TenantDbContext _context;
    private readonly ILogger<WorkflowRulesController> _logger;

    public WorkflowRulesController(
        TenantDbContext context,
        ILogger<WorkflowRulesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all rules for a workflow
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.WorkflowView)]
    public async Task<IActionResult> GetRules(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workflow = await _context.Workflows.FindAsync(new object[] { workflowId }, cancellationToken);
            if (workflow == null)
            {
                return NotFound(ApiResponse.Fail("Workflow not found"));
            }

            var rules = await _context.WorkflowRules
                .Include(r => r.Actions.Where(a => a.IsActive))
                .Where(r => r.WorkflowId == workflowId)
                .OrderBy(r => r.Order)
                .ToListAsync(cancellationToken);

            return Ok(ApiResponse.Ok(rules));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow rules");
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve rules"));
        }
    }

    /// <summary>
    /// Get rule by ID
    /// </summary>
    [HttpGet("{ruleId}")]
    [RequirePermission(Permissions.WorkflowView)]
    public async Task<IActionResult> GetRule(
        Guid workflowId,
        Guid ruleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rule = await _context.WorkflowRules
                .Include(r => r.Actions.Where(a => a.IsActive))
                .FirstOrDefaultAsync(r => r.Id == ruleId && r.WorkflowId == workflowId, cancellationToken);

            if (rule == null)
            {
                return NotFound(ApiResponse.Fail("Rule not found"));
            }

            return Ok(ApiResponse.Ok(rule));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rule {RuleId}", ruleId);
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve rule"));
        }
    }

    /// <summary>
    /// Create new rule
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.WorkflowCreate)]
    public async Task<IActionResult> CreateRule(
        Guid workflowId,
        [FromBody] CreateWorkflowRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workflow = await _context.Workflows.FindAsync(new object[] { workflowId }, cancellationToken);
            if (workflow == null)
            {
                return NotFound(ApiResponse.Fail("Workflow not found"));
            }

            var rule = new WorkflowRule
            {
                WorkflowId = workflowId,
                Name = request.Name,
                Description = request.Description,
                Conditions = request.Conditions,
                ConditionLogic = request.ConditionLogic ?? ConditionLogic.And,
                IsActive = request.IsActive ?? true,
                Order = request.Order ?? 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkflowRules.Add(rule);
            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetRule),
                new { workflowId, ruleId = rule.Id },
                ApiResponse.Ok(rule, "Rule created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rule");
            return StatusCode(500, ApiResponse.Fail("Failed to create rule"));
        }
    }

    /// <summary>
    /// Update rule
    /// </summary>
    [HttpPut("{ruleId}")]
    [RequirePermission(Permissions.WorkflowEdit)]
    public async Task<IActionResult> UpdateRule(
        Guid workflowId,
        Guid ruleId,
        [FromBody] UpdateWorkflowRuleRequest request,
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

            if (request.Name != null) rule.Name = request.Name;
            if (request.Description != null) rule.Description = request.Description;
            if (request.Conditions != null) rule.Conditions = request.Conditions;
            if (request.ConditionLogic.HasValue) rule.ConditionLogic = request.ConditionLogic.Value;
            if (request.IsActive.HasValue) rule.IsActive = request.IsActive.Value;
            if (request.Order.HasValue) rule.Order = request.Order.Value;

            rule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ApiResponse.Ok(rule, "Rule updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rule {RuleId}", ruleId);
            return StatusCode(500, ApiResponse.Fail("Failed to update rule"));
        }
    }

    /// <summary>
    /// Delete rule
    /// </summary>
    [HttpDelete("{ruleId}")]
    [RequirePermission(Permissions.WorkflowDelete)]
    public async Task<IActionResult> DeleteRule(
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

            _context.WorkflowRules.Remove(rule);
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ApiResponse.Ok("Rule deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rule {RuleId}", ruleId);
            return StatusCode(500, ApiResponse.Fail("Failed to delete rule"));
        }
    }

    /// <summary>
    /// Toggle rule active status
    /// </summary>
    [HttpPut("{ruleId}/toggle")]
    [RequirePermission(Permissions.WorkflowEdit)]
    public async Task<IActionResult> ToggleRule(
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

            rule.IsActive = !rule.IsActive;
            rule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ApiResponse.Ok(rule, $"Rule {(rule.IsActive ? "activated" : "deactivated")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling rule {RuleId}", ruleId);
            return StatusCode(500, ApiResponse.Fail("Failed to toggle rule"));
        }
    }
}

#region Request Models

public class CreateWorkflowRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Conditions { get; set; }
    public ConditionLogic? ConditionLogic { get; set; }
    public bool? IsActive { get; set; }
    public int? Order { get; set; }
}

public class UpdateWorkflowRuleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Conditions { get; set; }
    public ConditionLogic? ConditionLogic { get; set; }
    public bool? IsActive { get; set; }
    public int? Order { get; set; }
}

#endregion
