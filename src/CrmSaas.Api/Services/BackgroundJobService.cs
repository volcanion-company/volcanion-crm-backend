using Hangfire;
using System.Linq.Expressions;

namespace CrmSaas.Api.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public BackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }

    public string Enqueue<T>(Expression<Action<T>> methodCall)
    {
        return _backgroundJobClient.Enqueue(methodCall);
    }

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
    {
        return _backgroundJobClient.Schedule(methodCall, delay);
    }

    public string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt)
    {
        return _backgroundJobClient.Schedule(methodCall, enqueueAt);
    }

    public void AddOrUpdateRecurringJob<T>(
        string jobId,
        Expression<Action<T>> methodCall,
        string cronExpression,
        TimeZoneInfo? timeZone = null)
    {
        _recurringJobManager.AddOrUpdate(
            jobId,
            methodCall,
            cronExpression,
            new RecurringJobOptions
            {
                TimeZone = timeZone ?? TimeZoneInfo.Local
            });
    }

    public void RemoveRecurringJob(string jobId)
    {
        _recurringJobManager.RemoveIfExists(jobId);
    }

    public bool Delete(string jobId)
    {
        return _backgroundJobClient.Delete(jobId);
    }
}
