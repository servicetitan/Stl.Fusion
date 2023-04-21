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
        var mustGetExisting = (callOptions & CallOptions.GetExisting) != 0;

        if (existing == null)
            return mustGetExisting;

        var mustInvalidate = (callOptions & CallOptions.Invalidate) == CallOptions.Invalidate;
        if (mustInvalidate) {
            // CallOptions.Invalidate is:
            // - always paired with CallOptions.GetExisting 
            // - never paired with CallOptions.Capture 
            existing.Invalidate();
            return true;
        }

        // CallOptions.GetExisting | CallOptions.Capture can be intact from here
        if (mustGetExisting) {
            context.Capture(existing);
            ((IComputedImpl)existing).RenewTimeouts(false);
            return true;
        }

        // Only CallOptions.Capture can be intact from here

        // The remaining part of this method matches exactly to TryUseExistingFromLock 
        if (!existing.IsConsistent())
            return false;

        existing.UseNew(context, usedBy);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryUseExistingFromLock<T>(this Computed<T>? existing, ComputeContext context, IComputed? usedBy)
    {
        // We know that:
        // - CallOptions.GetExisting is unused here - it always leads to true in TryUseExisting,
        //   so we simply won't get to this method if it was used
        // - Since CallOptions.Invalidate implies GetExisting, it also can't be used here
        // So the only possible option is CallOptions.Capture 
        if (existing == null || !existing.IsConsistent())
            return false;

        existing.UseNew(context, usedBy);
        return true;
    }

    internal static void UseNew<T>(this Computed<T> computed, ComputeContext context, IComputed? usedBy)
    {
        if (usedBy != null)
            ((IComputedImpl)usedBy).AddUsed(computed);
        ((IComputedImpl)computed).RenewTimeouts(true);
        context.Capture(computed);
    }

    internal static T Strip<T>(this Computed<T>? computed, ComputeContext context)
    {
        if (computed == null)
            return default!;
        if (CallOptions.GetExisting == (context.CallOptions & CallOptions.GetExisting))
            return default!;
        return computed.Value;
    }

    internal static Task<T> StripToTask<T>(this Computed<T>? computed, ComputeContext context)
    {
        if (computed == null)
            return TaskCache<T>.DefaultResultTask;
        if (CallOptions.GetExisting == (context.CallOptions & CallOptions.GetExisting))
            return TaskCache<T>.DefaultResultTask;
        return computed.OutputAsTask;
    }
}
