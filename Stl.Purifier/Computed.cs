using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
        Invalidating,
        Invalidated,
    }

    public interface IComputed : IEquatable<IComputed>
    {
        IFunction Function { get; }
        object Key { get; }
        object? Value { get; }
        long Tag { get; } // ~ Unique for the specific (Func, Key) pair
        ComputedState State { get; }
        bool IsValid { get; }
        public event Action<IComputed> Invalidated;

        bool Invalidate();
        void AddUsedValue(IComputed usedValue);
        void RemoveUsedValue(IComputed usedValue);
        void AddUsedBy(IComputed usedBy); // Should be called only from AddUsedValue
    }

    public interface IComputed<TValue> : IComputed, IEquatable<IComputed<TValue>>
    {
        new TValue Value { get; }
        void SetValue(TValue value);
    }
    
    public interface IKeyedComputed<TKey> : IComputed, IEquatable<IKeyedComputed<TKey>>
        where TKey : notnull
    {
        new TKey Key { get; }
        new IFunction<TKey> Function { get; }
    }

    public interface IComputed<TKey, TValue> : IComputed<TValue>, IKeyedComputed<TKey>, IEquatable<IComputed<TKey, TValue>>
        where TKey : notnull
    { }

    public class Computed<TKey, TValue> : IComputed<TKey, TValue>,
        IEquatable<Computed<TKey, TValue>> 
        where TKey : notnull
    {
        private volatile int _state;
        private TValue _value = default!;
        private RefHashSetSlim2<IComputed> _usedValues;
        private HashSetSlim2<ComputedRef<TKey>> _usedBy;
        private event Action<IComputed>? _invalidated;
        private object Lock => this;

        #region "Untyped" versions of properties

        IFunction IComputed.Function => Function;
        IFunction<TKey> IKeyedComputed<TKey>.Function => Function;
        // ReSharper disable once HeapView.BoxingAllocation
        object IComputed.Key => Key;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IComputed.Value => Value;

        #endregion
        
        public IFunction<TKey, TValue> Function { get; }
        public bool IsValid => State == ComputedState.Computed;
        public ComputedState State => (ComputedState) _state;
        public TKey Key { get; }
        public long Tag { get; }
        public TValue Value {
            get {
                AssertStateIsNot(ComputedState.Computing);
                return _value;
            }
        }

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

        public Computed(IFunction<TKey, TValue> function, TKey key, long tag)
        {
            Function = function;
            Key = key;
            Tag = tag;
        }

        public override string ToString() 
            => $"{GetType().Name}({Function}({Key}), Tag: #{Tag}, State: {State})";

        public void AddUsedValue(IComputed usedValue)
        {
            lock (Lock) {
                AssertStateIs(ComputedState.Computing);
                usedValue.AddUsedBy(this);
                _usedValues.Add(usedValue);
            }
        }

        public void RemoveUsedValue(IComputed usedValue)
        {
            lock (Lock) {
                _usedValues.Remove(usedValue);
            }
        }

        public void AddUsedBy(IComputed usedBy)
        {
            var usedByRef = ((IKeyedComputed<TKey>) usedBy).ToRef();
            lock (Lock) {
                AssertStateIs(ComputedState.Computed);
                _usedBy.Add(usedByRef);
            }
        }

        public void SetValue(TValue value)
        {
            lock (Lock) {
                if (!TryChangeState(ComputedState.Computed))
                    throw Errors.WrongComputedState(ComputedState.Computing, State);
                _value = value;
            }
        }

        public bool Invalidate()
        {
            if (!TryChangeState(ComputedState.Invalidating))
                return false;
            ListBuffer<IComputed> dependencies = default;
            try {
                lock (Lock) {
                    _usedBy.Apply(this, 
                        (self, dRef) => dRef.TryResolve()?.RemoveUsedValue(self));
                    _usedBy.Clear();

                    Interlocked.Exchange(ref _state, (int) ComputedState.Invalidated);

                    dependencies = ListBuffer<IComputed>.LeaseAndSetCount(_usedValues.Count);
                    _usedValues.CopyTo(dependencies.Span);
                    _usedValues.Clear();
                }
                _invalidated?.Invoke(this);
                for (var i = 0; i < dependencies.Span.Length; i++) {
                    ref var d = ref dependencies.Span[i];
                    d.Invalidate();
                    d = null!; // Just in case buffers aren't cleaned up when you return them back
                }
                return true;
            }
            finally {
                dependencies.Release();
            }
        }

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
            => Equals(other as Computed<TKey, TValue>);
        bool IEquatable<IComputed<TValue>>.Equals(IComputed<TValue>? other) 
            => Equals(other as Computed<TKey, TValue>);
        bool IEquatable<IKeyedComputed<TKey>>.Equals(IKeyedComputed<TKey>? other) 
            => Equals(other as Computed<TKey, TValue>);
        bool IEquatable<IComputed<TKey, TValue>>.Equals(IComputed<TKey, TValue>? other) 
            => Equals(other as Computed<TKey, TValue>);
        public override bool Equals(object? other) 
            => Equals(other as Computed<TKey, TValue>);
        public bool Equals(Computed<TKey, TValue>? other) => 
            !ReferenceEquals(null, other) 
                && ReferenceEquals(Function, other.Function)
                && EqualityComparer<TKey>.Default.Equals(Key, other.Key);

        public override int GetHashCode() 
            => HashCode.Combine(Function, EqualityComparer<TKey>.Default.GetHashCode(Key));
        
        public static bool operator ==(Computed<TKey, TValue>? left, Computed<TKey, TValue>? right) => Equals(left, right);
        public static bool operator !=(Computed<TKey, TValue>? left, Computed<TKey, TValue>? right) => !Equals(left, right);
    }
}
