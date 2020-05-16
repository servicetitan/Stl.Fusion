using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Collections;
using Stl.Collections.Slim;
using Stl.Fusion.Bridge;
using Stl.Fusion.Internal;
using Stl.Time;

namespace Stl.Fusion
{
    public enum ComputedState
    {
        Computing = 0,
        Consistent,
        Invalidated,
    }

    public interface IComputed : IResult
    {
        ComputedInput Input { get; }
        IResult Output { get; }
        Type OutputType { get; }
        int Tag { get; } // ~ Unique for the specific (Func, Key) pair
        ComputedState State { get; }
        bool IsConsistent { get; }
        event Action<IComputed, object?> Invalidated;
        IntMoment LastAccessTime { get; set; }
        int KeepAliveTime { get; set; } // In IntMoment Units

        bool Invalidate(object? invalidatedBy = null);
        ValueTask<IComputed?> UpdateAsync(CancellationToken cancellationToken = default);
        ValueTask<IComputed?> UpdateAsync(ComputeContext context, CancellationToken cancellationToken = default);

        void Touch();
        TResult Apply<TArg, TResult>(IComputedApplyHandler<TArg, TResult> handler, TArg arg);
    }
    
    public interface IComputed<TOut> : IComputed, IResult<TOut>
    {
        new Result<TOut> Output { get; }
        TaggedResult<TOut> TaggedOutput { get; }
        bool TrySetOutput(Result<TOut> output);
        void SetOutput(Result<TOut> output);

        new ValueTask<IComputed<TOut>?> UpdateAsync(CancellationToken cancellationToken = default);
        new ValueTask<IComputed<TOut>?> UpdateAsync(ComputeContext context, CancellationToken cancellationToken = default);
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
        private volatile int _state;
        private Result<TOut> _output = default!;
        private RefHashSetSlim2<IComputedImpl> _used;
        private HashSetSlim2<(ComputedInput Input, int Tag)> _usedBy;
        private event Action<IComputed, object?>? _invalidated;
        private object? _invalidatedBy;
        private volatile int _lastAccessTime;
        private int _keepAliveTime;
        private object Lock => this;

        public bool IsConsistent => State == ComputedState.Consistent;
        public ComputedState State => (ComputedState) _state;
        public TIn Input { get; }
        public IFunction<TIn, TOut> Function => (IFunction<TIn, TOut>) Input.Function; 
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
        public TaggedResult<TOut> TaggedOutput => new TaggedResult<TOut>(Output, Tag);

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

        public Computed(TIn input, int tag)
        {
            Input = input;
            Tag = tag;
            _keepAliveTime = Computed.DefaultKeepAliveTime;
            _lastAccessTime = IntMoment.Clock.EpochOffsetUnits;
        }

        public Computed(TIn input, Result<TOut> output, int tag)
        {
            Input = input;
            _state = (int) ComputedState.Consistent;
            _output = output;
            Tag = tag;
            _keepAliveTime = Computed.DefaultKeepAliveTime;
            _lastAccessTime = IntMoment.Clock.EpochOffsetUnits;
        }

        public override string ToString() 
            => $"{GetType().Name}({Input}, Tag: #{Tag}, State: {State})";

        void IComputedImpl.AddUsed(IComputedImpl used)
        {
            lock (Lock) {
                switch (State) {
                case ComputedState.Computing:
                    break; // Expected state
                case ComputedState.Consistent:
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
            lock (Lock) {
                switch (State) {
                case ComputedState.Computing:
                    throw Errors.WrongComputedState(State);
                case ComputedState.Consistent:
                    break; // Expected state
                case ComputedState.Invalidated:
                    usedBy.Invalidate(_invalidatedBy);
                    return; 
                default:
                    throw new ArgumentOutOfRangeException();
                }
                _usedBy.Add((usedBy.Input, usedBy.Tag));
            }
        }

        void IComputedImpl.RemoveUsedBy(IComputedImpl usedBy)
        {
            lock (Lock) {
                _usedBy.Remove((usedBy.Input, usedBy.Tag));
            }
        }

        public bool TrySetOutput(Result<TOut> output)
        {
            if (!TryChangeState(ComputedState.Consistent))
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
            ListBuffer<(ComputedInput Input, int Tag)> usedBy = default;
            try {
                lock (Lock) {
                    _invalidatedBy = invalidatedBy;
                    usedBy = ListBuffer<(ComputedInput, int)>.LeaseAndSetCount(_usedBy.Count);
                    _usedBy.CopyTo(usedBy.Span);
                    _usedBy.Clear();
                    _used.Apply(this, (self, c) => c.RemoveUsedBy(self));
                    _used.Clear();
                }
                try {
                    _invalidated?.Invoke(this, invalidatedBy);
                }
                catch {
                    // We should never throw errors during the invalidation
                }
                for (var i = 0; i < usedBy.Span.Length; i++) {
                    ref var d = ref usedBy.Span[i];
                    d.Input.TryGetCachedComputed(d.Tag)?.Invalidate(invalidatedBy);
                    // Just in case buffers aren't cleaned up when you return them back
                    d = default!; 
                }
                return true;
            }
            finally {
                usedBy.Release();
            }
        }

        async ValueTask<IComputed?> IComputed.UpdateAsync(CancellationToken cancellationToken) 
            => await UpdateAsync(null!, cancellationToken).ConfigureAwait(false);
        async ValueTask<IComputed?> IComputed.UpdateAsync(ComputeContext context, CancellationToken cancellationToken) 
            => await UpdateAsync(cancellationToken).ConfigureAwait(false);
        public ValueTask<IComputed<TOut>?> UpdateAsync(CancellationToken cancellationToken)
            => UpdateAsync(null!, cancellationToken);
        public async ValueTask<IComputed<TOut>?> UpdateAsync(ComputeContext context, CancellationToken cancellationToken)
            => IsConsistent ? this : await Function.InvokeAsync(Input, null, context, cancellationToken);

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
}
