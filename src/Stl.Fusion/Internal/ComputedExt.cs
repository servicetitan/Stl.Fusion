namespace Stl.Fusion.Internal;

public static class ComputedExt
{
    private static class TaskCache<T>
    {
        public static readonly Task<T> DefaultResultTask = Task.FromResult(default(T)!);
    }

    internal static bool TryUseExisting<T>(this IComputed<T>? existing, ComputeContext context, IComputed? usedBy)
    {
        var callOptions = context.CallOptions;
        var mustUseExisting = (callOptions & CallOptions.TryGetExisting) != 0;

        if (existing == null)
            return mustUseExisting;
        if (!(mustUseExisting || existing.IsConsistent()))
            return false;
        // We're here, if (existing != null && (mustUseExisting || existing.IsConsistent()))

        context.TryCapture(existing);
        var invalidate = (callOptions & CallOptions.Invalidate) == CallOptions.Invalidate;
        if (invalidate) {
            existing.Invalidate();
            return true;
        }
        if (!mustUseExisting)
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) existing!);
        ((IComputedImpl?) existing)?.RenewTimeouts();
        return true;
    }

    internal static async ValueTask<ResultBox<T>?> TryUseExisting<T>(
        this IAsyncComputed<T>? existing, ComputeContext context, IComputed? usedBy,
        CancellationToken cancellationToken)
    {
        var callOptions = context.CallOptions;
        var mustUseExisting = (callOptions & CallOptions.TryGetExisting) != 0;

        if (existing == null)
            return mustUseExisting ? ResultBox<T>.Default : null;
        if (!(mustUseExisting || existing.IsConsistent()))
            return null;

        var invalidate = (callOptions & CallOptions.Invalidate) == CallOptions.Invalidate;
        if (invalidate) {
            existing.Invalidate();
            context.TryCapture(existing);
            return existing.MaybeOutput ?? ResultBox<T>.Default;
        }

        var result = existing.MaybeOutput;
        if (result == null) {
            result = await existing.GetOutput(cancellationToken).ConfigureAwait(false);
            if (result == null)
                return null;
        }

        context.TryCapture(existing);
        if (!mustUseExisting)
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) existing!);
        ((IComputedImpl?) existing)?.RenewTimeouts();
        return result;
    }

    internal static bool TryUseExistingFromUse<T>(this IComputed<T>? existing, ComputeContext context, IComputed? usedBy)
    {
        if (existing == null || !existing.IsConsistent())
            return false;
        context.TryCapture(existing);
        ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) existing!);
        ((IComputedImpl?) existing)?.RenewTimeouts();
        return true;
    }

    internal static async ValueTask<ResultBox<T>?> TryUseExistingFromUse<T>(
        this IAsyncComputed<T>? existing, ComputeContext context, IComputed? usedBy,
        CancellationToken cancellationToken)
    {
        if (existing == null || !existing.IsConsistent())
            return null;

        var result = existing.MaybeOutput;
        if (result == null) {
            result = await existing.GetOutput(cancellationToken).ConfigureAwait(false);
            if (result == null)
                return null;
        }

        context.TryCapture(existing);
        ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) existing!);
        ((IComputedImpl?) existing)?.RenewTimeouts();
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void UseNew<T>(this IComputed<T> computed, ComputeContext context, IComputed? usedBy)
    {
        ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) computed);
        ((IComputedImpl?) computed)?.RenewTimeouts();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T Strip<T>(this IComputed<T>? computed, ComputeContext context)
    {
        if (computed == null)
            return default!;
        if (CallOptions.Invalidate == (context.CallOptions & CallOptions.Invalidate))
            return default!;
        return computed.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Task<T> StripToTask<T>(this IComputed<T>? computed, ComputeContext context)
    {
        if (computed == null)
            return TaskCache<T>.DefaultResultTask;
        if (CallOptions.Invalidate == (context.CallOptions & CallOptions.Invalidate))
            return TaskCache<T>.DefaultResultTask;
        return computed.Output.AsTask();
    }
}
