using Stl.Caching;
using Stl.Fusion.Internal;

namespace Stl.Fusion;

public static class ComputedExt
{
    private static readonly RefHolder RefHolder = new();

    public static void Invalidate(this IComputed computed, TimeSpan delay, bool? usePreciseTimer = null)
    {
        if (delay <= TimeSpan.Zero) {
            computed.Invalidate();
            return;
        }

        var bPrecise = usePreciseTimer ?? delay <= TimeSpan.FromSeconds(1);
        if (!bPrecise) {
            Timeouts.Invalidate.AddOrUpdateToEarlier(computed, Timeouts.Clock.Now + delay);
            computed.Invalidated += c => Timeouts.Invalidate.Remove(c);
            return;
        }

        using var _ = ExecutionContextExt.SuppressFlow();
        var cts = new CancellationTokenSource(delay);
        var registration = cts.Token.Register(() => {
            // No need to schedule this via Task.Run, since this code is
            // either invoked from Invalidate method (via Invalidated handler),
            // so Invalidate() call will do nothing & return immediately,
            // or it's invoked via one of timer threads, i.e. where it's
            // totally fine to invoke Invalidate directly as well.
            computed.Invalidate();
            cts.Dispose();
        });
        computed.Invalidated += _ => {
            try {
                if (!cts.IsCancellationRequested)
                    cts.Cancel(true);
            } catch {
                // Intended: this method should never throw any exceptions
            }
            finally {
                registration.Dispose();
                cts.Dispose();
            }
        };
    }

    public static Task WhenInvalidated(this IComputed computed, CancellationToken cancellationToken = default)
    {
        if (computed.ConsistencyState == ConsistencyState.Invalidated)
            return Task.CompletedTask;
        var taskSource = TaskSource.New<Unit>(true);
        if (cancellationToken != CancellationToken.None)
            return new WhenInvalidatedClosure(taskSource, computed, cancellationToken).Task;
        // No way to cancel / unregister the handler here
        computed.Invalidated += _ => taskSource.TrySetResult(default);
        return taskSource.Task;
    }

    public static void SetOutput<T>(this IComputed<T> computed, Result<T> output)
    {
        if (!computed.TrySetOutput(output))
            throw Errors.WrongComputedState(ConsistencyState.Computing, computed.ConsistencyState);
    }

    // Updates N computed so that all of them are in consistent state

    public static async ValueTask<(IComputed<T1>, IComputed<T2>)> Update<T1, T2>(
        IComputed<T1> c1,
        IComputed<T2> c2,
        CancellationToken cancellationToken = default)
    {
        while (true) {
            var t1 = c1.IsConsistent() ? null : c1.Update(cancellationToken).AsTask();
            var t2 = c2.IsConsistent() ? null : c2.Update(cancellationToken).AsTask();
            if (t1 is null && t2 is null)
                return (c1, c2);
            await Task.WhenAll(t1 ?? Task.CompletedTask, t2 ?? Task.CompletedTask)
                .ConfigureAwait(false);
#pragma warning disable MA0004
            c1 = t1 is null ? c1 : await t1;
            c2 = t2 is null ? c2 : await t2;
#pragma warning restore MA0004
        }
    }

    public static async ValueTask<(IComputed<T1>, IComputed<T2>, IComputed<T3>)> Update<T1, T2, T3>(
        IComputed<T1> c1,
        IComputed<T2> c2,
        IComputed<T3> c3,
        CancellationToken cancellationToken = default)
    {
        while (true) {
            var t1 = c1.IsConsistent() ? null : c1.Update(cancellationToken).AsTask();
            var t2 = c2.IsConsistent() ? null : c2.Update(cancellationToken).AsTask();
            var t3 = c3.IsConsistent() ? null : c3.Update(cancellationToken).AsTask();
            if (t1 is null && t2 is null && t3 is null)
                return (c1, c2, c3);
            await Task.WhenAll(t1 ?? Task.CompletedTask, t2 ?? Task.CompletedTask, t3 ?? Task.CompletedTask)
                .ConfigureAwait(false);
#pragma warning disable MA0004
            c1 = t1 is null ? c1 : await t1;
            c2 = t2 is null ? c2 : await t2;
            c3 = t3 is null ? c3 : await t3;
#pragma warning restore MA0004
        }
    }

    // When

    public static async Task<IComputed<T>> When<T>(this IComputed<T> computed,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        while (true) {
            if (!computed.IsConsistent())
                computed = await computed.Update(cancellationToken).ConfigureAwait(false);
            if (predicate(computed.Value))
                return computed;
            await computed.WhenInvalidated(cancellationToken).ConfigureAwait(false);
        }
    }

    // Changes

    public static async IAsyncEnumerable<IComputed<T>> Changes<T>(
        this IComputed<T> computed,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true) {
            computed = await computed.Update(cancellationToken).ConfigureAwait(false);
            yield return computed;
            await computed.WhenInvalidated(cancellationToken).ConfigureAwait(false);
        }
        // ReSharper disable once IteratorNeverReturns
    }
}
