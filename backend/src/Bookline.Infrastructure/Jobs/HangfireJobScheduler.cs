namespace Bookline.Infrastructure.Jobs;

using System.Linq.Expressions;
using Bookline.Application.Common.Interfaces;
using Hangfire;

public class HangfireJobScheduler : IBackgroundJobScheduler
{
    public string Enqueue(Expression<Func<Task>> methodCall)
    {
        return BackgroundJob.Enqueue(methodCall);
    }

    public void Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        BackgroundJob.Schedule(methodCall, delay);
    }

    public void AddOrUpdateRecurringJob(string recurringJobId, Expression<Func<Task>> methodCall, string cronExpression)
    {
        RecurringJob.AddOrUpdate(recurringJobId, methodCall, cronExpression);
    }
}
