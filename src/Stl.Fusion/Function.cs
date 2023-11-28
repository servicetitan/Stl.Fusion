using Stl.Fusion.Internal;
using Stl.Internal;
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

public abstract class FunctionBase<T>(IServiceProvider services) : IFunction<T>
{
    protected static AsyncLockSet<ComputedInput> InputLocks => ComputedRegistry.Instance.InputLocks;

    private ILogger? _log;
    private ValueOf<ILogger?>? _debugLog;

    protected ILogger Log => _log ??= Services.LogFor(GetType());
    protected ILogger? DebugLog => (_debugLog ??= ValueOf.New(Log.IfEnabled(LogLevel.Debug))).Value;

    public IServiceProvider Services { get; } = services;

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

        var computed = GetExisting(input);
        if (computed.TryUseExisting(context, usedBy))
            return computed!;

        using var releaser = await InputLocks.Lock(input, cancellationToken).ConfigureAwait(false);

        computed = GetExisting(input);
        if (computed.TryUseExistingFromLock(context, usedBy))
            return computed!;

        releaser.MarkLockedLocally();
        computed = await Compute(input, computed, cancellationToken).ConfigureAwait(false);
        computed.UseNew(context, usedBy);
        return computed;
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
        var computed = GetExisting(input);
        return computed.TryUseExisting(context, usedBy)
            ? computed.StripToTask(context)
            : TryRecompute(input, usedBy, context, cancellationToken);
    }

    protected async Task<T> TryRecompute(ComputedInput input,
        IComputed? usedBy,
        ComputeContext context,
        CancellationToken cancellationToken = default)
    {
        using var releaser = await InputLocks.Lock(input, cancellationToken).ConfigureAwait(false);

        var computed = GetExisting(input);
        if (computed.TryUseExistingFromLock(context, usedBy))
            return computed.Strip(context);

        releaser.MarkLockedLocally();
        computed = await Compute(input, computed, cancellationToken).ConfigureAwait(false);
        computed.UseNew(context, usedBy);
        return computed.Value;
    }

    protected Computed<T>? GetExisting(ComputedInput input)
        => input.GetExistingComputed() as Computed<T>;

    // Protected & private

    protected abstract ValueTask<Computed<T>> Compute(
        ComputedInput input, Computed<T>? existing, CancellationToken cancellationToken);
}
