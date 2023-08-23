using System.Diagnostics.CodeAnalysis;
using Stl.Collections.Slim;
using Stl.Conversion;
using Stl.Fusion.Internal;
using Stl.Fusion.Operations.Internal;
using Stl.Versioning;
using Errors = Stl.Fusion.Internal.Errors;

namespace Stl.Fusion;

public interface IComputed : IResult, IHasVersion<LTag>
{
    ComputedOptions Options { get; }
    ComputedInput Input { get; }
    ConsistencyState ConsistencyState { get; }
    Type OutputType { get; }
    IResult Output { get; }
    Task OutputAsTask { get; }
    event Action<IComputed> Invalidated;

    void Invalidate(bool immediately = false);
    TResult Apply<TArg, TResult>(IComputedApplyHandler<TArg, TResult> handler, TArg arg);

    ValueTask<IComputed> Update(CancellationToken cancellationToken = default);
    ValueTask<object> Use(CancellationToken cancellationToken = default);
}

public abstract class Computed<T> : IComputedImpl, IResult<T>
{
    private readonly ComputedOptions _options;
    private volatile int _state;
    private volatile ComputedFlags _flags;
    private long _lastKeepAliveSlot;
    private Result<T> _output;
    private Task<T>? _outputAsTask;
    private RefHashSetSlim3<IComputedImpl> _used;
    private HashSetSlim3<(ComputedInput Input, LTag Version)> _usedBy;
    // ReSharper disable once InconsistentNaming
    private InvalidatedHandlerSet _invalidated;

    protected ComputedFlags Flags => _flags;
    protected object Lock => this;

    public ComputedOptions Options => _options;
    public ComputedInput Input { get; }
    public ConsistencyState ConsistencyState => (ConsistencyState) _state;
    public IFunction<T> Function => (IFunction<T>) Input.Function;
    public LTag Version { get; }
    public Type OutputType => typeof(T);

    public virtual Result<T> Output {
        get {
            this.AssertConsistencyStateIsNot(ConsistencyState.Computing);
            return _output;
        }
    }

    public Task<T> OutputAsTask {
        get {
            if (_outputAsTask != null)
                return _outputAsTask;
            lock (Lock) {
                this.AssertConsistencyStateIsNot(ConsistencyState.Computing);
                return _outputAsTask ??= _output.AsTask();
            }
        }
    }

    // IResult<T> properties
    public T? ValueOrDefault => Output.ValueOrDefault;
    public T Value => Output.Value;
    public Exception? Error => Output.Error;
    public bool HasValue => Output.HasValue;
    public bool HasError => Output.HasError;

    // "Untyped" versions of properties
    ComputedInput IComputed.Input => Input;
    // ReSharper disable once HeapView.BoxingAllocation
    IResult IComputed.Output => Output;
    // ReSharper disable once HeapView.BoxingAllocation
    object? IResult.UntypedValue => Output.Value;
    Task IComputed.OutputAsTask => OutputAsTask;

    public event Action<IComputed> Invalidated {
        add {
            if (ConsistencyState == ConsistencyState.Invalidated) {
                value(this);
                return;
            }
            lock (Lock) {
                if (ConsistencyState == ConsistencyState.Invalidated) {
                    value(this);
                    return;
                }
                _invalidated.Add(value);
            }
        }
        remove {
            lock (Lock) {
                if (ConsistencyState == ConsistencyState.Invalidated)
                    return;
                _invalidated.Remove(value);
            }
        }
    }

    protected Computed(ComputedOptions options, ComputedInput input, LTag version)
    {
        _options = options;
        Input = input;
        Version = version;
    }

    protected Computed(
        ComputedOptions options,
        ComputedInput input,
        Result<T> output,
        LTag version,
        bool isConsistent)
    {
        _options = options;
        Input = input;
        _state = (int) (isConsistent ? ConsistencyState.Consistent : ConsistencyState.Invalidated);
        _output = output;
        Version = version;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out T value, out Exception? error)
        => Output.Deconstruct(out value, out error);

    public void Deconstruct(out T value, out Exception? error, out LTag version)
    {
        Output.Deconstruct(out value, out error);
        version = Version;
    }

    public override string ToString()
        => $"{GetType().GetName()}({Input} {Version}, State: {ConsistencyState})";

    public bool TrySetOutput(Result<T> output)
    {
        bool mustInvalidate;
        lock (Lock) {
            if (ConsistencyState != ConsistencyState.Computing)
                return false;

            SetStateUnsafe(ConsistencyState.Consistent);
            _output = output;
            mustInvalidate = (_flags & ComputedFlags.InvalidateOnSetOutput) != 0;
        }

        if (mustInvalidate) {
            Invalidate();
            return true;
        }

        StartAutoInvalidation();
        return true;
    }

    public void Invalidate(bool immediately = false)
    {
        if (ConsistencyState == ConsistencyState.Invalidated)
            return;

        // Debug.WriteLine($"{nameof(Invalidate)}: {this}");
        lock (Lock) {
            var flags = _flags;
            switch (ConsistencyState) {
            case ConsistencyState.Invalidated:
                return;
            case ConsistencyState.Computing:
                flags |= ComputedFlags.InvalidateOnSetOutput;
                if (immediately)
                    flags |= ComputedFlags.InvalidationDelayStarted;
                _flags = flags;
                return;
            }

            // ConsistencyState == ConsistencyState.Computing from here

            immediately |= Options.InvalidationDelay == default;
            if (immediately)
                SetStateUnsafe(ConsistencyState.Invalidated);
            else {
                if ((flags & ComputedFlags.InvalidationDelayStarted) != 0)
                    return; // Already started

                _flags = flags | ComputedFlags.InvalidationDelayStarted;
            }
        }

        if (!immediately) {
            // Delayed invalidation
            this.Invalidate(Options.InvalidationDelay);
            return;
        }

        // Instant invalidation - it may happen just once,
        // so we don't need a lock here.
        try {
            try {
                OnInvalidated();
                _invalidated.Invoke(this);
                _invalidated = default;
            }
            finally {
                // Any code called here may not throw
                _used.Apply(this, (self, c) => c.RemoveUsedBy(self));
                _used.Clear();
                _usedBy.Apply(default(Unit), static (_, usedByEntry) => {
                    var c = usedByEntry.Input.GetExistingComputed();
                    if (c != null && c.Version == usedByEntry.Version)
                        c.Invalidate(); // Invalidate doesn't throw - ever
                });
                _usedBy.Clear();
            }
        }
        catch (Exception e) {
            // We should never throw errors during the invalidation
            try {
                var log = Input.Function.Services.LogFor(GetType());
                log.LogError(e, "Error on invalidation");
            }
            catch {
                // Intended: Invalidate doesn't throw!
            }
        }
    }

    protected virtual void OnInvalidated()
        => CancelTimeouts();

    protected void StartAutoInvalidation()
    {
        if (!this.IsConsistent())
            return;

        var hasTransientError = _output.Error is { } error && IsTransientError(error);
        var timeout = hasTransientError
            ? _options.TransientErrorInvalidationDelay
            : _options.AutoInvalidationDelay;
        if (timeout != TimeSpan.MaxValue)
            this.Invalidate(timeout);
    }

    public void RenewTimeouts(bool isNew)
    {
        if (ConsistencyState == ConsistencyState.Invalidated)
            return; // We shouldn't register miss here, since it's going to be counted as hit anyway

        var minCacheDuration = Options.MinCacheDuration;
        if (minCacheDuration != default) {
            var keepAliveSlot = Timeouts.GetKeepAliveSlot(Timeouts.Clock.Now + minCacheDuration);
            var lastKeepAliveSlot = Interlocked.Exchange(ref _lastKeepAliveSlot, keepAliveSlot);
            if (lastKeepAliveSlot != keepAliveSlot)
                Timeouts.KeepAlive.AddOrUpdateToLater(this, keepAliveSlot);
        }

        ComputedRegistry.Instance.ReportAccess(this, isNew);
    }

    public void CancelTimeouts()
    {
        var options = Options;
        if (options.MinCacheDuration != default) {
            Interlocked.Exchange(ref _lastKeepAliveSlot, 0);
            Timeouts.KeepAlive.Remove(this);
        }
    }

    // Update

    async ValueTask<IComputed> IComputed.Update(CancellationToken cancellationToken)
        => await Update(cancellationToken).ConfigureAwait(false);
    public async ValueTask<Computed<T>> Update(CancellationToken cancellationToken = default)
    {
        if (this.IsConsistent())
            return this;

        using var scope = ComputeContext.Suppress();
        return await Function
            .Invoke(Input, null, scope.Context, cancellationToken)
            .ConfigureAwait(false);
    }

    // Use

    async ValueTask<object> IComputed.Use(CancellationToken cancellationToken)
        => (await Use(cancellationToken).ConfigureAwait(false))!;
    public virtual async ValueTask<T> Use(CancellationToken cancellationToken = default)
    {
        var usedBy = Computed.GetCurrent();
        var context = ComputeContext.Current;
        if ((context.CallOptions & CallOptions.GetExisting) != 0) // Both GetExisting & Invalidate
            throw Errors.InvalidContextCallOptions(context.CallOptions);
        if (this.IsConsistent() && this.TryUseExistingFromLock(context, usedBy))
            return Value;

        var computed = await Function
            .Invoke(Input, usedBy, context, cancellationToken)
            .ConfigureAwait(false);
        return computed.Value;
    }

    // Apply

    public TResult Apply<TArg, TResult>(IComputedApplyHandler<TArg, TResult> handler, TArg arg)
        => handler.Apply(this, arg);

    // IResult<T> methods

    public bool IsValue([MaybeNullWhen(false)] out T value)
        => Output.IsValue(out value);
    public bool IsValue([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out Exception error)
        => Output.IsValue(out value, out error!);
    public Result<T> AsResult()
        => Output.AsResult();
    public Result<TOther> Cast<TOther>()
        => Output.Cast<TOther>();
    T IConvertibleTo<T>.Convert() => Value;
    Result<T> IConvertibleTo<Result<T>>.Convert() => AsResult();

    // IComputedImpl methods

    IComputedImpl[] IComputedImpl.Used {
        get {
            var result = new IComputedImpl[_used.Count];
            lock (Lock) {
                _used.CopyTo(result);
                return result;
            }
        }
    }

    (ComputedInput Input, LTag Version)[] IComputedImpl.UsedBy {
        get {
            var result = new (ComputedInput Input, LTag Version)[_usedBy.Count];
            lock (Lock) {
                _usedBy.CopyTo(result);
                return result;
            }
        }
    }

    void IComputedImpl.AddUsed(IComputedImpl used)
    {
        // Debug.WriteLine($"{nameof(IComputedImpl.AddUsed)}: {this} <- {used}");
        lock (Lock) {
            if (ConsistencyState != ConsistencyState.Computing) {
                // The current computed is either:
                // - Invalidated: nothing to do in this case.
                //   Deps are meaningless for whatever is already invalidated.
                // - Consistent: this means the dependency computation hasn't been completed
                //   while the dependant was computing, which literally means it is actually unused.
                //   This happens e.g. when N tasks to compute dependencies start during the computation,
                //   but only some of them are awaited. Other results might be ignored e.g. because
                //   an exception was thrown in one of early "awaits". And if you "linearize" such a
                //   method, it becomes clear that dependencies that didn't finish by the end of computation
                //   actually aren't used, coz in the "linear" flow they would be requested at some
                //   later point.
                return;
            }
            if (used.AddUsedBy(this))
                _used.Add(used);
        }
    }

    bool IComputedImpl.AddUsedBy(IComputedImpl usedBy)
    {
        lock (Lock) {
            switch (ConsistencyState) {
            case ConsistencyState.Computing:
                throw Errors.WrongComputedState(ConsistencyState);
            case ConsistencyState.Invalidated:
                usedBy.Invalidate();
                return false;
            }

            var usedByRef = (usedBy.Input, usedBy.Version);
            _usedBy.Add(usedByRef);
            return true;
        }
    }

    void IComputedImpl.RemoveUsedBy(IComputedImpl usedBy)
    {
        lock (Lock) {
            if (ConsistencyState == ConsistencyState.Invalidated)
                // _usedBy is already empty or going to be empty soon;
                // moreover, only Invalidated code can modify
                // _used/_usedBy once invalidation flag is set
                return;

            _usedBy.Remove((usedBy.Input, usedBy.Version));
        }
    }

    (int OldCount, int NewCount) IComputedImpl.PruneUsedBy()
    {
        lock (Lock) {
            if (ConsistencyState != ConsistencyState.Consistent)
                // _usedBy is already empty or going to be empty soon;
                // moreover, only Invalidated code can modify
                // _used/_usedBy once invalidation flag is set
                return (0, 0);

            var replacement = new HashSetSlim3<(ComputedInput Input, LTag Version)>();
            var oldCount = _usedBy.Count;
            foreach (var entry in _usedBy.Items) {
                var c = entry.Input.GetExistingComputed();
                if (c != null && c.Version == entry.Version)
                    replacement.Add(entry);
            }
            _usedBy = replacement;
            return (oldCount, _usedBy.Count);
        }
    }

    void IComputedImpl.CopyUsedTo(ref ArrayBuffer<IComputedImpl> buffer)
    {
        lock (Lock) {
            var count = buffer.Count;
            buffer.EnsureCapacity(count + _used.Count);
            _used.CopyTo(buffer.Buffer.AsSpan(count));
        }
    }

    // Protected & private methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetStateUnsafe(ConsistencyState newState)
        => _state = (int)newState;

    bool IComputedImpl.IsTransientError(Exception error) => IsTransientError(error);
    private bool IsTransientError(Exception error)
    {
        ITransientErrorDetector? transientErrorDetector = null;
        try {
            var services = Input.Function.Services;
            transientErrorDetector = services.GetService<ITransientErrorDetector<IComputed>>();
        }
        catch (ObjectDisposedException) {
            // We want to handle IServiceProvider disposal gracefully
        }
        transientErrorDetector ??= TransientErrorDetector.DefaultPreferTransient;
        return transientErrorDetector.IsTransient(error);
    }
}
