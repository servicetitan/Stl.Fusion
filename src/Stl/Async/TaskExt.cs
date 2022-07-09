using System.Linq.Expressions;
using System.Runtime.ExceptionServices;
using Stl.Internal;

namespace Stl.Async;

#pragma warning disable MA0004

public static class TaskExt
{
    private static readonly MethodInfo FromTypedTaskInternalMethod =
        typeof(Result).GetMethod(nameof(FromTypedTaskInternal), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly ConcurrentDictionary<Type, Func<Task, IResult>> ToTypedResultCache = new();

    public static readonly Task NeverEndingTask;
    public static readonly Task<Unit> NeverEndingUnitTask;
    public static readonly Task<Unit> UnitTask = Task.FromResult(Unit.Default);
    public static readonly Task<bool> TrueTask = Task.FromResult(true);
    public static readonly Task<bool> FalseTask = Task.FromResult(false);

    public static readonly TaskCompletionSource<Unit> UnitTaskCompletionSource;

    static TaskExt()
    {
        NeverEndingUnitTask = NeverEnding();
        NeverEndingTask = NeverEndingUnitTask;
        var unitTcs = new TaskCompletionSource<Unit>();
        unitTcs.SetResult(default);
        UnitTaskCompletionSource = unitTcs;

        async Task<Unit> NeverEnding()
            => await TaskSource.New<Unit>(true).Task.ConfigureAwait(false);
    }

    // ToValueTask

    public static ValueTask<T> ToValueTask<T>(this Task<T> source) => new(source);
    public static ValueTask ToValueTask(this Task source) => new(source);

    // GetUnwrappedException

    public static Exception GetBaseException(this Task task)
        => task.AssertCompleted().Exception?.GetBaseException()
            ?? (task.IsCanceled
                ? new TaskCanceledException(task)
                : throw Errors.TaskIsFaultedButNoExceptionAvailable());

    // ToResultSynchronously

    public static Result<Unit> ToResultSynchronously(this Task task)
        => task.AssertCompleted().IsCompletedSuccessfully()
            ? default
            : new Result<Unit>(default, task.GetBaseException());

    public static Result<T> ToResultSynchronously<T>(this Task<T> task)
        => task.AssertCompleted().IsCompletedSuccessfully()
            ? task.Result
            : new Result<T>(default!, task.GetBaseException());

    // ToResultAsync

    public static async Task<Result<Unit>> ToResultAsync(this Task task)
    {
        try {
            await task.ConfigureAwait(false);
            return default;
        }
        catch (Exception e) {
            return new Result<Unit>(default, e);
        }
    }

    public static async Task<Result<T>> ToResultAsync<T>(this Task<T> task)
    {
        try {
            return await task.ConfigureAwait(false);
        }
        catch (Exception e) {
            return new Result<T>(default!, e);
        }
    }

    // ToTypedResultSynchronously

    public static IResult ToTypedResultSynchronously(this Task task)
    {
        var tValue = task.AssertCompleted().GetType().GetTaskOrValueTaskArgument();
        if (tValue == null) {
            // ReSharper disable once HeapView.BoxingAllocation
            return task.IsCompletedSuccessfully()
                ? new Result<Unit>()
                : new Result<Unit>(default, task.GetBaseException());
        }

        return ToTypedResultCache.GetOrAdd(tValue, static tValue1 => {
            var mFromUntypedTaskInternal = FromTypedTaskInternalMethod.MakeGenericMethod(tValue1);
            var pTask = Expression.Parameter(typeof(Task));
            var fn = Expression.Lambda<Func<Task, IResult>>(
                Expression.Call(mFromUntypedTaskInternal, pTask),
                pTask
            ).Compile();
            return fn;
        }).Invoke(task);
    }

    // ToTypedResultSynchronously

    public static Task<IResult> ToTypedResultAsync(this Task task)
        => task.ContinueWith(
            t => t.ToTypedResultSynchronously(), 
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    // WaitAsync

#if !NET6_0_OR_GREATER
    public static Task WaitAsync(
        this Task task,
        CancellationToken cancellationToken = default)
        => task.WaitAsync(MomentClockSet.Default.CpuClock, Timeout.InfiniteTimeSpan, cancellationToken);

    public static Task WaitAsync(
        this Task task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        => task.WaitAsync(MomentClockSet.Default.CpuClock, timeout, cancellationToken);
#endif

    public static Task WaitAsync(
        this Task task,
        IMomentClock clock,
        CancellationToken cancellationToken = default)
        => task.WaitAsync(clock, Timeout.InfiniteTimeSpan, cancellationToken);

    public static Task WaitAsync(
        this Task task,
        IMomentClock clock,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (task.IsCompleted)
            return task;
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return timeout == Timeout.InfiniteTimeSpan
            ? cancellationToken.CanBeCanceled ? WaitForCancellation() : task
            : WaitForTimeout();

        async Task WaitForCancellation() {
            using var dTask = cancellationToken.ToTask();
            var winnerTask = await Task.WhenAny(task, dTask.Resource).ConfigureAwait(false);
            await winnerTask;
        }

        async Task WaitForTimeout()
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try {
                var timeoutTask = clock.Delay(timeout, cts.Token);
                var winnerTask = await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);
                if (winnerTask != task) {
                    await timeoutTask;
                    throw new TimeoutException();
                }
                await task;
            }
            finally {
                cts.Cancel(); // Ensures delayTask is cancelled to avoid memory leak
            }
        }
    }

#if !NET6_0_OR_GREATER
    public static Task<T> WaitAsync<T>(
        this Task<T> task,
        CancellationToken cancellationToken = default)
        => task.WaitAsync(MomentClockSet.Default.CpuClock, Timeout.InfiniteTimeSpan, cancellationToken);

    public static Task<T> WaitAsync<T>(
        this Task<T> task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        => task.WaitAsync(MomentClockSet.Default.CpuClock, timeout, cancellationToken);
#endif

    public static Task<T> WaitAsync<T>(
        this Task<T> task,
        IMomentClock clock,
        CancellationToken cancellationToken = default)
        => task.WaitAsync(clock, Timeout.InfiniteTimeSpan, cancellationToken);

    public static Task<T> WaitAsync<T>(
        this Task<T> task,
        IMomentClock clock,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (task.IsCompleted)
            return task;
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<T>(cancellationToken);

        return timeout == Timeout.InfiniteTimeSpan
            ? cancellationToken.CanBeCanceled ? WaitForCancellation() : task
            : WaitForTimeout();

        async Task<T> WaitForCancellation() {
            using var dTask = cancellationToken.ToTask();
            var winnerTask = await Task.WhenAny(task, dTask.Resource).ConfigureAwait(false);
            if (winnerTask != task) {
                cancellationToken.ThrowIfCancellationRequested();
                throw Errors.InternalError("This method can't get here.");
            }
            return await task;
        }

        async Task<T> WaitForTimeout()
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try {
                var timeoutTask = clock.Delay(timeout, cts.Token);
                var winnerTask = await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);
                if (winnerTask != task) {
                    await timeoutTask;
                    throw new TimeoutException();
                }
                return await task;
            }
            finally {
                cts.Cancel(); // Ensures delayTask is cancelled to avoid memory leak
            }
        }
    }

    // WaitResultAsync

    public static Task<Result<Unit>> WaitResultAsync(
        this Task task,
        CancellationToken cancellationToken = default)
        => task.WaitResultAsync(MomentClockSet.Default.CpuClock, Timeout.InfiniteTimeSpan, cancellationToken);

    public static Task<Result<Unit>> WaitResultAsync(
        this Task task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        => task.WaitResultAsync(MomentClockSet.Default.CpuClock, timeout, cancellationToken);

    public static Task<Result<Unit>> WaitResultAsync(
        this Task task,
        IMomentClock clock,
        CancellationToken cancellationToken = default)
        => task.WaitResultAsync(clock, Timeout.InfiniteTimeSpan, cancellationToken);

    public static Task<Result<Unit>> WaitResultAsync(
        this Task task,
        IMomentClock clock,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (task.IsCompleted)
            return task.ToResultAsync();
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(new Result<Unit>(default!, new OperationCanceledException(cancellationToken)));

        return timeout == Timeout.InfiniteTimeSpan
            ? cancellationToken.CanBeCanceled ? WaitForCancellation() : task.ToResultAsync()
            : WaitForTimeout();

        async Task<Result<Unit>> WaitForCancellation() {
            using var dTask = cancellationToken.ToTask();
            var winnerTask = await Task.WhenAny(task, dTask.Resource).ConfigureAwait(false);
            return winnerTask.ToResultSynchronously();
        }

        async Task<Result<Unit>> WaitForTimeout()
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try {
                var timeoutTask = clock.Delay(timeout, cts.Token);
                var winnerTask = await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);
                if (winnerTask != task) {
                    if (cancellationToken.IsCancellationRequested)
                        return new Result<Unit>(default!, new OperationCanceledException(cancellationToken));
                    return new Result<Unit>(default!, new TimeoutException());
                }
                return task.ToResultSynchronously();
            }
            finally {
                cts.Cancel(); // Ensures delayTask is cancelled to avoid memory leak
            }
        }
    }

    public static Task<Result<T>> WaitResultAsync<T>(
        this Task<T> task,
        CancellationToken cancellationToken = default)
        => task.WaitResultAsync(MomentClockSet.Default.CpuClock, Timeout.InfiniteTimeSpan, cancellationToken);

    public static Task<Result<T>> WaitResultAsync<T>(
        this Task<T> task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        => task.WaitResultAsync(MomentClockSet.Default.CpuClock, timeout, cancellationToken);

    public static Task<Result<T>> WaitResultAsync<T>(
        this Task<T> task,
        IMomentClock clock,
        CancellationToken cancellationToken = default)
        => task.WaitResultAsync(clock, Timeout.InfiniteTimeSpan, cancellationToken);

    public static Task<Result<T>> WaitResultAsync<T>(
        this Task<T> task,
        IMomentClock clock,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (task.IsCompleted)
            return task.ToResultAsync();
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(new Result<T>(default!, new OperationCanceledException(cancellationToken)));

        return timeout == Timeout.InfiniteTimeSpan
            ? cancellationToken.CanBeCanceled ? WaitForCancellation() : task.ToResultAsync()
            : WaitForTimeout();

        async Task<Result<T>> WaitForCancellation() {
            using var dTask = cancellationToken.ToTask();
            var winnerTask = await Task.WhenAny(task, dTask.Resource).ConfigureAwait(false);
            if (winnerTask != task)
                return new Result<T>(default!, new OperationCanceledException(cancellationToken));
            return task.ToResultSynchronously();
        }

        async Task<Result<T>> WaitForTimeout()
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try {
                var timeoutTask = clock.Delay(timeout, cts.Token);
                var winnerTask = await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);
                if (winnerTask != task) {
                    if (cancellationToken.IsCancellationRequested)
                        return new Result<T>(default!, new OperationCanceledException(cancellationToken));
                    return new Result<T>(default!, new TimeoutException());
                }
                return task.ToResultSynchronously();
            }
            finally {
                cts.Cancel(); // Ensures delayTask is cancelled to avoid memory leak
            }
        }
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

    public static async Task SuppressExceptions(this Task task, Func<Exception, bool>? filter = null)
    {
        try {
            await task.ConfigureAwait(false);
        }
        catch (Exception e) {
            if (filter?.Invoke(e) ?? true)
                return;
            throw;
        }
    }

    public static async Task<T> SuppressExceptions<T>(this Task<T> task, Func<Exception, bool>? filter = null)
    {
        try {
            return await task.ConfigureAwait(false);
        }
        catch (Exception e) {
            if (filter?.Invoke(e) ?? true)
                return default!;
            throw;
        }
    }

    public static Task SuppressCancellation(this Task task)
        => task.ContinueWith(
            t => {
                if (t.IsCompletedSuccessfully() || t.IsCanceled)
                    return;
                ExceptionDispatchInfo.Capture(t.Exception!.GetBaseException()).Throw();
            }, 
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    public static Task<T> SuppressCancellation<T>(this Task<T> task)
        => task.ContinueWith(
            t => t.IsCanceled ? default! : t.Result,
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

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

    // AssertXxx

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task AssertCompleted(this Task task)
        => !task.IsCompleted ? throw Errors.TaskIsNotCompleted() : task;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<T> AssertCompleted<T>(this Task<T> task)
        => !task.IsCompleted ? throw Errors.TaskIsNotCompleted() : task;

    // Private methods

    private static IResult FromTypedTaskInternal<T>(Task task)
        // ReSharper disable once HeapView.BoxingAllocation
        => task.IsCompletedSuccessfully()
            ? Result.Value(((Task<T>) task).Result)
            : Result.Error<T>(task.GetBaseException());
}
