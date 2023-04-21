using Stl.Fusion.Internal;

namespace Stl.Fusion;

public static class Computed
{
    private static readonly AsyncLocal<IComputed?> CurrentLocal = new();

    public static TimeSpan PreciseInvalidationDelayThreshold { get; set; } = TimeSpan.FromSeconds(1);

    // GetCurrent & ChangeCurrent

    public static IComputed? GetCurrent() => CurrentLocal.Value;

    public static Computed<T> GetCurrent<T>()
    {
        var untypedCurrent = GetCurrent();
        if (untypedCurrent is Computed<T> c)
            return c;
        if (untypedCurrent == null)
            throw Errors.ComputedCurrentIsNull();
        throw Errors.ComputedCurrentIsOfIncompatibleType(typeof(Computed<T>));
    }

    public static ClosedDisposable<IComputed?> ChangeCurrent(IComputed? newCurrent)
    {
        var oldCurrent = GetCurrent();
        if (newCurrent != null)
            ComputeContext.Current.Capture(newCurrent);
        if (oldCurrent == newCurrent)
            return default;
        CurrentLocal.Value = newCurrent;
        return Disposable.NewClosed(oldCurrent, oldCurrent1 => CurrentLocal.Value = oldCurrent1);
    }

    public static ClosedDisposable<IComputed?> SuspendDependencyCapture()
        => ChangeCurrent(null);

    // Invalidation

    public static bool IsInvalidating()
        => (ComputeContext.Current.CallOptions & CallOptions.Invalidate) == CallOptions.Invalidate;

    public static ComputeContextScope Invalidate()
        => ComputeContext.Invalidate.Activate();
    public static ComputeContextScope SuspendInvalidate()
        => ComputeContext.Default.Activate();

    // BeginCapture (sync Capture API)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComputeContextScope BeginCapture()
        => ComputeContext.New(CallOptions.Capture).Activate();

    // TryCapture

    public static async ValueTask<Option<IComputed>> TryCapture(Func<Task> producer)
    {
        using var ccs = BeginCapture();
        IComputed result;
        try {
            await producer().ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            if (ccs.Context.TryGetCaptured(out result!) && result.HasError)
                return Option.Some(result); // Return the original error, if possible
            throw;
        }
        return ccs.Context.TryGetCaptured(out result!) ? Option.Some(result) : default;
    }

    public static async ValueTask<Option<Computed<T>>> TryCapture<T>(Func<Task<T>> producer)
    {
        using var ccs = BeginCapture();
        Computed<T> result;
        try {
            await producer().ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            if (ccs.Context.TryGetCaptured(out result!) && result.HasError)
                return Option.Some(result); // Return the original error, if possible
            throw;
        }
        return ccs.Context.TryGetCaptured(out result!) ? Option.Some(result) : default;
    }

    public static async ValueTask<Option<IComputed>> TryCapture(Func<ValueTask> producer)
    {
        using var ccs = BeginCapture();
        IComputed result;
        try {
            await producer().ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            if (ccs.Context.TryGetCaptured(out result!) && result.HasError)
                return Option.Some(result); // Return the original error, if possible
            throw;
        }
        return ccs.Context.TryGetCaptured(out result!) ? Option.Some(result) : default;
    }

    public static async ValueTask<Option<Computed<T>>> TryCapture<T>(Func<ValueTask<T>> producer)
    {
        using var ccs = BeginCapture();
        Computed<T> result;
        try {
            await producer().ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            if (ccs.Context.TryGetCaptured(out result!) && result.HasError)
                return Option.Some(result); // Return the original error, if possible
            throw;
        }
        return ccs.Context.TryGetCaptured(out result!) ? Option.Some(result) : default;
    }

    // Capture

    public static async ValueTask<IComputed> Capture(Func<Task> producer)
    {
        using var ccs = BeginCapture();
        try {
            await producer().ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            if (ccs.Context.TryGetCaptured(out var result) && result.HasError)
                return result; // Return the original error, if possible
            throw;
        }
        return ccs.Context.GetCaptured();
    }

    public static async ValueTask<Computed<T>> Capture<T>(Func<Task<T>> producer)
    {
        using var ccs = BeginCapture();
        try {
            await producer().ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            if (ccs.Context.TryGetCaptured<T>(out var result) && result.HasError)
                return result; // Return the original error, if possible
            throw;
        }
        return ccs.Context.GetCaptured<T>();
    }

    public static async ValueTask<IComputed> Capture(Func<ValueTask> producer)
    {
        using var ccs = BeginCapture();
        try {
            await producer().ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            if (ccs.Context.TryGetCaptured(out var result) && result.HasError)
                return result; // Return the original error, if possible
            throw;
        }
        return ccs.Context.GetCaptured();
    }

    public static async ValueTask<Computed<T>> Capture<T>(Func<ValueTask<T>> producer)
    {
        using var ccs = BeginCapture();
        try {
            await producer().ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            if (ccs.Context.TryGetCaptured<T>(out var result) && result.HasError)
                return result; // Return the original error, if possible
            throw;
        }
        return ccs.Context.GetCaptured<T>();
    }

    // GetExisting

    public static Computed<T>? GetExisting<T>(Func<Task<T>> producer)
    {
        using var ccs = ComputeContext.New(CallOptions.Capture | CallOptions.GetExisting).Activate();
        var task = producer();
        task.AssertCompleted(); // The must be always synchronous in this case
        return ccs.Context.TryGetCaptured<T>(out var result) ? result : default;
    }

    public static Computed<T>? GetExisting<T>(Func<ValueTask<T>> producer)
    {
        using var ccs = ComputeContext.New(CallOptions.Capture | CallOptions.GetExisting).Activate();
        var task = producer();
        task.AssertCompleted(); // The must be always synchronous in this case
        return ccs.Context.TryGetCaptured<T>(out var result) ? result : default;
    }
}
