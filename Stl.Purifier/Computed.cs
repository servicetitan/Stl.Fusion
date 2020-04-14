using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Stl.Collections;
using Stl.Collections.Slim;
using Stl.Purifier.Internal;

namespace Stl.Purifier
{
    public enum ComputedState
    {
        Computing = 0,
        Computed,
        Invalidated,
    }

    public interface IComputed : IResult, IEquatable<IComputed>
    {
        IFunction Function { get; }
        object Input { get; }
        IResult Output { get; }
        Type OutputType { get; }
        long Tag { get; } // ~ Unique for the specific (Func, Key) pair
        ComputedState State { get; }
        bool IsValid { get; }
        public event Action<IComputed> Invalidated;

        bool Invalidate();
        void AddUsed(IComputed used);
        void RemoveUsed(IComputed used);
        void AddUsedBy(IComputed usedBy); // Should be called only from AddUsedValue
    }

    public interface IComputed<TOut> : IComputed, IResult<TOut>, 
        IEquatable<IComputed<TOut>>
    {
        bool TrySetOutput(Result<TOut> output);
        void SetOutput(Result<TOut> output);
    }
    
    public interface IComputedWithTypedInput<TIn> : IComputed, 
        IEquatable<IComputedWithTypedInput<TIn>>
        where TIn : notnull
    {
        new TIn Input { get; }
        new IFunction<TIn> Function { get; }
    }

    public interface IComputed<TIn, TOut> : IComputed<TOut>, IComputedWithTypedInput<TIn>, 
        IEquatable<IComputed<TIn, TOut>>
        where TIn : notnull
    { }

    public class Computed<TIn, TOut> : IComputed<TIn, TOut>,
        IEquatable<Computed<TIn, TOut>> 
        where TIn : notnull
    {
        private volatile int _state;
        private Result<TOut> _output = default!;
        private RefHashSetSlim2<IComputed> _usedValues;
        private HashSetSlim2<ComputedRef<TIn>> _usedBy;
        private event Action<IComputed>? _invalidated;
        private object Lock => this;
        
        public IFunction<TIn, TOut> Function { get; }
        public bool IsValid => State == ComputedState.Computed;
        public ComputedState State => (ComputedState) _state;
        public TIn Input { get; }
        public long Tag { get; }

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

        public event Action<IComputed> Invalidated {
            add {
                lock (Lock) {
                    if (State != ComputedState.Invalidated)
                        _invalidated += value;
                    else
                        value?.Invoke(this);
                }
            }
            remove {
                lock (Lock) {
                    _invalidated -= value;
                }
            }
        }

        public Computed(IFunction<TIn, TOut> function, TIn input, long tag)
        {
            Function = function;
            Input = input;
            Tag = tag;
        }

        public override string ToString() 
            => $"{GetType().Name}({Function}({Input}), Tag: #{Tag}, State: {State})";

        public void AddUsed(IComputed used)
        {
            lock (Lock) {
                AssertStateIs(ComputedState.Computing);
                used.AddUsedBy(this);
                _usedValues.Add(used);
            }
        }

        public void RemoveUsed(IComputed used)
        {
            lock (Lock) {
                _usedValues.Remove(used);
            }
        }

        void IComputed.AddUsedBy(IComputed usedBy)
        {
            var usedByRef = ((IComputedWithTypedInput<TIn>) usedBy).ToRef();
            lock (Lock) {
                AssertStateIs(ComputedState.Computed);
                _usedBy.Add(usedByRef);
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

        public bool Invalidate()
        {
            if (!TryChangeState(ComputedState.Invalidated))
                return false;
            ListBuffer<IComputed> dependencies = default;
            try {
                lock (Lock) {
                    _usedBy.Apply(this, 
                        (self, dRef) => dRef.TryResolve()?.RemoveUsed(self));
                    _usedBy.Clear();

                    dependencies = ListBuffer<IComputed>.LeaseAndSetCount(_usedValues.Count);
                    _usedValues.CopyTo(dependencies.Span);
                    _usedValues.Clear();
                }
                _invalidated?.Invoke(this);
                for (var i = 0; i < dependencies.Span.Length; i++) {
                    ref var d = ref dependencies.Span[i];
                    d.Invalidate();
                    // Just in case buffers aren't cleaned up when you return them back
                    d = null!; 
                }
                return true;
            }
            finally {
                dependencies.Release();
            }
        }

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
            var oldState = (int) newState - 1;
            return oldState == Interlocked.CompareExchange(ref _state, (int) newState, oldState);
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

        // Equality

        bool IEquatable<IComputed>.Equals(IComputed? other) 
            => Equals(other as Computed<TIn, TOut>);
        bool IEquatable<IComputed<TOut>>.Equals(IComputed<TOut>? other) 
            => Equals(other as Computed<TIn, TOut>);
        bool IEquatable<IComputedWithTypedInput<TIn>>.Equals(IComputedWithTypedInput<TIn>? other) 
            => Equals(other as Computed<TIn, TOut>);
        bool IEquatable<IComputed<TIn, TOut>>.Equals(IComputed<TIn, TOut>? other) 
            => Equals(other as Computed<TIn, TOut>);
        public override bool Equals(object? other) 
            => Equals(other as Computed<TIn, TOut>);
        public bool Equals(Computed<TIn, TOut>? other) => 
            !ReferenceEquals(null, other) 
                && ReferenceEquals(Function, other.Function)
                && EqualityComparer<TIn>.Default.Equals(Input, other.Input);

        public override int GetHashCode() 
            => HashCode.Combine(Function, EqualityComparer<TIn>.Default.GetHashCode(Input));
        
        public static bool operator ==(Computed<TIn, TOut>? left, Computed<TIn, TOut>? right) => Equals(left, right);
        public static bool operator !=(Computed<TIn, TOut>? left, Computed<TIn, TOut>? right) => !Equals(left, right);
    }

    public static class Computed
    {
        private static readonly AsyncLocal<IComputed?> CurrentLocal = new AsyncLocal<IComputed?>();
        
        public static IComputed? Current => CurrentLocal.Value;

        public static IComputed<T> Return<T>(T value)
        {
            var current = Current;
            if (current is IComputed<T> c) {
                c.SetOutput(value);
                return c;
            }
            if (current == null)
                throw Errors.NoCurrentComputed();
            throw Errors.CurrentComputedIsOfIncompatibleType(typeof(T), current.OutputType);
        }

        public static Disposable<IComputed?> ChangeCurrent(IComputed? newCurrent)
        {
            var oldCurrent = Current;
            CurrentLocal.Value = newCurrent;
            return Disposable.New(oldCurrent, oldCurrent1 => CurrentLocal.Value = oldCurrent1);
        }
    }
}
