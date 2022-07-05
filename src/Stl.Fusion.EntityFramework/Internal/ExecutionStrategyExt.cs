using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Stl.Fusion.EntityFramework.Internal;

public static class ExecutionStrategyExt
{
    private static readonly Func<ExecutionStrategy, Exception, bool>? ShouldRetryOnCached;
#if !NET6_0_OR_GREATER
    private static readonly AsyncLocal<bool?>? SuspendedCached;
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

#if NET6_0_OR_GREATER
    public static ClosedDisposable<Unit> TrySuspend()
        => default;

    public static bool? TryGetIsSuspended()
        => null;

    public static bool TrySetIsSuspended(bool value)
        => false;
#else
    public static ClosedDisposable<Unit> TrySuspend()
    {
        if (TryGetIsSuspended() is not false)
            return default;
        TrySetIsSuspended(true);
        return new ClosedDisposable<Unit>(default, _ => TrySetIsSuspended(false));
    }

    public static bool? TryGetIsSuspended()
    {
        if (SuspendedCached is not { } suspended)
            return null;
        return suspended.Value ?? false;
    }

    public static bool TrySetIsSuspended(bool value)
    {
        if (SuspendedCached is not { } suspended)
            return false;
        suspended.Value = value;
        return true;
    }
#endif
}
