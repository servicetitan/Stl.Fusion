using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Collections.Slim;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;

namespace Stl.Fusion
{
    public interface IComputed : IHasConsistencyState, IResult
    {
        ComputedOptions Options { get; }
        ComputedInput Input { get; }
        Type OutputType { get; }
        IResult Output { get; }
        LTag Version { get; } // ~ Unique for the specific (Func, Key) pair
        event Action<IComputed> Invalidated;

        bool Invalidate();
        TResult Apply<TArg, TResult>(IComputedApplyHandler<TArg, TResult> handler, TArg arg);

        ValueTask<IComputed> Update(bool addDependency, CancellationToken cancellationToken = default);
        ValueTask<object> Use(CancellationToken cancellationToken = default);
    }

    public interface IComputed<TOut> : IComputed, IResult<TOut>
    {
        new Result<TOut> Output { get; }
        bool TrySetOutput(Result<TOut> output);

        new ValueTask<IComputed<TOut>> Update(bool addDependency, CancellationToken cancellationToken = default);
        new ValueTask<TOut> Use(CancellationToken cancellationToken = default);
    }

    public interface IAsyncComputed : IComputed
    {
        IResult? MaybeOutput { get; }
        ValueTask<IResult?> GetOutputAsync(CancellationToken cancellationToken = default);
    }

    public interface IAsyncComputed<T> : IAsyncComputed, IComputed<T>
    {
        new ResultBox<T>? MaybeOutput { get; }
        new ValueTask<ResultBox<T>?> GetOutputAsync(CancellationToken cancellationToken = default);
    }

    public interface IComputedWithTypedInput<out TIn> : IComputed
        where TIn : ComputedInput
    {
        new TIn Input { get; }
    }

    public interface IComputed<out TIn, TOut> : IComputed<TOut>, IComputedWithTypedInput<TIn>
        where TIn : ComputedInput
    { }

    public class Computed<TIn, TOut> : IComputed<TIn, TOut>, IComputedImpl
        where TIn : ComputedInput
    {
        private readonly ComputedOptions _options;
        private volatile int _state;
        private Result<TOut> _output;
        private RefHashSetSlim3<IComputedImpl> _used;
        private HashSetSlim3<(ComputedInput Input, LTag Version)> _usedBy;
        // ReSharper disable once InconsistentNaming
        private event Action<IComputed>? _invalidated;
        private bool _invalidateOnSetOutput;

        protected bool InvalidateOnSetOutput => _invalidateOnSetOutput;
        protected object Lock => this;

        public ComputedOptions Options => _options;
        public TIn Input { get; }
        public ConsistencyState ConsistencyState => (ConsistencyState) _state;
        public bool IsConsistent() => ConsistencyState == ConsistencyState.Consistent;
        public IFunction<TIn, TOut> Function => (IFunction<TIn, TOut>) Input.Function;
        public LTag Version { get; }
        public Type OutputType => typeof(TOut);

        public virtual Result<TOut> Output {
            get {
                this.AssertConsistencyStateIsNot(ConsistencyState.Computing);
                return _output;
            }
        }

        // IResult<T> properties
        public Exception? Error => Output.Error;
        public bool HasValue => Output.HasValue;
        public bool HasError => Output.HasError;
        public TOut UnsafeValue => Output.UnsafeValue;
        public TOut Value => Output.Value;

        // "Untyped" versions of properties
        ComputedInput IComputed.Input => Input;
        // ReSharper disable once HeapView.BoxingAllocation
        IResult IComputed.Output => Output;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.UnsafeValue => Output.UnsafeValue;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.Value => Output.Value;

        public event Action<IComputed> Invalidated {
            add {
                if (ConsistencyState == ConsistencyState.Invalidated) {
                    value?.Invoke(this);
                    return;
                }
                lock (Lock) {
                    if (ConsistencyState == ConsistencyState.Invalidated) {
                        value.Invoke(this);
                        return;
                    }
                    _invalidated += value;
                }
            }
            remove {
                lock (Lock) {
                    if (ConsistencyState == ConsistencyState.Invalidated)
                        return;
                    _invalidated -= value;
                }
            }
        }

        protected Computed(ComputedOptions options, TIn input, LTag version)
        {
            _options = options;
            Input = input;
            Version = version;
            ComputedRegistry.Instance.Register(this);
        }

        protected Computed(ComputedOptions options, TIn input, Result<TOut> output, LTag version, bool isConsistent)
        {
            _options = options;
            Input = input;
            _state = (int) (isConsistent ? ConsistencyState.Consistent : ConsistencyState.Invalidated);
            _output = output;
            Version = version;
            if (isConsistent)
                ComputedRegistry.Instance.Register(this);
        }

        public override string ToString()
            => $"{GetType().Name}({Input} {Version}, State: {ConsistencyState})";

        public virtual bool TrySetOutput(Result<TOut> output)
        {
            if (Options.RewriteErrors && !output.IsValue(out _, out var error)) {
                var errorRewriter = Function.Services.GetRequiredService<IErrorRewriter>();
                output = Result.Error<TOut>(errorRewriter.Rewrite(this, error));
            }
            if (ConsistencyState != ConsistencyState.Computing)
                return false;
            lock (Lock) {
                if (ConsistencyState != ConsistencyState.Computing)
                    return false;
                SetStateUnsafe(ConsistencyState.Consistent);
                _output = output;
            }
            OnOutputSet(output);
            return true;
        }

        protected void OnOutputSet(Result<TOut> output)
        {
            if (InvalidateOnSetOutput) {
                Invalidate();
                return;
            }
            var timeout = output.HasError
                ? _options.ErrorAutoInvalidateTime
                : _options.AutoInvalidateTime;
            if (timeout != TimeSpan.MaxValue)
                this.Invalidate(timeout);
        }

        public bool Invalidate()
        {
            if (ConsistencyState == ConsistencyState.Invalidated)
                return false;
            // Debug.WriteLine($"{nameof(Invalidate)}: {this}");
            lock (Lock) {
                switch (ConsistencyState) {
                case ConsistencyState.Invalidated:
                    return false;
                case ConsistencyState.Computing:
                    _invalidateOnSetOutput = true;
                    return true;
                }
                SetStateUnsafe(ConsistencyState.Invalidated);
            }
            try {
                _used.Apply(this, (self, c) => c.RemoveUsedBy(self));
                _used.Clear();
                _invalidated?.Invoke(this);
                _usedBy.Apply(default(Unit), (_, usedByEntry) => {
                    var c = ComputedRegistry.Instance.TryGet(usedByEntry.Input);
                    if (c != null && c.Version == usedByEntry.Version)
                        c.Invalidate();
                });
                _usedBy.Clear();
                OnInvalidated();
            }
            catch (Exception e) {
                // We should never throw errors during the invalidation
                try {
                    var log = Input.Function.Services.GetService<ILogger<Computed<TIn, TOut>>>();
                    log?.LogError(e, "Error on invalidation");
                }
                catch {
                    // Intended
                }
            }
            _invalidated = null;
            return true;
        }

        protected virtual void OnInvalidated()
        {
            ComputedRegistry.Instance.Unregister(this);
            CancelTimeouts();
        }

        // UpdateAsync

        async ValueTask<IComputed> IComputed.Update(bool addDependency, CancellationToken cancellationToken)
            => await Update(addDependency, cancellationToken).ConfigureAwait(false);
        public async ValueTask<IComputed<TOut>> Update(bool addDependency, CancellationToken cancellationToken = default)
        {
            var usedBy = addDependency ? Computed.GetCurrent() : null;
            var context = ComputeContext.Current;

            if (this.TryUseExisting(context, usedBy))
                return this;
            return await Function
                .Invoke(Input, usedBy, context, cancellationToken)
                .ConfigureAwait(false);
        }

        // UseAsync

        async ValueTask<object> IComputed.Use(CancellationToken cancellationToken)
            => (await Use(cancellationToken).ConfigureAwait(false))!;
        public async ValueTask<TOut> Use(CancellationToken cancellationToken = default)
        {
            var computed = await Update(true, cancellationToken).ConfigureAwait(false);
            return computed.Value;
        }

        // Apply

        public TResult Apply<TArg, TResult>(IComputedApplyHandler<TArg, TResult> handler, TArg arg)
            => handler.Apply(this, arg);

        // IResult<T> methods

        public void Deconstruct(out TOut value, out Exception? error)
            => Output.Deconstruct(out value, out error);
        public bool IsValue([MaybeNullWhen(false)] out TOut value)
            => Output.IsValue(out value);
        public bool IsValue([MaybeNullWhen(false)] out TOut value, [MaybeNullWhen(true)] out Exception error)
            => Output.IsValue(out value, out error!);
        public Result<TOut> AsResult()
            => Output.AsResult();
        public Result<TOther> Cast<TOther>()
            => Output.Cast<TOther>();
        TOut IConvertibleTo<TOut>.Convert() => Value;
        Result<TOut> IConvertibleTo<Result<TOut>>.Convert() => AsResult();

        // IComputedImpl methods

        void IComputedImpl.AddUsed(IComputedImpl used)
        {
            // Debug.WriteLine($"{nameof(IComputedImpl.AddUsed)}: {this} <- {used}");
            lock (Lock) {
                switch (ConsistencyState) {
                case ConsistencyState.Consistent:
                    throw Errors.WrongComputedState(ConsistencyState);
                case ConsistencyState.Invalidated:
                    return; // Already invalidated, so nothing to do here
                }
                used.AddUsedBy(this);
                _used.Add(used);
            }
        }

        void IComputedImpl.AddUsedBy(IComputedImpl usedBy)
        {
            lock (Lock) {
                switch (ConsistencyState) {
                case ConsistencyState.Computing:
                    throw Errors.WrongComputedState(ConsistencyState);
                case ConsistencyState.Invalidated:
                    usedBy.Invalidate();
                    return;
                }

                var usedByRef = (usedBy.Input, usedBy.Version);
                _usedBy.Add(usedByRef);
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

        public virtual void RenewTimeouts()
        {
            if (ConsistencyState == ConsistencyState.Invalidated)
                return;
            var options = Options;
            if (options.KeepAliveTime > TimeSpan.Zero)
                Timeouts.KeepAlive.AddOrUpdateToLater(this, Timeouts.Clock.Now + options.KeepAliveTime);
        }

        public virtual void CancelTimeouts()
        {
            var options = Options;
            if (options.KeepAliveTime > TimeSpan.Zero)
                Timeouts.KeepAlive.Remove(this);
        }

        // Protected & private methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetStateUnsafe(ConsistencyState newState)
            => _state = (int) newState;
    }

    public class Computed<T> : Computed<ComputeMethodInput, T>
    {
        public Computed(ComputedOptions options, ComputeMethodInput input, LTag version)
            : base(options, input, version) { }

        protected Computed(ComputedOptions options, ComputeMethodInput input, Result<T> output, LTag version, bool isConsistent = true)
            : base(options, input, output, version, isConsistent) { }
    }
}
