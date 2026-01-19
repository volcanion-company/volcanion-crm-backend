using CrmSaas.Api.Entities;
using CrmSaas.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CrmSaas.Api.Services;

public interface IWorkflowEngine
{
    Task ProcessWorkflowsAsync(object entity, WorkflowTriggerType triggerType, object? oldEntity = null, CancellationToken cancellationToken = default);
    Task<List<Workflow>> GetApplicableWorkflowsAsync(string entityType, WorkflowTriggerType triggerType, CancellationToken cancellationToken = default);
}

public class WorkflowEngine : IWorkflowEngine
{
    private readonly TenantDbContext _context;
    private readonly IWorkflowConditionEvaluator _conditionEvaluator;
    private readonly IWorkflowActionExecutor _actionExecutor;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(
        TenantDbContext context,
        IWorkflowConditionEvaluator conditionEvaluator,
        IWorkflowActionExecutor actionExecutor,
        ILogger<WorkflowEngine> logger)
    {
        _context = context;
        _conditionEvaluator = conditionEvaluator;
        _actionExecutor = actionExecutor;
        _logger = logger;
    }

    public async Task ProcessWorkflowsAsync(
        object entity,
        WorkflowTriggerType triggerType,
        object? oldEntity = null,
        CancellationToken cancellationToken = default)
    {
        var entityType = entity.GetType().Name;

        try
        {
            // Get applicable workflows
            var workflows = await GetApplicableWorkflowsAsync(entityType, triggerType, cancellationToken);

            if (workflows.Count == 0)
            {
                _logger.LogDebug("No workflows found for {EntityType} with trigger {TriggerType}", 
                    entityType, triggerType);
                return;
            }

            _logger.LogInformation("Processing {Count} workflows for {EntityType} with trigger {TriggerType}",
                workflows.Count, entityType, triggerType);

            // Process each workflow in order
            foreach (var workflow in workflows.OrderBy(w => w.ExecutionOrder))
            {
                var shouldStop = await ProcessWorkflowAsync(workflow, entity, oldEntity, cancellationToken);
                
                if (shouldStop && workflow.StopOnMatch)
                {
                    _logger.LogInformation("Workflow {WorkflowId} matched and set to stop processing", workflow.Id);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing workflows for {EntityType}", entityType);
        }
    }

    public async Task<List<Workflow>> GetApplicableWorkflowsAsync(
        string entityType,
        WorkflowTriggerType triggerType,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Workflow>()
            .Include(w => w.Rules.Where(r => r.IsActive))
                .ThenInclude(r => r.Actions.Where(a => a.IsActive))
            .Where(w => w.IsActive &&
                       w.EntityType == entityType &&
                       w.TriggerType == triggerType)
            .OrderBy(w => w.ExecutionOrder)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> ProcessWorkflowAsync(
        Workflow workflow,
        object entity,
        object? oldEntity,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var entityId = GetEntityId(entity);
        var matched = false;

        try
        {
            _logger.LogDebug("Processing workflow {WorkflowId} - {WorkflowName}", workflow.Id, workflow.Name);

            // Check trigger field filter (for OnUpdate trigger)
            if (workflow.TriggerType == WorkflowTriggerType.OnUpdate && 
                !string.IsNullOrEmpty(workflow.TriggerFields))
            {
                if (!HasMonitoredFieldChanged(entity, oldEntity, workflow.TriggerFields))
                {
                    await LogWorkflowExecutionAsync(workflow.Id, null, null, entity, 
                        WorkflowExecutionStatus.Skipped, "Monitored fields not changed", 
                        stopwatch.ElapsedMilliseconds, cancellationToken);
                    return false;
                }
            }

            // Process each rule
            foreach (var rule in workflow.Rules.Where(r => r.IsActive).OrderBy(r => r.Order))
            {
                var ruleMatched = await ProcessRuleAsync(workflow, rule, entity, oldEntity, stopwatch, cancellationToken);
                if (ruleMatched)
                {
                    matched = true;
                }
            }

            if (!matched)
            {
                await LogWorkflowExecutionAsync(workflow.Id, null, null, entity,
                    WorkflowExecutionStatus.Skipped, "No rules matched",
                    stopwatch.ElapsedMilliseconds, cancellationToken);
            }

            return matched;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing workflow {WorkflowId}", workflow.Id);
            
            await LogWorkflowExecutionAsync(workflow.Id, null, null, entity,
                WorkflowExecutionStatus.Failed, $"Error: {ex.Message}",
                stopwatch.ElapsedMilliseconds, cancellationToken);

            return false;
        }
    }

    private async Task<bool> ProcessRuleAsync(
        Workflow workflow,
        WorkflowRule rule,
        object entity,
        object? oldEntity,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        try
        {
            // Evaluate rule conditions
            var conditionsMet = _conditionEvaluator.EvaluateConditions(
                rule.Conditions, entity, rule.ConditionLogic);

            if (!conditionsMet)
            {
                _logger.LogDebug("Rule {RuleId} conditions not met", rule.Id);
                return false;
            }

            _logger.LogInformation("Rule {RuleId} - {RuleName} matched, executing {ActionCount} actions",
                rule.Id, rule.Name, rule.Actions.Count);

            // Execute actions
            foreach (var action in rule.Actions.Where(a => a.IsActive).OrderBy(a => a.Order))
            {
                await ProcessActionAsync(workflow, rule, action, entity, stopwatch, cancellationToken);
            }

            await LogWorkflowExecutionAsync(workflow.Id, rule.Id, null, entity,
                WorkflowExecutionStatus.Success, $"Rule matched, executed {rule.Actions.Count} actions",
                stopwatch.ElapsedMilliseconds, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing rule {RuleId}", rule.Id);
            
            await LogWorkflowExecutionAsync(workflow.Id, rule.Id, null, entity,
                WorkflowExecutionStatus.Failed, $"Error: {ex.Message}",
                stopwatch.ElapsedMilliseconds, cancellationToken);

            return false;
        }
    }

    private async Task ProcessActionAsync(
        Workflow workflow,
        WorkflowRule rule,
        WorkflowAction action,
        object entity,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Executing action {ActionId} of type {ActionType}", 
                action.Id, action.ActionType);

            // Apply delay if specified
            if (action.DelayMinutes > 0)
            {
                _logger.LogInformation("Action {ActionId} has delay of {Minutes} minutes - skipping for now",
                    action.Id, action.DelayMinutes);
                
                // TODO: Queue delayed action for background job processing
                await LogWorkflowExecutionAsync(workflow.Id, rule.Id, action.Id, entity,
                    WorkflowExecutionStatus.Pending, $"Action queued with {action.DelayMinutes}min delay",
                    stopwatch.ElapsedMilliseconds, cancellationToken);
                return;
            }

            // Execute action
            var result = await _actionExecutor.ExecuteActionAsync(action, entity, cancellationToken);

            await LogWorkflowExecutionAsync(workflow.Id, rule.Id, action.Id, entity,
                result.IsSuccess ? WorkflowExecutionStatus.Success : WorkflowExecutionStatus.Failed,
                result.Message ?? "Action executed",
                stopwatch.ElapsedMilliseconds, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Action {ActionId} failed: {Message}", action.Id, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action {ActionId}", action.Id);
            
            await LogWorkflowExecutionAsync(workflow.Id, rule.Id, action.Id, entity,
                WorkflowExecutionStatus.Failed, $"Error: {ex.Message}",
                stopwatch.ElapsedMilliseconds, cancellationToken);
        }
    }

    private bool HasMonitoredFieldChanged(object entity, object? oldEntity, string triggerFieldsJson)
    {
        if (oldEntity == null) return true; // New entity = all fields "changed"

        try
        {
            var fields = System.Text.Json.JsonSerializer.Deserialize<List<string>>(triggerFieldsJson);
            if (fields == null || fields.Count == 0) return true;

            foreach (var field in fields)
            {
                var newValue = GetPropertyValue(entity, field);
                var oldValue = GetPropertyValue(oldEntity, field);

                if (!Equals(newValue, oldValue))
                {
                    _logger.LogDebug("Monitored field {Field} changed from {OldValue} to {NewValue}",
                        field, oldValue, newValue);
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return true; // Error parsing = allow execution
        }
    }

    private object? GetPropertyValue(object obj, string propertyName)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        return prop?.GetValue(obj);
    }

    private Guid GetEntityId(object entity)
    {
        var idProp = entity.GetType().GetProperty("Id");
        if (idProp != null && idProp.PropertyType == typeof(Guid))
        {
            return (Guid)(idProp.GetValue(entity) ?? Guid.Empty);
        }
        return Guid.Empty;
    }

    private async Task LogWorkflowExecutionAsync(
        Guid workflowId,
        Guid? ruleId,
        Guid? actionId,
        object entity,
        WorkflowExecutionStatus status,
        string? message,
        long durationMs,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = new WorkflowExecutionLog
            {
                WorkflowId = workflowId,
                WorkflowRuleId = ruleId,
                WorkflowActionId = actionId,
                EntityType = entity.GetType().Name,
                EntityId = GetEntityId(entity),
                Status = status,
                ErrorMessage = status == WorkflowExecutionStatus.Failed ? message : null,
                ExecutionDetails = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Message = message,
                    Timestamp = DateTime.UtcNow
                }),
                DurationMs = (int)durationMs,
                ExecutedAt = DateTime.UtcNow
            };

            _context.Set<WorkflowExecutionLog>().Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging workflow execution");
        }
    }
}
