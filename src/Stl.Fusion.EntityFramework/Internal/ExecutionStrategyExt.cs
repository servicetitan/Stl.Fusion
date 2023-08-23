using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Stl.Fusion.EntityFramework.Internal;

public static class ExecutionStrategyExt
{
    private static readonly Func<ExecutionStrategy, Exception, bool>? ShouldRetryOnCached;
#if !NET6_0_OR_GREATER
    private static readonly AsyncLocal<bool?>? SuspendedCached;
#else
    private static readonly AsyncLocal<ExecutionStrategy?>? CurrentCached;
#endif

    static ExecutionStrategyExt()
    {
        var tStrategy = typeof(ExecutionStrategy);
        var pStrategy = Expression.Parameter(tStrategy, "strategy");

        var mShouldRetryOn = tStrategy.GetMethod(
            "ShouldRetryOn", BindingFlags.Instance | BindingFlags.NonPublic);
        if (mShouldRetryOn != null) {
            var pError = Expression.Parameter(typeof(Exception), "error");
            var eBody = Expression.Call(pStrategy, mShouldRetryOn, pError);
            ShouldRetryOnCached = (Func<ExecutionStrategy, Exception, bool>) Expression
                .Lambda(eBody, pStrategy, pError)
                .Compile();
        }

#if !NET6_0_OR_GREATER
        var fSuspended = tStrategy.GetField(
            "_suspended", BindingFlags.Static | BindingFlags.NonPublic);
        if (fSuspended?.GetValue(null) is AsyncLocal<bool?> value)
            SuspendedCached = value;
#else
        var fCurrent = tStrategy.GetField(
            "_current", BindingFlags.Static | BindingFlags.NonPublic);
        if (fCurrent?.GetValue(null) is AsyncLocal<ExecutionStrategy?> value)
            CurrentCached = value;
#endif
    }

    public static bool ShouldRetryOn(this IExecutionStrategy executionStrategy, Exception exception)
    {
        if (executionStrategy is not ExecutionStrategy strategy)
            return false;
        if (ShouldRetryOnCached is not { } shouldRetryOn)
            return false;

        return shouldRetryOn.Invoke(strategy, exception);
    }

#if !NET6_0_OR_GREATER
    public static bool Suspend(DbContext dbContext)
    {
        if (SuspendedCached is not { } suspended)
            return false;

        suspended.Value = true;
        return true;
    }
#else
    public static bool Suspend(DbContext dbContext)
    {
        if (CurrentCached is not { } current)
            return false;

        if (ExecutionStrategy.Current == null) {
            var executionStrategy = dbContext.Database.CreateExecutionStrategy();
            if (executionStrategy is ExecutionStrategy es) {
                current.Value = es;
                return true;
            }
        }
        return false;
    }
#endif
}
