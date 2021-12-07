using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Stl.Fusion.EntityFramework.Internal;

public static class ExecutionStrategyExt
{
    private static readonly Func<ExecutionStrategy, Exception, bool> ShouldRetryOnCached;

    static ExecutionStrategyExt()
    {
        var tStrategy = typeof(ExecutionStrategy);
        var shouldRetryOnMethod = tStrategy.GetMethod(
            "ShouldRetryOn", BindingFlags.Instance | BindingFlags.NonPublic);
        var pStrategy = Expression.Parameter(tStrategy, "strategy");
        var pError = Expression.Parameter(typeof(Exception), "error");
        var eCall = Expression.Call(pStrategy, shouldRetryOnMethod!, pError);
        ShouldRetryOnCached = (Func<ExecutionStrategy, Exception, bool>) Expression
            .Lambda(eCall, pStrategy, pError)
            .Compile();
    }

    public static bool ShouldRetryOn(this IExecutionStrategy executionStrategy, Exception exception)
        => executionStrategy is ExecutionStrategy retryingExecutionStrategy
            && ShouldRetryOnCached.Invoke(retryingExecutionStrategy, exception);
}
