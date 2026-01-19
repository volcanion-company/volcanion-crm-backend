namespace CrmSaas.Api.Services;

/// <summary>
/// Service for scheduling and managing background jobs
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueue a fire-and-forget job for immediate execution
    /// </summary>
    string Enqueue<T>(System.Linq.Expressions.Expression<Action<T>> methodCall);
    
    /// <summary>
    /// Schedule a delayed job
    /// </summary>
    string Schedule<T>(System.Linq.Expressions.Expression<Action<T>> methodCall, TimeSpan delay);
    
    /// <summary>
    /// Schedule a delayed job at specific time
    /// </summary>
    string Schedule<T>(System.Linq.Expressions.Expression<Action<T>> methodCall, DateTimeOffset enqueueAt);
    
    /// <summary>
    /// Schedule a recurring job with cron expression
    /// </summary>
    void AddOrUpdateRecurringJob<T>(
        string jobId,
        System.Linq.Expressions.Expression<Action<T>> methodCall,
        string cronExpression,
        TimeZoneInfo? timeZone = null);
    
    /// <summary>
    /// Remove a recurring job
    /// </summary>
    void RemoveRecurringJob(string jobId);
    
    /// <summary>
    /// Delete a scheduled job
    /// </summary>
    bool Delete(string jobId);
}
