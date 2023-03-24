using Stl.Fusion.Internal;
using Stl.Locking;
using Stl.Versioning;

namespace Stl.Fusion;

public interface IAnonymousComputedSource : IFunction
{
    ComputedOptions ComputedOptions { get; init; }
    VersionGenerator<LTag> VersionGenerator { get; init; }
}

public class AnonymousComputedSource<T> : ComputedInput,
    IFunction<T>, IAnonymousComputedSource,
    IEquatable<AnonymousComputedSource<T>>
{
    private volatile AnonymousComputed<T>? _computed;
    private string? _category;
    private ILogger? _log;

    protected AsyncLock AsyncLock { get; }
    protected object Lock => AsyncLock;
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public IServiceProvider Services { get; }

    public override string Category {
        get => _category ??= GetType().GetName();
        init => _category = value;
    }

    public ComputedOptions ComputedOptions { get; init; }
    public VersionGenerator<LTag> VersionGenerator { get; init; }
    public Func<AnonymousComputedSource<T>, CancellationToken, ValueTask<T>> Computer { get; }
    public event Action<AnonymousComputed<T>>? Invalidated;
    public event Action<AnonymousComputed<T>>? Updated;

    public bool IsComputed => _computed != null;
    public AnonymousComputed<T> Computed {
        get => _computed ?? throw Errors.AnonymousComputedSourceIsNotComputedYet();
        private set {
            if (value?.Source != this)
                throw new ArgumentOutOfRangeException(nameof(value));
            value.AssertConsistencyStateIsNot(ConsistencyState.Computing);
            lock (Lock) {
                if (value == _computed)
                    return;

                _computed?.Invalidate();
                _computed = value;
                Updated?.Invoke(value);
            }
        }
    }

    public AnonymousComputedSource(
        IServiceProvider services,
        Func<AnonymousComputedSource<T>, CancellationToken, ValueTask<T>> computer,
        string? category = null)
    {
        Services = services;
        Computer = computer;
        _category = category;

        ComputedOptions = ComputedOptions.Default;
        VersionGenerator = services.VersionGenerator<LTag>();
        AsyncLock = new AsyncLock(ReentryMode.CheckedFail);
        Initialize(this, RuntimeHelpers.GetHashCode(this));
    }

    // ComputedInput

    public override IComputed? GetExistingComputed() => Computed;

    // Update & Use

    public async ValueTask<Computed<T>> Update(CancellationToken cancellationToken = default)
    {
        using var scope = ComputeContext.Suppress();
        return await Invoke(null, scope.Context, cancellationToken).ConfigureAwait(false);
    }

    public virtual async ValueTask<T> Use(CancellationToken cancellationToken = default)
    {
        var usedBy = Stl.Fusion.Computed.GetCurrent();
        var context = ComputeContext.Current;
        if ((context.CallOptions & CallOptions.GetExisting) != 0) // Both GetExisting & Invalidate
            throw Errors.InvalidContextCallOptions(context.CallOptions);

        var computed = _computed;
        if (computed?.IsConsistent() == true && computed.TryUseExistingFromUse(context, usedBy))
            return computed.Value;

        computed = (AnonymousComputed<T>) await Invoke(usedBy, context, cancellationToken).ConfigureAwait(false);
        return computed.Value;
    }

    // Equality

    public bool Equals(AnonymousComputedSource<T>? other)
        => ReferenceEquals(this, other);
    public override bool Equals(ComputedInput? other)
        => ReferenceEquals(this, other);
    public override bool Equals(object? other)
        => ReferenceEquals(this, other);
    public override int GetHashCode()
        => HashCode;

    // Private methods

    private async ValueTask<Computed<T>> Invoke(
        IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        context ??= ComputeContext.Current;

        var result = _computed;
        if (result.TryUseExisting(context, usedBy))
            return result!;

        using var _ = await AsyncLock.Lock(cancellationToken).ConfigureAwait(false);

        result = _computed;
        if (result.TryUseExisting(context, usedBy))
            return result!;

        result = await GetComputed(cancellationToken).ConfigureAwait(false);
        result.UseNew(context, usedBy);
        return result;
    }

    private Task<T> InvokeAndStrip(
        IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        context ??= ComputeContext.Current;

        var result = _computed;
        return result.TryUseExisting(context, usedBy)
            ? result.StripToTask(context)
            : TryRecompute(usedBy, context, cancellationToken);
    }

    private async Task<T> TryRecompute(
        IComputed? usedBy, ComputeContext context,
        CancellationToken cancellationToken)
    {
        using var _ = await AsyncLock.Lock(cancellationToken).ConfigureAwait(false);

        var result = _computed;
        if (result.TryUseExisting(context, usedBy))
            return result.Strip(context);

        result = await GetComputed(cancellationToken).ConfigureAwait(false);
        result.UseNew(context, usedBy);
        return result.Value;
    }

    private async ValueTask<AnonymousComputed<T>> GetComputed(CancellationToken cancellationToken)
    {
        var computed = new AnonymousComputed<T>(ComputedOptions, this, VersionGenerator.NextVersion());
        using (Fusion.Computed.ChangeCurrent(computed)) {
            try {
                var value = await Computer.Invoke(this, cancellationToken).ConfigureAwait(false);
                computed.TrySetOutput(Result.New(value));
            }
            catch (Exception e) when (e is not OperationCanceledException) {
                computed.TrySetOutput(Result.Error<T>(e));
            }
        }

        // It's super important to make "Computed = computed" assignment after "using" block -
        // otherwise all State events will be triggered while Computed.Current still points on
        // computed (which is already computed), so if any compute method runs inside
        // the event handler, it will fail on attempt to add a dependency.
        Computed = computed;
        return computed;
    }

    internal void OnInvalidated(AnonymousComputed<T> computed)
    {
        try {
            Invalidated?.Invoke(computed);
        }
        catch (Exception e) {
            Log.LogError(e, "Invalidated handler failed");
        }
    }

    // IFunction<T> & IFunction

    ValueTask<Computed<T>> IFunction<T>.Invoke(ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(input, this))
            // This "Function" supports just a single input == this
            throw new ArgumentOutOfRangeException(nameof(input));

        return Invoke(usedBy, context, cancellationToken);
    }

    async ValueTask<IComputed> IFunction.Invoke(ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(input, this))
            // This "Function" supports just a single input == this
            throw new ArgumentOutOfRangeException(nameof(input));

        return await Invoke(usedBy, context, cancellationToken).ConfigureAwait(false);
    }

    async Task IFunction.InvokeAndStrip(
        ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(input, this))
            // This "Function" supports just a single input == this
            throw new ArgumentOutOfRangeException(nameof(input));

        await InvokeAndStrip(usedBy, context, cancellationToken).ConfigureAwait(false);
    }

    Task<T> IFunction<T>.InvokeAndStrip(ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(input, this))
            // This "Function" supports just a single input == this
            throw new ArgumentOutOfRangeException(nameof(input));

        return InvokeAndStrip(usedBy, context, cancellationToken);
    }
}
