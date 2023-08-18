using Stl.Fusion.Internal;

namespace Stl.Fusion;

public static class AnonymousComputedSourceExt
{
    // When

    public static Task<Computed<T>> When<T>(this AnonymousComputedSource<T> source,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default)
        => source.When(predicate, FixedDelayer.Instant, cancellationToken);
    public static async Task<Computed<T>> When<T>(this AnonymousComputedSource<T> source,
        Func<T, bool> predicate,
        IUpdateDelayer updateDelayer,
        CancellationToken cancellationToken = default)
    {
        while (true) {
            var computed = await source.Update(cancellationToken).ConfigureAwait(false);
            if (predicate.Invoke(computed.Value))
                return computed;

            await computed.WhenInvalidated(cancellationToken).ConfigureAwait(false);
            await updateDelayer.Delay(0, cancellationToken).ConfigureAwait(false);
        }
    }

    public static Task<Computed<T>> When<T>(this AnonymousComputedSource<T> source,
        Func<T, Exception?, bool> predicate,
        CancellationToken cancellationToken = default)
        => source.When(predicate, FixedDelayer.Instant, cancellationToken);
    public static async Task<Computed<T>> When<T>(this AnonymousComputedSource<T> source,
        Func<T, Exception?, bool> predicate,
        IUpdateDelayer updateDelayer,
        CancellationToken cancellationToken = default)
    {
        while (true) {
            var computed = await source.Update(cancellationToken).ConfigureAwait(false);
            var (value, error) = computed;
            if (predicate.Invoke(value, error))
                return computed;

            await computed.WhenInvalidated(cancellationToken).ConfigureAwait(false);
            await updateDelayer.Delay(0, cancellationToken).ConfigureAwait(false);
        }
    }

    // Changes

    public static IAsyncEnumerable<Computed<T>> Changes<T>(
        this AnonymousComputedSource<T> source,
        CancellationToken cancellationToken = default)
        => source.Changes(FixedDelayer.Instant, cancellationToken);
    public static async IAsyncEnumerable<Computed<T>> Changes<T>(
        this AnonymousComputedSource<T> source,
        IUpdateDelayer updateDelayer,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var retryCount = 0;
        while (true) {
            var computed = await source.Update(cancellationToken).ConfigureAwait(false);
            yield return computed;

            await computed.WhenInvalidated(cancellationToken).ConfigureAwait(false);

            var hasTransientError = computed.Error is { } error && ((IComputedImpl) computed).IsTransientError(error);
            retryCount = hasTransientError ? retryCount + 1 : 0;

            await updateDelayer.Delay(retryCount, cancellationToken).ConfigureAwait(false);
        }
        // ReSharper disable once IteratorNeverReturns
    }
}
