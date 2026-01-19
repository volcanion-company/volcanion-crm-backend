using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

/// <summary>
/// Workflow definition - automated business process
/// </summary>
public class Workflow : TenantAuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Target entity type: Customer, Lead, Opportunity, Ticket, etc.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// When to trigger: OnCreate, OnUpdate, OnDelete, Scheduled
    /// </summary>
    public WorkflowTriggerType TriggerType { get; set; }
    
    /// <summary>
    /// For OnUpdate trigger: which fields to monitor (JSON array)
    /// Example: ["Status", "AssignedToUserId"]
    /// </summary>
    public string? TriggerFields { get; set; }
    
    /// <summary>
    /// For Scheduled trigger: cron expression
    /// Example: "0 9 * * MON" (every Monday at 9am)
    /// </summary>
    public string? ScheduleExpression { get; set; }
    
    /// <summary>
    /// Workflow is active and will be executed
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Order of execution when multiple workflows match
    /// </summary>
    public int ExecutionOrder { get; set; } = 0;
    
    /// <summary>
    /// Stop processing other workflows if this one matches
    /// </summary>
    public bool StopOnMatch { get; set; } = false;
    
    // Navigation
    public virtual ICollection<WorkflowRule> Rules { get; set; } = [];
}

/// <summary>
/// Workflow rule - defines conditions and actions
/// </summary>
public class WorkflowRule : TenantAuditableEntity
{
    public Guid WorkflowId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Rule execution order within workflow
    /// </summary>
    public int Order { get; set; } = 0;
    
    /// <summary>
    /// Conditions to evaluate (JSON)
    /// Example: [
    ///   {"Field": "Status", "Operator": "Equals", "Value": "Won"},
    ///   {"Field": "Amount", "Operator": "GreaterThan", "Value": 10000}
    /// ]
    /// </summary>
    public string? Conditions { get; set; }
    
    /// <summary>
    /// Logic operator for multiple conditions: And, Or
    /// </summary>
    public ConditionLogic ConditionLogic { get; set; } = ConditionLogic.And;
    
    /// <summary>
    /// Rule is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual Workflow? Workflow { get; set; }
    public virtual ICollection<WorkflowAction> Actions { get; set; } = [];
}

/// <summary>
/// Workflow action - what to do when rule matches
/// </summary>
public class WorkflowAction : TenantAuditableEntity
{
    public Guid WorkflowRuleId { get; set; }
    
    /// <summary>
    /// Action type: UpdateField, SendEmail, CreateTask, AssignOwner, etc.
    /// </summary>
    public WorkflowActionType ActionType { get; set; }
    
    /// <summary>
    /// Execution order within rule
    /// </summary>
    public int Order { get; set; } = 0;
    
    /// <summary>
    /// Action configuration (JSON)
    /// UpdateField: {"Field": "Status", "Value": "Contacted"}
    /// SendEmail: {"TemplateId": "xxx", "To": "{{AssignedToUser.Email}}"}
    /// CreateTask: {"Subject": "Follow up", "DueDate": "+3d", "AssignedTo": "{{OwnerId}}"}
    /// AssignOwner: {"UserId": "xxx"} or {"Team": "Sales"}
    /// CreateActivity: {"Type": "Call", "Subject": "...", "DueDate": "+1d"}
    /// Webhook: {"Url": "https://...", "Method": "POST", "Payload": {...}}
    /// </summary>
    public string ActionConfig { get; set; } = "{}";
    
    /// <summary>
    /// Delay before executing action (minutes)
    /// </summary>
    public int DelayMinutes { get; set; } = 0;
    
    /// <summary>
    /// Action is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual WorkflowRule? WorkflowRule { get; set; }
}

/// <summary>
/// Workflow execution log - audit trail
/// </summary>
public class WorkflowExecutionLog : TenantAuditableEntity
{
    public Guid WorkflowId { get; set; }
    public Guid? WorkflowRuleId { get; set; }
    public Guid? WorkflowActionId { get; set; }
    
    /// <summary>
    /// Target entity type
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Target entity ID
    /// </summary>
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// Execution status: Success, Failed, Skipped
    /// </summary>
    public WorkflowExecutionStatus Status { get; set; }
    
    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Execution details (JSON)
    /// </summary>
    public string? ExecutionDetails { get; set; }
    
    /// <summary>
    /// Execution duration in milliseconds
    /// </summary>
    public int DurationMs { get; set; }
    
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public virtual Workflow? Workflow { get; set; }
}

#region Enums

public enum WorkflowTriggerType
{
    OnCreate = 0,       // When entity is created
    OnUpdate = 1,       // When entity is updated
    OnDelete = 2,       // When entity is deleted (soft delete)
    Scheduled = 3,      // Time-based trigger (cron)
    Manual = 4          // Manually triggered by user
}

public enum ConditionLogic
{
    And = 0,  // All conditions must match
    Or = 1    // Any condition can match
}

public enum WorkflowActionType
{
    UpdateField = 0,        // Update entity field value
    SendEmail = 1,          // Send email notification
    CreateTask = 2,         // Create activity/task
    AssignOwner = 3,        // Change record owner
    CreateActivity = 4,     // Create follow-up activity
    SendWebhook = 5,        // Call external webhook
    CreateRecord = 6,       // Create related record
    UpdateRelated = 7,      // Update related records
    SendNotification = 8,   // In-app notification
    SendSms = 9            // SMS notification
}

public enum WorkflowExecutionStatus
{
    Success = 0,
    Failed = 1,
    Skipped = 2,
    Pending = 3
}

public enum ConditionOperator
{
    Equals = 0,
    NotEquals = 1,
    Contains = 2,
    NotContains = 3,
    StartsWith = 4,
    EndsWith = 5,
    GreaterThan = 6,
    GreaterThanOrEqual = 7,
    LessThan = 8,
    LessThanOrEqual = 9,
    IsNull = 10,
    IsNotNull = 11,
    In = 12,            // Value in list
    NotIn = 13,
    Between = 14,
    Changed = 15,       // Field value changed (for OnUpdate trigger)
    ChangedTo = 16,     // Field changed to specific value
    ChangedFrom = 17    // Field changed from specific value
}

#endregion
