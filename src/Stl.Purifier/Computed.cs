using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Collections;
using Stl.Collections.Slim;
using Stl.Purifier.Internal;
using Stl.Time;

namespace Stl.Purifier
{
    public enum ComputedState
    {
        Computing = 0,
        Computed,
        Invalidated,
    }

    public interface IComputed : IResult
    {
        IFunction Function { get; }
        object Input { get; }
        IResult Output { get; }
        Type OutputType { get; }
        int Tag { get; } // ~ Unique for the specific (Func, Key) pair
        ComputedState State { get; }
        bool IsValid { get; }
        event Action<IComputed, object?> Invalidated;
        IntMoment LastAccessTime { get; set; }
        int KeepAliveTime { get; set; } // In IntMoment Units

        bool Invalidate(object? invalidatedBy = null);
        ValueTask<IComputed?> RenewAsync(CancellationToken cancellationToken = default);
        ValueTask<IComputed?> RenewAsync(ComputeContext context, CancellationToken cancellationToken = default);

        void Touch();
        TResult Apply<TArg, TResult>(IComputedApplyHandler<TArg, TResult> handler, TArg arg);
    }
    
    public interface IComputed<TOut> : IComputed, IResult<TOut>
    {
        new Result<TOut> Output { get; }
        bool TrySetOutput(Result<TOut> output);
        void SetOutput(Result<TOut> output);

        new ValueTask<IComputed<TOut>?> RenewAsync(CancellationToken cancellationToken = default);
        new ValueTask<IComputed<TOut>?> RenewAsync(ComputeContext context, CancellationToken cancellationToken = default);
    }
    
    public interface IComputedWithTypedInput<TIn> : IComputed 
        where TIn : notnull
    {
        new TIn Input { get; }
        new IFunction<TIn> Function { get; }
    }

    public interface IComputed<TIn, TOut> : IComputed<TOut>, IComputedWithTypedInput<TIn> 
        where TIn : notnull
    { }

    public class Computed<TIn, TOut> : IComputed<TIn, TOut>, IComputedImpl
        where TIn : notnull
    {
        private volatile int _state;
        private Result<TOut> _output = default!;
        private RefHashSetSlim2<IComputedImpl> _used;
        private HashSetSlim2<ComputedRef<TIn>> _usedBy;
        private event Action<IComputed, object?>? _invalidated;
        private object? _invalidatedBy;
        private volatile int _lastAccessTime;
        private int _keepAliveTime;
        private object Lock => this;

        public IFunction<TIn, TOut> Function { get; }
        public bool IsValid => State == ComputedState.Computed;
        public ComputedState State => (ComputedState) _state;
        public TIn Input { get; }
        public int Tag { get; }
        public IntMoment LastAccessTime {
            get => new IntMoment(_lastAccessTime);
            set => Interlocked.Exchange(ref _lastAccessTime, value.EpochOffsetUnits);
        }

        public int KeepAliveTime {
            get => _keepAliveTime;
            set {
                AssertStateIs(ComputedState.Computing);
                _keepAliveTime = value;
            }
        }

        public Type OutputType => typeof(TOut);
        public Result<TOut> Output {
            get {
                AssertStateIsNot(ComputedState.Computing);
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
        IFunction IComputed.Function => Function;
        IFunction<TIn> IComputedWithTypedInput<TIn>.Function => Function;
        // ReSharper disable once HeapView.BoxingAllocation
        object IComputed.Input => Input;
        // ReSharper disable once HeapView.BoxingAllocation
        IResult IComputed.Output => Output;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.UnsafeValue => Output.UnsafeValue;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.Value => Output.Value;        

        public event Action<IComputed, object?> Invalidated {
            add {
                lock (Lock) {
                    if (State != ComputedState.Invalidated)
                        _invalidated += value;
                    else
                        value?.Invoke(this, _invalidatedBy);
                }
            }
            remove {
                lock (Lock) {
                    _invalidated -= value;
                }
            }
        }

        public Computed(IFunction<TIn, TOut> function, TIn input, int tag)
        {
            Function = function;
            Input = input;
            Tag = tag;
            _keepAliveTime = Computed.DefaultKeepAliveTime;
            _lastAccessTime = IntMoment.Clock.EpochOffsetUnits;
        }

        public override string ToString() 
            => $"{GetType().Name}({Function}({Input}), Tag: #{Tag}, State: {State})";

        void IComputedImpl.AddUsed(IComputedImpl used)
        {
            lock (Lock) {
                switch (State) {
                case ComputedState.Computing:
                    break; // Expected state
                case ComputedState.Computed:
                    throw Errors.WrongComputedState(State);
                case ComputedState.Invalidated:
                    return; // Already invalidated, so nothing to do here
                default:
                    throw new ArgumentOutOfRangeException();
                }
                used.AddUsedBy(this);
                _used.Add(used);
            }
        }

        void IComputedImpl.AddUsedBy(IComputedImpl usedBy)
        {
            var usedByRef = ((IComputedWithTypedInput<TIn>) usedBy).ToRef();
            lock (Lock) {
                switch (State) {
                case ComputedState.Computing:
                    throw Errors.WrongComputedState(State);
                case ComputedState.Computed:
                    break; // Expected state
                case ComputedState.Invalidated:
                    usedBy.Invalidate(_invalidatedBy);
                    return; 
                default:
                    throw new ArgumentOutOfRangeException();
                }
                _usedBy.Add(usedByRef);
            }
        }

        void IComputedImpl.RemoveUsedBy(IComputedImpl usedBy)
        {
            var usedByRef = ((IComputedWithTypedInput<TIn>) usedBy).ToRef();
            lock (Lock) {
                _usedBy.Remove(usedByRef);
            }
        }

        public bool TrySetOutput(Result<TOut> output)
        {
            if (!TryChangeState(ComputedState.Computed))
                return false;
            lock (Lock)
                _output = output;
            return true;
        }

        public void SetOutput(Result<TOut> output)
        {
            if (!TrySetOutput(output))
                throw Errors.WrongComputedState(ComputedState.Computing, State);
        }

        public bool Invalidate(object? invalidatedBy = null)
        {
            if (!TryChangeState(ComputedState.Invalidated))
                return false;
            ListBuffer<ComputedRef<TIn>> usedBy = default;
            try {
                lock (Lock) {
                    _invalidatedBy = invalidatedBy;
                    usedBy = ListBuffer<ComputedRef<TIn>>.LeaseAndSetCount(_usedBy.Count);
                    _usedBy.CopyTo(usedBy.Span);
                    _usedBy.Clear();
                    _used.Apply(this, (self, c) => c.RemoveUsedBy(self));
                    _used.Clear();
                }
                _invalidated?.Invoke(this, invalidatedBy);
                for (var i = 0; i < usedBy.Span.Length; i++) {
                    ref var d = ref usedBy.Span[i];
                    d.TryResolve()?.Invalidate(invalidatedBy);
                    // Just in case buffers aren't cleaned up when you return them back
                    d = default!; 
                }
                return true;
            }
            finally {
                usedBy.Release();
            }
        }

        async ValueTask<IComputed?> IComputed.RenewAsync(CancellationToken cancellationToken) 
            => await RenewAsync(null!, cancellationToken).ConfigureAwait(false);
        async ValueTask<IComputed?> IComputed.RenewAsync(ComputeContext context, CancellationToken cancellationToken) 
            => await RenewAsync(cancellationToken).ConfigureAwait(false);
        public ValueTask<IComputed<TOut>?> RenewAsync(CancellationToken cancellationToken)
            => RenewAsync(null!, cancellationToken);
        public async ValueTask<IComputed<TOut>?> RenewAsync(ComputeContext context, CancellationToken cancellationToken)
            => IsValid ? this : await Function.InvokeAsync(Input, null, context, cancellationToken);

        // Touch

        public void Touch() 
            => Interlocked.Exchange(ref _lastAccessTime, IntMoment.Clock.EpochOffsetUnits);

        // Apply methods

        public TResult Apply<TArg, TResult>(IComputedApplyHandler<TArg, TResult> handler, TArg arg) 
            => handler.Apply<TIn, TOut>(this, arg);

        // IResult<T> methods

        public void Deconstruct(out TOut value, out Exception? error) 
            => Output.Deconstruct(out value, out error);
        public bool IsValue([MaybeNullWhen(false)] out TOut value)
            => Output.IsValue(out value);
        public bool IsValue([MaybeNullWhen(false)] out TOut value, [MaybeNullWhen(true)] out Exception error) 
            => Output.IsValue(out value, out error!);
        public void ThrowIfError() => Output.ThrowIfError();

        // Protected & private methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryChangeState(ComputedState newState)
        {
            var newStateInt = (int) newState;
            while (true) {
                var oldState = _state;
                if (oldState >= newStateInt)
                    return false;
                if (oldState == Interlocked.CompareExchange(ref _state, newStateInt, oldState))
                    return true;
                // No SpinWait / something similar is needed here, since
                // each iteration in this loop means the state was updated
                // by another thread, and since the new value can only be
                // higher, the max. # of iterations here is 2.
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AssertStateIs(ComputedState expectedState)
        {
            if (State != expectedState)
                throw Errors.WrongComputedState(expectedState, State);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AssertStateIsNot(ComputedState unexpectedState)
        {
            if (State == unexpectedState)
                throw Errors.WrongComputedState(State);
        }
    }

    public static class Computed
    {
        public static readonly int DefaultKeepAliveTime = IntMoment.SecondsToUnits(1);
        private static readonly AsyncLocal<IComputed?> CurrentLocal = new AsyncLocal<IComputed?>();

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

        public static Disposable<IComputed?> ChangeCurrent(IComputed? newCurrent)
        {
            var oldCurrent = GetCurrent();
            if (oldCurrent == newCurrent)
                return Disposable.New(oldCurrent, _ => { });
            CurrentLocal.Value = newCurrent;
            return Disposable.New(oldCurrent, oldCurrent1 => CurrentLocal.Value = oldCurrent1);
        }

        // Capture & invalidate

        public static async Task<IComputed<T>?> CaptureAsync<T>(Func<Task<T>> producer)
        {
            using var ccs = ComputeContext.New(ComputeOptions.Capture).Activate();
            await producer.Invoke().ConfigureAwait(false);
            var result = ccs.Context.GetCapturedComputed<T>();
            return result;
        }

        public static IComputed<T>? Invalidate<T>(Func<Task<T>> producer, object? invalidatedBy = null)
        {
            using var ccs = ComputeContext.New(ComputeOptions.Invalidate, invalidatedBy).Activate();
            var task = producer.Invoke();
            // The flow is essentially synchronous in this case, so...
            task.AssertCompleted();
            return ccs.Context.GetCapturedComputed<T>();
        }

        public static IComputed<T>? TryGetCached<T>(Func<Task<T>> producer)
        {
            using var ccs = ComputeContext.New(ComputeOptions.TryGetCached).Activate();
            var task = producer.Invoke();
            // The flow is essentially synchronous in this case, so...
            task.AssertCompleted();
            return ccs.Context.GetCapturedComputed<T>();
        }
    }
}
