using CrmSaas.Api.Entities;
using System.Text.Json;
using System.Reflection;

namespace CrmSaas.Api.Services;

public interface IWorkflowConditionEvaluator
{
    bool EvaluateConditions(string? conditionsJson, object entity, ConditionLogic logic);
    bool EvaluateCondition(WorkflowCondition condition, object entity, object? oldEntity = null);
}

public class WorkflowConditionEvaluator : IWorkflowConditionEvaluator
{
    public bool EvaluateConditions(string? conditionsJson, object entity, ConditionLogic logic)
    {
        if (string.IsNullOrWhiteSpace(conditionsJson))
            return true; // No conditions = always match

        try
        {
            var conditions = JsonSerializer.Deserialize<List<WorkflowCondition>>(conditionsJson);
            if (conditions == null || conditions.Count == 0)
                return true;

            return logic == ConditionLogic.And
                ? conditions.All(c => EvaluateCondition(c, entity))
                : conditions.Any(c => EvaluateCondition(c, entity));
        }
        catch
        {
            return false; // Invalid JSON or evaluation error = no match
        }
    }

    public bool EvaluateCondition(WorkflowCondition condition, object entity, object? oldEntity = null)
    {
        try
        {
            var fieldValue = GetFieldValue(entity, condition.Field);
            var conditionValue = condition.Value;

            return condition.Operator switch
            {
                ConditionOperator.Equals => AreEqual(fieldValue, conditionValue),
                ConditionOperator.NotEquals => !AreEqual(fieldValue, conditionValue),
                ConditionOperator.Contains => Contains(fieldValue, conditionValue),
                ConditionOperator.NotContains => !Contains(fieldValue, conditionValue),
                ConditionOperator.StartsWith => StartsWith(fieldValue, conditionValue),
                ConditionOperator.EndsWith => EndsWith(fieldValue, conditionValue),
                ConditionOperator.GreaterThan => IsGreaterThan(fieldValue, conditionValue),
                ConditionOperator.GreaterThanOrEqual => IsGreaterThanOrEqual(fieldValue, conditionValue),
                ConditionOperator.LessThan => IsLessThan(fieldValue, conditionValue),
                ConditionOperator.LessThanOrEqual => IsLessThanOrEqual(fieldValue, conditionValue),
                ConditionOperator.IsNull => fieldValue == null,
                ConditionOperator.IsNotNull => fieldValue != null,
                ConditionOperator.In => IsIn(fieldValue, conditionValue),
                ConditionOperator.NotIn => !IsIn(fieldValue, conditionValue),
                ConditionOperator.Between => IsBetween(fieldValue, conditionValue),
                ConditionOperator.Changed => HasChanged(entity, oldEntity, condition.Field),
                ConditionOperator.ChangedTo => HasChangedTo(entity, oldEntity, condition.Field, conditionValue),
                ConditionOperator.ChangedFrom => HasChangedFrom(entity, oldEntity, condition.Field, conditionValue),
                _ => false
            };
        }
        catch
        {
            return false; // Evaluation error = no match
        }
    }

    private object? GetFieldValue(object entity, string fieldPath)
    {
        var parts = fieldPath.Split('.');
        object? current = entity;

        foreach (var part in parts)
        {
            if (current == null) return null;

            var prop = current.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return null;

            current = prop.GetValue(current);
        }

        return current;
    }

    private bool AreEqual(object? fieldValue, string? conditionValue)
    {
        if (fieldValue == null && conditionValue == null) return true;
        if (fieldValue == null || conditionValue == null) return false;

        // Try convert to same type for comparison
        var fieldStr = fieldValue.ToString();
        return string.Equals(fieldStr, conditionValue, StringComparison.OrdinalIgnoreCase);
    }

    private bool Contains(object? fieldValue, string? conditionValue)
    {
        if (fieldValue == null || conditionValue == null) return false;
        return fieldValue.ToString()?.Contains(conditionValue, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private bool StartsWith(object? fieldValue, string? conditionValue)
    {
        if (fieldValue == null || conditionValue == null) return false;
        return fieldValue.ToString()?.StartsWith(conditionValue, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private bool EndsWith(object? fieldValue, string? conditionValue)
    {
        if (fieldValue == null || conditionValue == null) return false;
        return fieldValue.ToString()?.EndsWith(conditionValue, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private bool IsGreaterThan(object? fieldValue, string? conditionValue)
    {
        return CompareNumeric(fieldValue, conditionValue) > 0;
    }

    private bool IsGreaterThanOrEqual(object? fieldValue, string? conditionValue)
    {
        return CompareNumeric(fieldValue, conditionValue) >= 0;
    }

    private bool IsLessThan(object? fieldValue, string? conditionValue)
    {
        return CompareNumeric(fieldValue, conditionValue) < 0;
    }

    private bool IsLessThanOrEqual(object? fieldValue, string? conditionValue)
    {
        return CompareNumeric(fieldValue, conditionValue) <= 0;
    }

    private int CompareNumeric(object? fieldValue, string? conditionValue)
    {
        if (fieldValue == null || conditionValue == null) return 0;

        // Try parse as decimal for comparison
        if (decimal.TryParse(fieldValue.ToString(), out var fieldDecimal) &&
            decimal.TryParse(conditionValue, out var conditionDecimal))
        {
            return fieldDecimal.CompareTo(conditionDecimal);
        }

        // Try as DateTime
        if (DateTime.TryParse(fieldValue.ToString(), out var fieldDate) &&
            DateTime.TryParse(conditionValue, out var conditionDate))
        {
            return fieldDate.CompareTo(conditionDate);
        }

        return 0;
    }

    private bool IsIn(object? fieldValue, string? conditionValue)
    {
        if (fieldValue == null || conditionValue == null) return false;

        // conditionValue should be comma-separated list: "Active,New,Open"
        var values = conditionValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var fieldStr = fieldValue.ToString();

        return values.Any(v => string.Equals(v, fieldStr, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsBetween(object? fieldValue, string? conditionValue)
    {
        if (fieldValue == null || conditionValue == null) return false;

        // conditionValue format: "min,max"
        var parts = conditionValue.Split(',');
        if (parts.Length != 2) return false;

        if (decimal.TryParse(fieldValue.ToString(), out var fieldDecimal) &&
            decimal.TryParse(parts[0], out var min) &&
            decimal.TryParse(parts[1], out var max))
        {
            return fieldDecimal >= min && fieldDecimal <= max;
        }

        return false;
    }

    private bool HasChanged(object entity, object? oldEntity, string field)
    {
        if (oldEntity == null) return false; // Can't check change without old value

        var newValue = GetFieldValue(entity, field);
        var oldValue = GetFieldValue(oldEntity, field);

        return !AreEqual(newValue, oldValue?.ToString());
    }

    private bool HasChangedTo(object entity, object? oldEntity, string field, string? targetValue)
    {
        if (!HasChanged(entity, oldEntity, field)) return false;

        var newValue = GetFieldValue(entity, field);
        return AreEqual(newValue, targetValue);
    }

    private bool HasChangedFrom(object entity, object? oldEntity, string field, string? sourceValue)
    {
        if (oldEntity == null || !HasChanged(entity, oldEntity, field)) return false;

        var oldValue = GetFieldValue(oldEntity, field);
        return AreEqual(oldValue, sourceValue);
    }
}

#region Models

public class WorkflowCondition
{
    public string Field { get; set; } = string.Empty;
    public ConditionOperator Operator { get; set; }
    public string? Value { get; set; }
}

#endregion
