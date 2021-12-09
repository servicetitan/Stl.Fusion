using System.Runtime.ExceptionServices;
using Stl.Internal;

namespace Stl.Async;

public static class TaskExt
{
    public static readonly Task InfiniteTask;
    public static readonly Task<Unit> InfiniteUnitTask;
    public static readonly TaskCompletionSource<Unit> UnitTaskCompletionSource;
    public static readonly Task<Unit> UnitTask = Task.FromResult(Unit.Default);
    public static readonly Task<bool> TrueTask = Task.FromResult(true);
    public static readonly Task<bool> FalseTask = Task.FromResult(false);

    static TaskExt()
    {
        InfiniteUnitTask = new TaskCompletionSource<Unit>().Task;
        InfiniteTask = InfiniteUnitTask;
        var unitTcs = new TaskCompletionSource<Unit>();
        unitTcs.SetResult(default);
        UnitTaskCompletionSource = unitTcs;
    }

    // ToXxx

    public static ValueTask<T> ToValueTask<T>(this Task<T> source) => new ValueTask<T>(source);
    public static ValueTask ToValueTask(this Task source) => new ValueTask(source);

    // WithFakeCancellation

    public static Task<T> WithFakeCancellation<T>(
        this Task<T> task,
        CancellationToken cancellationToken)
    {
        if (cancellationToken == default)
            return task;

        async Task<T> TaskOrCancellationTokenTask() {
            using var dTask = cancellationToken.ToTask<T>(task.CreationOptions);
            var winner = await Task.WhenAny(task, dTask.Resource).ConfigureAwait(false);
            return await winner.ConfigureAwait(false);
        }

        return TaskOrCancellationTokenTask();
    }

    public static Task WithFakeCancellation(
        this Task task,
        CancellationToken cancellationToken)
    {
        if (cancellationToken == default)
            return task;

        async Task TaskOrCancellationTokenTask() {
            using var dTask = cancellationToken.ToTask(task.CreationOptions);
            var winner = await Task.WhenAny(task, dTask.Resource).ConfigureAwait(false);
            await winner.ConfigureAwait(false);
        }

        return TaskOrCancellationTokenTask();
    }

    // WithTimeout

    public static Task<bool> WithTimeout(
        this Task task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        => task.WithTimeout(MomentClockSet.Default.CpuClock, timeout, cancellationToken);

    public static async Task<bool> WithTimeout(
        this Task task,
        IMomentClock clock,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        Task? completedTask = null;
        using var cts = new CancellationTokenSource();
        var ctsToken = cts.Token;

        await using var _ = cancellationToken
            .Register(state => ((CancellationTokenSource) state!).Cancel(), cts)
            .ToAsyncDisposableAdapter().ConfigureAwait(false);

        var delayTask = clock.Delay(timeout, ctsToken);
        completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);

        if (completedTask != task) {
            ctsToken.ThrowIfCancellationRequested();
            return false;
        }

        cts.Cancel(); // Ensures delayTask is cancelled to avoid memory leak
        await task.ConfigureAwait(false);
        return true;
    }

    public static Task<Option<T>> WithTimeout<T>(
        this Task<T> task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        => task.WithTimeout(MomentClockSet.Default.CpuClock, timeout, cancellationToken);

    public static async Task<Option<T>> WithTimeout<T>(
        this Task<T> task,
        IMomentClock clock,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        Task? completedTask = null;
        using var cts = new CancellationTokenSource();
        var ctsToken = cts.Token;
        await using var _ = cancellationToken
            .Register(state => ((CancellationTokenSource) state!).Cancel(), cts)
            .ToAsyncDisposableAdapter().ConfigureAwait(false);

        var delayTask = clock.Delay(timeout, ctsToken);
        completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);

        if (completedTask != task) {
            ctsToken.ThrowIfCancellationRequested();
            return Option<T>.None;
        }

        cts.Cancel(); // Ensures delayTask is cancelled to avoid memory leak
        return await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Cross-platform version of <code>IsCompletedSuccessfully</code> from .NET Core.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <returns>True if <paramref name="task"/> is completed successfully; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompletedSuccessfully(this Task task)
    {
#if NETSTANDARD2_0
        return task.Status == TaskStatus.RanToCompletion;
#else
        return task.IsCompletedSuccessfully;
#endif
    }

    // SuppressXxx

    public static Task SuppressExceptions(this Task task)
        => task.ContinueWith(_ => { }, TaskScheduler.Default);

    public static Task<T> SuppressExceptions<T>(this Task<T> task)
        => task.ContinueWith(t => t.IsCompletedSuccessfully() ? t.Result : default!, TaskScheduler.Default);

    public static Task SuppressCancellation(this Task task)
        => task.ContinueWith(t => {
            if (t.IsCompletedSuccessfully() || t.IsCanceled)
                return;
            ExceptionDispatchInfo.Capture(t.Exception!).Throw();
        }, TaskScheduler.Default);

    public static Task<T> SuppressCancellation<T>(this Task<T> task)
        => task.ContinueWith(t => !t.IsCanceled ? t.Result : default!, TaskScheduler.Default);

    // Join

    public static async Task<(T1, T2)> Join<T1, T2>(
        this Task<T1> task1, Task<T2> task2)
    {
        var r1 = await task1.ConfigureAwait(false);
        var r2 = await task2.ConfigureAwait(false);
        return (r1, r2);
    }

    public static async Task<(T1, T2, T3)> Join<T1, T2, T3>(
        Task<T1> task1, Task<T2> task2, Task<T3> task3)
    {
        var r1 = await task1.ConfigureAwait(false);
        var r2 = await task2.ConfigureAwait(false);
        var r3 = await task3.ConfigureAwait(false);
        return (r1, r2, r3);
    }

    public static async Task<(T1, T2, T3, T4)> Join<T1, T2, T3, T4>(
        Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4)
    {
        var r1 = await task1.ConfigureAwait(false);
        var r2 = await task2.ConfigureAwait(false);
        var r3 = await task3.ConfigureAwait(false);
        var r4 = await task4.ConfigureAwait(false);
        return (r1, r2, r3, r4);
    }

    public static async Task<(T1, T2, T3, T4, T5)> Join<T1, T2, T3, T4, T5>(
        Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5)
    {
        var r1 = await task1.ConfigureAwait(false);
        var r2 = await task2.ConfigureAwait(false);
        var r3 = await task3.ConfigureAwait(false);
        var r4 = await task4.ConfigureAwait(false);
        var r5 = await task5.ConfigureAwait(false);
        return (r1, r2, r3, r4, r5);
    }

    // WhileRunning

    public static async Task WhileRunning(this Task dependency, Func<CancellationToken, Task> dependentTaskFactory)
    {
        using var cts = new CancellationTokenSource();
        var dependentTask = dependentTaskFactory(cts.Token);
        try {
            await dependency.ConfigureAwait(false);
        }
        finally {
            cts.Cancel();
            await dependentTask.SuppressCancellation().ConfigureAwait(false);
        }
    }

    public static async Task<T> WhileRunning<T>(this Task<T> dependency, Func<CancellationToken, Task> dependentTaskFactory)
    {
        using var cts = new CancellationTokenSource();
        var dependentTask = dependentTaskFactory(cts.Token);
        try {
            return await dependency.ConfigureAwait(false);
        }
        finally {
            cts.Cancel();
            await dependentTask.SuppressCancellation().ConfigureAwait(false);
        }
    }

    // AssertXxx

    public static Task AssertCompleted(this Task task)
        => !task.IsCompleted ? throw Errors.TaskIsNotCompleted() : task;

    public static Task<T> AssertCompleted<T>(this Task<T> task)
        => !task.IsCompleted ? throw Errors.TaskIsNotCompleted() : task;

    public static ValueTask AssertCompleted(this ValueTask task)
        => !task.IsCompleted ? throw Errors.TaskIsNotCompleted() : task;

    public static ValueTask<T> AssertCompleted<T>(this ValueTask<T> task)
        => !task.IsCompleted ? throw Errors.TaskIsNotCompleted() : task;
}
