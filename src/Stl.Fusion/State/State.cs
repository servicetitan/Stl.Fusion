using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Conversion;
using Stl.DependencyInjection;
using Stl.Fusion.Internal;
using Stl.Locking;
using Stl.Reflection;
using Stl.Versioning;

namespace Stl.Fusion
{
    public interface IState : IResult, IHasServices
    {
        public interface IOptions
        {
            ComputedOptions ComputedOptions { get; set; }
            VersionGenerator<LTag>? VersionGenerator { get; set; }
            Action<IState>? EventConfigurator { get; set; }
            bool InitialIsConsistent { get; set; }
        }

        IStateSnapshot Snapshot { get; }
        IComputed Computed { get; }
        object? LatestNonErrorValue { get; }

        event Action<IState, StateEventKind>? Invalidated;
        event Action<IState, StateEventKind>? Updating;
        event Action<IState, StateEventKind>? Updated;
    }

    public interface IState<T> : IState, IResult<T>
    {
        new StateSnapshot<T> Snapshot { get; }
        new IComputed<T> Computed { get; }
        new T LatestNonErrorValue { get; }

        new event Action<IState<T>, StateEventKind>? Invalidated;
        new event Action<IState<T>, StateEventKind>? Updating;
        new event Action<IState<T>, StateEventKind>? Updated;
    }

    public abstract class State<T> : ComputedInput,
        IState<T>,
        IEquatable<State<T>>,
        IFunction<State<T>, T>
    {
        public class Options : IState.IOptions
        {
            public static readonly Func<IState<T>, Result<T>> DefaultInitialOutputFactory =
                state => Result.Value(ActivatorExt.New<T>(false));

            public ComputedOptions ComputedOptions { get; set; } = ComputedOptions.Default;
            public VersionGenerator<LTag>? VersionGenerator { get; set; }
            public Func<IState<T>, Result<T>> InitialOutputFactory { get; set; } = DefaultInitialOutputFactory;
            public bool InitialIsConsistent { get; set; }

            public Action<IState<T>>? EventConfigurator { get; set; }
            Action<IState>? IState.IOptions.EventConfigurator { get; set; }
        }

        private volatile StateSnapshot<T>? _snapshot;
        protected VersionGenerator<LTag> VersionGenerator { get; set; }
        protected ComputedOptions ComputedOptions { get; }
        protected AsyncLock AsyncLock { get; }
        protected object Lock => AsyncLock;

        public StateSnapshot<T> Snapshot => _snapshot!;
        public IServiceProvider Services { get; }

        public IComputed<T> Computed {
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

        [MaybeNull]
        public T ValueOrDefault => Computed.ValueOrDefault;
        public T Value => Computed.Value;
        public Exception? Error => Computed.Error;
        public bool HasValue => Computed.HasValue;
        public bool HasError => Computed.HasError;
        public T LatestNonErrorValue => Snapshot.LatestNonErrorComputed.Value;

        IStateSnapshot IState.Snapshot => Snapshot;
        IComputed<T> IState<T>.Computed => Computed;
        IComputed IState.Computed => Computed;
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        object? IState.LatestNonErrorValue => LatestNonErrorValue;
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        object? IResult.Value => Computed.Value;

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
            Services = services;
            ComputedOptions = options.ComputedOptions;
            VersionGenerator = options.VersionGenerator ?? services.VersionGenerator<LTag>();
            options.EventConfigurator?.Invoke(this);
            var untypedOptions = (IState.IOptions) options;
            untypedOptions.EventConfigurator?.Invoke(this);

            Function = this;
            HashCode = RuntimeHelpers.GetHashCode(this);
            AsyncLock = new AsyncLock(ReentryMode.CheckedFail);
            if (initialize) Initialize(options);
        }

        public override string ToString()
            => $"{GetType().Name}(#{HashCode})";

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
            computed.TrySetOutput(options.InitialOutputFactory.Invoke(this));
            Computed = computed;
            if (!options.InitialIsConsistent)
                computed.Invalidate();
        }

        protected internal virtual void OnInvalidated(IComputed<T> computed)
        {
            var snapshot = Snapshot;
            if (computed != snapshot.Computed)
                return;
            Invalidated?.Invoke(this, StateEventKind.Invalidated);
            UntypedInvalidated?.Invoke(this, StateEventKind.Invalidated);
        }

        protected virtual void OnUpdating(IComputed<T> computed)
        {
            var snapshot = Snapshot;
            if (computed != snapshot.Computed)
                return;
            snapshot.OnUpdating();
            Updating?.Invoke(this, StateEventKind.Updating);
            UntypedUpdating?.Invoke(this, StateEventKind.Updating);
        }

        protected virtual void OnSetSnapshot(StateSnapshot<T> snapshot, StateSnapshot<T>? prevSnapshot)
        {
            if (prevSnapshot == null) {
                // First assignment / initialization
                if (snapshot.Computed.Options.IsAsyncComputed)
                    throw Errors.UnsupportedComputedOptions(snapshot.Computed.GetType());
                return;
            }
            prevSnapshot.OnUpdated();
            Updated?.Invoke(this, StateEventKind.Updated);
            UntypedUpdated?.Invoke(this, StateEventKind.Updated);
        }

        // IFunction<T> & IFunction

        Task<IComputed<T>> IFunction<State<T>, T>.Invoke(State<T> input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
            => Invoke(input, usedBy, context, cancellationToken);
        async Task<IComputed> IFunction.Invoke(ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
            => await Invoke((State<T>) input, usedBy, context, cancellationToken).ConfigureAwait(false);

        protected virtual async Task<IComputed<T>> Invoke(
            State<T> input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            if (input != this)
                // This "Function" supports just a single input == this
                throw new ArgumentOutOfRangeException(nameof(input));

            context ??= ComputeContext.Current;

            var result = Computed;
            if (result.TryUseExisting(context, usedBy))
                return result;

            using var _ = await AsyncLock.Lock(cancellationToken);

            result = Computed;
            if (result.TryUseExisting(context, usedBy))
                return result;

            OnUpdating(result);
            result = await GetComputed(cancellationToken).ConfigureAwait(false);
            result.UseNew(context, usedBy);
            return result;
        }

        async Task IFunction.InvokeAndStrip(
            ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
            => await InvokeAndStrip((State<T>) input, usedBy, context, cancellationToken).ConfigureAwait(false);
        Task<T> IFunction<State<T>, T>.InvokeAndStrip(State<T> input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
            => InvokeAndStrip(input, usedBy, context, cancellationToken);

        protected virtual async Task<T> InvokeAndStrip(
            State<T> input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            if (input != this)
                // This "Function" supports just a single input == this
                throw new ArgumentOutOfRangeException(nameof(input));

            context ??= ComputeContext.Current;

            var result = Computed;
            if (result.TryUseExisting(context, usedBy))
                return result.Strip(context);

            using var _ = await AsyncLock.Lock(cancellationToken);

            result = Computed;
            if (result.TryUseExisting(context, usedBy))
                return result.Strip(context);

            OnUpdating(result);
            result = await GetComputed(cancellationToken).ConfigureAwait(false);
            result.UseNew(context, usedBy);
            return result.Value;
        }

        protected async ValueTask<StateBoundComputed<T>> GetComputed(CancellationToken cancellationToken)
        {
            var computed = CreateComputed();
            using var _ = Fusion.Computed.ChangeCurrent(computed);

            try {
                var value = await Compute(cancellationToken).ConfigureAwait(false);
                computed.TrySetOutput(Result.New(value));
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                computed.TrySetOutput(Result.Error<T>(e));
            }

            Computed = computed;
            return computed;
        }

        protected abstract Task<T> Compute(CancellationToken cancellationToken);

        protected virtual StateBoundComputed<T> CreateComputed()
            => new(ComputedOptions, this, VersionGenerator.NextVersion());
    }
}
