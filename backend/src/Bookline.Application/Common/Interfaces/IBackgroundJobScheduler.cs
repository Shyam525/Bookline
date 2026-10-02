namespace Bookline.Application.Common.Interfaces;

using System.Linq.Expressions;

public interface IBackgroundJobScheduler
{
    string Enqueue(Expression<Func<Task>> methodCall);
    void Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);
    void AddOrUpdateRecurringJob(string recurringJobId, Expression<Func<Task>> methodCall, string cronExpression);
}
