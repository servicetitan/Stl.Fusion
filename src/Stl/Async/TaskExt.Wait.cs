using Stl.Internal;

namespace Stl.Async;

#pragma warning disable MA0004

public static partial class TaskExt
{
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
            using var cts = cancellationToken.CreateLinkedTokenSource();
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
            using var cts = cancellationToken.CreateLinkedTokenSource();
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
            using var cts = cancellationToken.CreateLinkedTokenSource();
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
            using var cts = cancellationToken.CreateLinkedTokenSource();
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

    // WaitErrorAsync

    public static async Task<Exception?> WaitErrorAsync(this Task task, bool throwOperationCancelledException = false)
    {
        try {
            await task.ConfigureAwait(false);
            return null;
        }
        catch (Exception error) {
            if (throwOperationCancelledException && error is OperationCanceledException)
                throw;
            return error;
        }
    }
}
