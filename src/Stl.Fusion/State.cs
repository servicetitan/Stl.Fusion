using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.DependencyInjection;
using Stl.Extensibility;
using Stl.Fusion.Internal;
using Stl.Generators;
using Stl.Locking;
using Stl.Reflection;

namespace Stl.Fusion
{
    public interface IState : IResult, IHasServices
    {
        public interface IOptions
        {
            ComputedOptions ComputedOptions { get; set; }
            Generator<LTag> VersionGenerator { get; set; }
            bool InitialIsConsistent { get; set; }
            Action<IState>? EventConfigurator { get; set; }
        }

        IStateSnapshot Snapshot { get; }
        IComputed Computed { get; }
        IComputed LastValueComputed { get; }
        object? LastValue { get; }
        object? Argument { get; }

        event Action<IState, StateEventKind>? Invalidated;
        event Action<IState, StateEventKind>? Updating;
        event Action<IState, StateEventKind>? Updated;

        bool Invalidate();
    }

    public interface IState<T> : IState, IResult<T>
    {
        new IStateSnapshot<T> Snapshot { get; }
        new IComputed<T> Computed { get; }
        new IComputed<T> LastValueComputed { get; }
        new T LastValue { get; }

        new event Action<IState<T>, StateEventKind>? Invalidated;
        new event Action<IState<T>, StateEventKind>? Updating;
        new event Action<IState<T>, StateEventKind>? Updated;

        Task WhenInvalidatedAsync<TState>(CancellationToken cancellationToken = default);
        ValueTask<T> UseAsync(CancellationToken cancellationToken = default);
    }

    public abstract class State<T> : ComputedInput,
        IState<T>,
        IEquatable<State<T>>,
        IFunction<State<T>, T>
    {
        public class Options : IState.IOptions
        {
            public static readonly Func<IState<T>, Result<T>> DefaultInitialOutputFactory =
                state => Result.Value(ActivatorEx.New<T>(false));

            public ComputedOptions ComputedOptions { get; set; } = ComputedOptions.Default;
            public Generator<LTag> VersionGenerator { get; set; } = ConcurrentLTagGenerator.Default;
            public Func<IState<T>, Result<T>> InitialOutputFactory { get; set; } = DefaultInitialOutputFactory;
            public bool InitialIsConsistent { get; set; }

            public Action<IState<T>>? EventConfigurator { get; set; }
            Action<IState>? IState.IOptions.EventConfigurator { get; set; }
        }

        private volatile StateSnapshot<T>? _snapshot;
        protected Generator<LTag> VersionGenerator { get; set; }
        protected ComputedOptions ComputedOptions { get; }
        protected AsyncLock AsyncLock { get; }
        protected object Lock => AsyncLock;

        public IStateSnapshot<T> Snapshot => _snapshot!;
        public IServiceProvider Services { get; }
        public object? Argument { get; }

        public IComputed<T> Computed {
            get => Snapshot.Computed;
            protected set {
                value.AssertConsistencyStateIsNot(ConsistencyState.Computing);
                lock (Lock) {
                    var oldSnapshot = _snapshot;
                    if (oldSnapshot != null) {
                        oldSnapshot.Computed.Invalidate();
                        _snapshot = new StateSnapshot<T>(value, oldSnapshot);
                    }
                    else
                        _snapshot = new StateSnapshot<T>(value);
                    OnUpdated(oldSnapshot);
                }
            }
        }
        public IComputed<T> LastValueComputed => Snapshot.LastValueComputed;
        public T LastValue => Snapshot.LastValue;

        public T UnsafeValue => Computed.UnsafeValue;
        public T Value => Computed.Value;
        public Exception? Error => Computed.Error;
        public bool HasValue => Computed.HasValue;
        public bool HasError => Computed.HasError;

        IComputed IState.LastValueComputed => LastValueComputed;
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        object? IState.LastValue => LastValue;
        IStateSnapshot IState.Snapshot => Snapshot;
        IComputed<T> IState<T>.Computed => Computed;
        IComputed IState.Computed => Computed;
        object? IResult.UnsafeValue => Computed.UnsafeValue;
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

        protected State(
            Options options, IServiceProvider services,
            object? argument = null, bool initialize = true)
        {
            Services = services;
            Argument = argument;
            ComputedOptions = options.ComputedOptions;
            VersionGenerator = options.VersionGenerator;
            options.EventConfigurator?.Invoke(this);
            var untypedOptions = (IState.IOptions) options;
            untypedOptions.EventConfigurator?.Invoke(this);

            Function = this;
            HashCode = RuntimeHelpers.GetHashCode(this);
            AsyncLock = new AsyncLock(ReentryMode.CheckedFail);
            if (initialize) Initialize(options);
        }

        public virtual ValueTask DisposeAsync() => ValueTaskEx.CompletedTask;

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

        public bool Invalidate()
            => Computed.Invalidate();
        public Task WhenInvalidatedAsync<TState>(CancellationToken cancellationToken = default)
            => Computed.WhenInvalidatedAsync(cancellationToken);
        public ValueTask<T> UseAsync(CancellationToken cancellationToken = default)
            => Computed.UseAsync(cancellationToken);

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
            Invalidated?.Invoke(this, StateEventKind.Invalidated);
            UntypedInvalidated?.Invoke(this, StateEventKind.Invalidated);
        }

        protected virtual void OnUpdating()
        {
            Snapshot.IsUpdating = true;
            Updating?.Invoke(this, StateEventKind.Updating);
            UntypedUpdating?.Invoke(this, StateEventKind.Updating);
        }

        protected virtual void OnUpdated(IStateSnapshot<T>? oldSnapshot)
        {
            if (oldSnapshot == null) {
                // First assignment / initialization
                var computed = Computed;
                if (computed.Options.IsAsyncComputed)
                    throw Errors.UnsupportedComputedOptions(computed.GetType());
            }
            Updated?.Invoke(this, StateEventKind.Updated);
            UntypedUpdated?.Invoke(this, StateEventKind.Updated);
        }

        // IFunction<T> & IFunction

        Task<IComputed<T>> IFunction<State<T>, T>.InvokeAsync(State<T> input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
            => InvokeAsync(input, usedBy, context, cancellationToken);
        async Task<IComputed> IFunction.InvokeAsync(ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
            => await InvokeAsync((State<T>) input, usedBy, context, cancellationToken).ConfigureAwait(false);

        protected virtual async Task<IComputed<T>> InvokeAsync(
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

            using var _ = await AsyncLock.LockAsync(cancellationToken);

            result = Computed;
            if (result.TryUseExisting(context, usedBy))
                return result;

            OnUpdating();
            result = await ComputeAsync(cancellationToken).ConfigureAwait(false);
            result.UseNew(context, usedBy);
            return result;
        }

        async Task IFunction.InvokeAndStripAsync(
            ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
            => await InvokeAndStripAsync((State<T>) input, usedBy, context, cancellationToken).ConfigureAwait(false);
        Task<T> IFunction<State<T>, T>.InvokeAndStripAsync(State<T> input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
            => InvokeAndStripAsync(input, usedBy, context, cancellationToken);

        protected virtual async Task<T> InvokeAndStripAsync(
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

            using var _ = await AsyncLock.LockAsync(cancellationToken);

            result = Computed;
            if (result.TryUseExisting(context, usedBy))
                return result.Strip(context);

            OnUpdating();
            result = await ComputeAsync(cancellationToken).ConfigureAwait(false);
            result.UseNew(context, usedBy);
            return result.Value;
        }

        protected async ValueTask<StateBoundComputed<T>> ComputeAsync(CancellationToken cancellationToken)
        {
            var computed = CreateComputed();
            using var _ = Fusion.Computed.ChangeCurrent(computed);

            try {
                var value = await ComputeValueAsync(cancellationToken).ConfigureAwait(false);
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

        protected abstract Task<T> ComputeValueAsync(CancellationToken cancellationToken);

        protected virtual StateBoundComputed<T> CreateComputed()
            => new(ComputedOptions, this, VersionGenerator.Next());
    }
}
