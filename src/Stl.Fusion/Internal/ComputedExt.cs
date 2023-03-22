namespace Stl.Fusion.Internal;

public static class ComputedExt
{
    private static class TaskCache<T>
    {
        public static readonly Task<T> DefaultResultTask = Task.FromResult(default(T)!);
    }

    internal static bool TryUseExisting<T>(this Computed<T>? existing, ComputeContext context, IComputed? usedBy)
    {
        var callOptions = context.CallOptions;
        var mustUseExisting = (callOptions & CallOptions.GetExisting) != 0;

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
            ((IComputedImpl?) usedBy)?.AddUsed(existing);
        ((IComputedImpl) existing).RenewTimeouts(false);
        return true;
    }

    internal static bool TryUseExistingFromUse<T>(this Computed<T>? existing, ComputeContext context, IComputed? usedBy)
    {
        if (existing == null || !existing.IsConsistent())
            return false;

        context.TryCapture(existing);
        ((IComputedImpl?) usedBy)?.AddUsed(existing);
        ((IComputedImpl?) existing)?.RenewTimeouts(false);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void UseNew<T>(this Computed<T> computed, ComputeContext context, IComputed? usedBy)
    {
        ((IComputedImpl?) usedBy)?.AddUsed(computed);
        ((IComputedImpl?) computed)?.RenewTimeouts(true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T Strip<T>(this Computed<T>? computed, ComputeContext context)
    {
        if (computed == null)
            return default!;
        if (CallOptions.GetExisting == (context.CallOptions & CallOptions.GetExisting))
            return default!;
        return computed.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Task<T> StripToTask<T>(this Computed<T>? computed, ComputeContext context)
    {
        if (computed == null)
            return TaskCache<T>.DefaultResultTask;
        if (CallOptions.GetExisting == (context.CallOptions & CallOptions.GetExisting))
            return TaskCache<T>.DefaultResultTask;
        return computed.OutputAsTask;
    }
}
