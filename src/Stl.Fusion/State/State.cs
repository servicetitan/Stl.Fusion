using System.Diagnostics.CodeAnalysis;
using Stl.Conversion;
using Stl.Fusion.Internal;
using Stl.Locking;
using Stl.Versioning;

namespace Stl.Fusion;

public interface IState : IResult, IHasServices
{
    public interface IOptions
    {
        ComputedOptions ComputedOptions { get; init; }
        VersionGenerator<LTag>? VersionGenerator { get; init; }
        Action<IState>? EventConfigurator { get; init; }
        string? Category { get; init; }
    }

    IStateSnapshot Snapshot { get; }
    IComputed Computed { get; }
    object? LastNonErrorValue { get; }

    event Action<IState, StateEventKind>? Invalidated;
    event Action<IState, StateEventKind>? Updating;
    event Action<IState, StateEventKind>? Updated;
}

public interface IState<T> : IState, IResult<T>
{
    new StateSnapshot<T> Snapshot { get; }
    new Computed<T> Computed { get; }
    new T LastNonErrorValue { get; }

    new event Action<IState<T>, StateEventKind>? Invalidated;
    new event Action<IState<T>, StateEventKind>? Updating;
    new event Action<IState<T>, StateEventKind>? Updated;
}

public abstract class State<T> : ComputedInput,
    IState<T>,
    IEquatable<State<T>>,
    IFunction<T>
{
    public record Options : IState.IOptions
    {
        public ComputedOptions ComputedOptions { get; init; } = ComputedOptions.Default;
        public VersionGenerator<LTag>? VersionGenerator { get; init; }
        public Result<T> InitialOutput { get; init; } = default;
        public string? Category { get; init; }

        public T InitialValue {
            get => InitialOutput.ValueOrDefault!;
            init => InitialOutput = new Result<T>(value, null);
        }

        public Action<IState<T>>? EventConfigurator { get; init; }
        Action<IState>? IState.IOptions.EventConfigurator { get; init; }
    }

    private volatile StateSnapshot<T>? _snapshot;
    private string? _category;
    private ILogger? _log;

    protected VersionGenerator<LTag> VersionGenerator { get; set; }
    protected ComputedOptions ComputedOptions { get; }
    protected AsyncLock AsyncLock { get; }
    protected object Lock => AsyncLock;
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public IServiceProvider Services { get; }
    public StateSnapshot<T> Snapshot => _snapshot!;

    public override string Category {
        get => _category ??= GetType().GetName();
        init => _category = value;
    }

    public Computed<T> Computed {
        get => Snapshot.Computed;
        protected set {
            value.AssertConsistencyStateIsNot(ConsistencyState.Computing);
            lock (Lock) {
                var prevSnapshot = _snapshot;
                if (prevSnapshot != null) {
                    prevSnapshot.Computed.Invalidate();
                    _snapshot = new StateSnapshot<T>(prevSnapshot, value);
                }
                else
                    _snapshot = new StateSnapshot<T>(this, value);
                OnSetSnapshot(_snapshot, prevSnapshot);
            }
        }
    }

    public T? ValueOrDefault => Computed.ValueOrDefault;
    public T Value => Computed.Value;
    public Exception? Error => Computed.Error;
    public bool HasValue => Computed.HasValue;
    public bool HasError => Computed.HasError;
    public T LastNonErrorValue => Snapshot.LastNonErrorComputed.Value;

    IStateSnapshot IState.Snapshot => Snapshot;
    Computed<T> IState<T>.Computed => Computed;
    IComputed IState.Computed => Computed;
    // ReSharper disable once HeapView.PossibleBoxingAllocation
    object? IState.LastNonErrorValue => LastNonErrorValue;
    // ReSharper disable once HeapView.PossibleBoxingAllocation
    object? IResult.UntypedValue => Computed.Value;

    public event Action<IState<T>, StateEventKind>? Invalidated;
    public event Action<IState<T>, StateEventKind>? Updating;
    public event Action<IState<T>, StateEventKind>? Updated;

    event Action<IState, StateEventKind>? IState.Invalidated {
        add => UntypedInvalidated += value;
        remove => UntypedInvalidated -= value;
    }
    event Action<IState, StateEventKind>? IState.Updating {
        add => UntypedUpdating += value;
        remove => UntypedUpdating -= value;
    }
    event Action<IState, StateEventKind>? IState.Updated {
        add => UntypedUpdated += value;
        remove => UntypedUpdated -= value;
    }

    protected event Action<IState<T>, StateEventKind>? UntypedInvalidated;
    protected event Action<IState<T>, StateEventKind>? UntypedUpdating;
    protected event Action<IState<T>, StateEventKind>? UntypedUpdated;

    protected State(Options options, IServiceProvider services, bool initialize = true)
    {
        Initialize(this, RuntimeHelpers.GetHashCode(this));
        Services = services;
        _category = options.Category;
        ComputedOptions = options.ComputedOptions;
        VersionGenerator = options.VersionGenerator ?? services.VersionGenerator<LTag>();
        options.EventConfigurator?.Invoke(this);
        var untypedOptions = (IState.IOptions) options;
        untypedOptions.EventConfigurator?.Invoke(this);

        AsyncLock = new AsyncLock(ReentryMode.CheckedFail);
        // ReSharper disable once VirtualMemberCallInConstructor
        if (initialize) Initialize(options);
    }

    public void Deconstruct(out T value, out Exception? error)
        => Computed.Deconstruct(out value, out error);

    public bool IsValue(out T value)
        => Computed.IsValue(out value!);
    public bool IsValue([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out Exception error)
        => Computed.IsValue(out value, out error);

    public Result<T> AsResult()
        => Computed.AsResult();
    public Result<TOther> Cast<TOther>()
        => Computed.Cast<TOther>();
    T IConvertibleTo<T>.Convert() => Value;
    Result<T> IConvertibleTo<Result<T>>.Convert() => AsResult();

    // Equality

    public bool Equals(State<T>? other)
        => ReferenceEquals(this, other);
    public override bool Equals(ComputedInput? other)
        => ReferenceEquals(this, other);
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj);
    public override int GetHashCode()
        => base.GetHashCode();

    // Protected methods

    protected virtual void Initialize(Options options)
    {
        var computed = CreateComputed();
        computed.TrySetOutput(options.InitialOutput);
        Computed = computed;
        if (this is not IMutableState)
            computed.Invalidate();
    }

    protected internal virtual void OnInvalidated(Computed<T> computed)
    {
        var snapshot = Snapshot;
        if (computed != snapshot.Computed)
            return;

        try {
            Invalidated?.Invoke(this, StateEventKind.Invalidated);
            UntypedInvalidated?.Invoke(this, StateEventKind.Invalidated);
        }
        catch (Exception e) {
            Log.LogError(e, "Invalidated / UntypedInvalidated handler failed");
        }
    }

    protected virtual void OnUpdating(Computed<T> computed)
    {
        var snapshot = Snapshot;
        if (computed != snapshot.Computed)
            return;

        try {
            snapshot.OnUpdating();
            Updating?.Invoke(this, StateEventKind.Updating);
            UntypedUpdating?.Invoke(this, StateEventKind.Updating);
        }
        catch (Exception e) {
            Log.LogError(e, "Updating / UntypedUpdating handler failed");
        }
    }

    protected virtual void OnSetSnapshot(StateSnapshot<T> snapshot, StateSnapshot<T>? prevSnapshot)
    {
        if (prevSnapshot == null)
            // First assignment / initialization
            return;

        try {
            prevSnapshot.OnUpdated();
            Updated?.Invoke(this, StateEventKind.Updated);
            UntypedUpdated?.Invoke(this, StateEventKind.Updated);
        }
        catch (Exception e) {
            Log.LogError(e, "Updated / UntypedUpdated handler failed");
        }
    }

    // ComputedInput

    public override IComputed? GetExistingComputed()
        => _snapshot?.Computed;

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

    protected virtual async ValueTask<Computed<T>> Invoke(
        IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        context ??= ComputeContext.Current;

        var computed = Computed;
        if (computed.TryUseExisting(context, usedBy))
            return computed;

        using var _ = await AsyncLock.Lock(cancellationToken).ConfigureAwait(false);

        computed = Computed;
        if (computed.TryUseExistingFromLock(context, usedBy))
            return computed;

        OnUpdating(computed);
        computed = await GetComputed(cancellationToken).ConfigureAwait(false);
        computed.UseNew(context, usedBy);
        return computed;
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

    protected virtual Task<T> InvokeAndStrip(
        IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        context ??= ComputeContext.Current;

        var result = Computed;
        return result.TryUseExisting(context, usedBy)
            ? result.StripToTask(context)
            : TryRecompute(usedBy, context, cancellationToken);
    }

    protected async Task<T> TryRecompute(
        IComputed? usedBy, ComputeContext context,
        CancellationToken cancellationToken)
    {
        using var _ = await AsyncLock.Lock(cancellationToken).ConfigureAwait(false);

        var computed = Computed;
        if (computed.TryUseExistingFromLock(context, usedBy))
            return computed.Strip(context);

        OnUpdating(computed);
        computed = await GetComputed(cancellationToken).ConfigureAwait(false);
        computed.UseNew(context, usedBy);
        return computed.Value;
    }

    protected async ValueTask<StateBoundComputed<T>> GetComputed(CancellationToken cancellationToken)
    {
        var computed = CreateComputed();
        using (Fusion.Computed.ChangeCurrent(computed)) {
            try {
                var value = await Compute(cancellationToken).ConfigureAwait(false);
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

    protected abstract Task<T> Compute(CancellationToken cancellationToken);

    protected virtual StateBoundComputed<T> CreateComputed()
        => new(ComputedOptions, this, VersionGenerator.NextVersion());
}
