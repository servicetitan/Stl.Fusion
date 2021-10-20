using FluentAssertions.Execution;

namespace Stl.Testing;

public static class TestExt
{
    public static readonly IEnumerable<TimeSpan> DefaultCheckIntervals = Intervals.Fixed(TimeSpan.FromMilliseconds(50));

    public static Func<Task> AsAsyncFunc(this Task task)
        => () => task;
    public static Func<Task<T>> AsAsyncFunc<T>(this Task<T> task)
        => () => task;
    public static Func<Task> AsAsyncFunc(this ValueTask task)
        => task.AsTask;
    public static Func<Task<T>> AsAsyncFunc<T>(this ValueTask<T> task)
        => task.AsTask;

    public static Task WhenMet(Action condition,
        TimeSpan waitDuration)
        => WhenMet(condition, null, waitDuration);

    public static async Task WhenMet(Action condition,
        IEnumerable<TimeSpan>? checkIntervals,
        TimeSpan waitDuration)
    {
        using var cts = new CancellationTokenSource(waitDuration);
        await WhenMet(condition, checkIntervals, cts.Token).ConfigureAwait(false);
    }

    public static async Task WhenMet(Action condition,
        IEnumerable<TimeSpan>? checkIntervals,
        CancellationToken cancellationToken)
    {
        foreach (var timeout in checkIntervals ?? DefaultCheckIntervals) {
            using (var scope = new AssertionScope()) {
                condition.Invoke();
                if (!scope.HasFailures())
                    return;
                if (!cancellationToken.IsCancellationRequested)
                    scope.Discard();
            }
            await Task.Delay(timeout, cancellationToken).SuppressCancellation().ConfigureAwait(false);
        }
    }
}
