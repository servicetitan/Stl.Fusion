using Stl.Fusion.Internal;
using Stl.Locking;

namespace Stl.Fusion;

public interface IFunction : IHasServices
{
    ValueTask<IComputed> Invoke(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default);
    Task InvokeAndStrip(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default);
}

public interface IFunction<T> : IFunction
{
    new ValueTask<Computed<T>> Invoke(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default);
    new Task<T> InvokeAndStrip(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default);
}

public abstract class FunctionBase<T> : IFunction<T>
{
    protected static AsyncLockSet<ComputedInput> InputLocks => ComputedRegistry.Instance.InputLocks;
    protected readonly ILogger Log;
    protected readonly ILogger? DebugLog;

    public IServiceProvider Services { get; }

    protected FunctionBase(IServiceProvider services)
    {
        Services = services;
        Log = Services.LogFor(GetType());
        DebugLog = Log.IsLogging(LogLevel.Debug) ? Log : null;
    }

    async ValueTask<IComputed> IFunction.Invoke(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken)
        => await Invoke(input, usedBy, context, cancellationToken).ConfigureAwait(false);

    public virtual async ValueTask<Computed<T>> Invoke(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default)
    {
        context ??= ComputeContext.Current;

        // Read-Lock-RetryRead-Compute-Store pattern

        var result = GetExisting(input);
        if (result.TryUseExisting(context, usedBy))
            return result!;

        using var _ = await InputLocks.Lock(input, cancellationToken).ConfigureAwait(false);

        result = GetExisting(input);
        if (result.TryUseExisting(context, usedBy))
            return result!;

        result = await Compute(input, result, cancellationToken).ConfigureAwait(false);
        result.UseNew(context, usedBy);
        return result;
    }

    Task IFunction.InvokeAndStrip(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken)
        => InvokeAndStrip(input, usedBy, context, cancellationToken);

    public virtual Task<T> InvokeAndStrip(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default)
    {
        context ??= ComputeContext.Current;

        var result = GetExisting(input);
        return result.TryUseExisting(context, usedBy)
            ? result.StripToTask(context)
            : TryRecompute(input, usedBy, context, cancellationToken);
    }

    protected async Task<T> TryRecompute(ComputedInput input,
        IComputed? usedBy,
        ComputeContext context,
        CancellationToken cancellationToken = default)
    {
        using var _ = await InputLocks.Lock(input, cancellationToken).ConfigureAwait(false);

        var result = GetExisting(input);
        if (result.TryUseExisting(context, usedBy))
            return result.Strip(context);

        result = await Compute(input, result, cancellationToken).ConfigureAwait(false);
        var output = result.Output; // It can't be gone here b/c KeepAlive isn't called yet
        result.UseNew(context, usedBy);
        return output.Value;
    }

    protected Computed<T>? GetExisting(ComputedInput input)
        => input.GetExistingComputed() as Computed<T>;

    // Protected & private

    protected abstract ValueTask<Computed<T>> Compute(
        ComputedInput input, Computed<T>? existing, CancellationToken cancellationToken);
}
