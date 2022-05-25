using Stl.Fusion.Internal;

namespace Stl.Fusion;

public static class Computed
{
    private static readonly AsyncLocal<IComputed?> CurrentLocal = new();

    // GetCurrent & ChangeCurrent

    public static IComputed? GetCurrent() => CurrentLocal.Value;

    public static IComputed<T> GetCurrent<T>()
    {
        var untypedCurrent = GetCurrent();
        if (untypedCurrent is IComputed<T> c)
            return c;
        if (untypedCurrent == null)
            throw Errors.ComputedCurrentIsNull();
        throw Errors.ComputedCurrentIsOfIncompatibleType(typeof(IComputed<T>));
    }

    public static ClosedDisposable<IComputed?> ChangeCurrent(IComputed? newCurrent)
    {
        var oldCurrent = GetCurrent();
        if (newCurrent != null)
            ComputeContext.Current.TryCapture(newCurrent);
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

    public static async ValueTask<Option<IComputed>> TryCapture(
        Func<CancellationToken, Task> producer,
        CancellationToken cancellationToken = default)
    {
        using var ccs = BeginCapture();
        IComputed result;
        try {
            await producer(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception) {
            if (ccs.Context.TryGetCaptured(out result!) && result.HasError)
                return Option.Some(result); // Return the original error, if possible
            throw;
        }
        return ccs.Context.TryGetCaptured(out result!) ? Option.Some(result) : default;
    }

    public static async ValueTask<Option<IComputed<T>>> TryCapture<T>(
        Func<CancellationToken, Task<T>> producer,
        CancellationToken cancellationToken = default)
    {
        using var ccs = BeginCapture();
        IComputed<T> result;
        try {
            await producer(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception) {
            if (ccs.Context.TryGetCaptured(out result!) && result.HasError)
                return Option.Some(result); // Return the original error, if possible
            throw;
        }
        return ccs.Context.TryGetCaptured(out result!) ? Option.Some(result) : default;
    }

    public static async ValueTask<Option<IComputed>> TryCapture(
        Func<CancellationToken, ValueTask> producer,
        CancellationToken cancellationToken = default)
    {
        using var ccs = BeginCapture();
        IComputed result;
        try {
            await producer(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception) {
            if (ccs.Context.TryGetCaptured(out result!) && result.HasError)
                return Option.Some(result); // Return the original error, if possible
            throw;
        }
        return ccs.Context.TryGetCaptured(out result!) ? Option.Some(result) : default;
    }

    public static async ValueTask<Option<IComputed<T>>> TryCapture<T>(
        Func<CancellationToken, ValueTask<T>> producer,
        CancellationToken cancellationToken = default)
    {
        using var ccs = BeginCapture();
        IComputed<T> result;
        try {
            await producer(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception) {
            if (ccs.Context.TryGetCaptured(out result!) && result.HasError)
                return Option.Some(result); // Return the original error, if possible
            throw;
        }
        return ccs.Context.TryGetCaptured(out result!) ? Option.Some(result) : default;
    }

    // Capture

    public static async ValueTask<IComputed> Capture(Func<CancellationToken, Task> producer, CancellationToken cancellationToken = default)
    {
        using var ccs = BeginCapture();
        try {
            await producer(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception) {
            if (ccs.Context.TryGetCaptured(out var result) && result.HasError)
                return result; // Return the original error, if possible
            throw;
        }
        return ccs.Context.GetCaptured();
    }

    public static async ValueTask<IComputed<T>> Capture<T>(Func<CancellationToken, Task<T>> producer, CancellationToken cancellationToken = default)
    {
        using var ccs = BeginCapture();
        try {
            await producer(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception) {
            if (ccs.Context.TryGetCaptured<T>(out var result) && result.HasError)
                return result; // Return the original error, if possible
            throw;
        }
        return ccs.Context.GetCaptured<T>();
    }

    public static async ValueTask<IComputed> Capture(Func<CancellationToken, ValueTask> producer, CancellationToken cancellationToken = default)
    {
        using var ccs = BeginCapture();
        try {
            await producer(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception) {
            if (ccs.Context.TryGetCaptured(out var result) && result.HasError)
                return result; // Return the original error, if possible
            throw;
        }
        return ccs.Context.GetCaptured();
    }

    public static async ValueTask<IComputed<T>> Capture<T>(Func<CancellationToken, ValueTask<T>> producer, CancellationToken cancellationToken = default)
    {
        using var ccs = BeginCapture();
        try {
            await producer(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception) {
            if (ccs.Context.TryGetCaptured<T>(out var result) && result.HasError)
                return result; // Return the original error, if possible
            throw;
        }
        return ccs.Context.GetCaptured<T>();
    }

    // GetExisting

    public static IComputed<T>? GetExisting<T>(Func<Task<T>> producer)
    {
        using var ccs = ComputeContext.New(CallOptions.GetExisting | CallOptions.Capture).Activate();
        var task = producer();
        task.AssertCompleted(); // The must be always synchronous in this case
        return ccs.Context.TryGetCaptured<T>(out var result) ? result : default;
    }

    public static IComputed<T>? GetExisting<T>(Func<ValueTask<T>> producer)
    {
        using var ccs = ComputeContext.New(CallOptions.GetExisting | CallOptions.Capture).Activate();
        var task = producer();
        task.AssertCompleted(); // The must be always synchronous in this case
        return ccs.Context.TryGetCaptured<T>(out var result) ? result : default;
    }
}
