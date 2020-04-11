using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading;
using Stl.Collections.Slim;
using Stl.Purifier.Internal;

namespace Stl.Purifier
{
    public enum ComputationState
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
        bool IsValid { get; }
        public event Action<IComputation>? Invalidated;

        bool Invalidate();
    }

    public interface IComputed<TValue> : IComputed, IEquatable<IComputed<TValue>>
    {
        new TValue Value { get; }
    }                           

    public interface IComputed<TKey, TValue> : IComputed<TValue>, IEquatable<IComputed<TKey, TValue>>
        where TKey : notnull
    {
        new IFunction<TKey, TValue> Function { get; }
        new TKey Key { get; }
    }

    public interface IComputation : IComputed, IEquatable<IComputation>
    {
        ComputationState State { get; }
        public void AddDependency(IComputation dependency);
        public void RemoveDependency(IComputation dependency);
        public void AddDependant(IComputation dependant);
    }

    public interface IComputation<TKey, TValue> : IComputation, IComputed<TKey, TValue>, IEquatable<IComputation<TKey, TValue>>
        where TKey : notnull
    {
        void Computed(TValue value);
    }

    public class Computation<TKey, TValue> : IComputation<TKey, TValue>,
        IEquatable<Computation<TKey, TValue>> 
        where TKey : notnull
    {
        private volatile int _state;
        private TValue _value = default!;
        // TODO: Replace ConcurrentDictionary w/ more efficient structure
        protected ConcurrentDictionary<IComputation, Unit>? Dependencies { get; set; }
        protected ConcurrentDictionary<IComputation, Unit>? Dependants { get; set; }

        #region "Untyped" versions of properties

        IFunction IComputed.Function => Function;
        // ReSharper disable once HeapView.BoxingAllocation
        object IComputed.Key => Key;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IComputed.Value => Value;

        #endregion
        
        public IFunction<TKey, TValue> Function { get; }
        public bool IsValid => State == ComputationState.Computed;
        public ComputationState State => (ComputationState) _state;
        public TKey Key { get; }
        public long Tag { get; }
        public TValue Value {
            get {
                AssertStateIs(ComputationState.Computed);
                return _value;
            }
        }

        public event Action<IComputation>? Invalidated;

        public Computation(IFunction<TKey, TValue> function, TKey key, long tag)
        {
            Function = function;
            Key = key;
            Tag = tag;
            Dependencies = new ConcurrentDictionary<IComputation, Unit>(
                ReferenceEqualityComparer<IComputation>.Default);
            // Dependants = null for now -- it's set in Computed 
        }

        public override string ToString() 
            => $"{GetType().Name}({Function}({Key}), Tag: #{Tag}, State: {State})";

        public void AddDependency(IComputation dependency) 
            => Dependencies?.AddOrUpdate(dependency, d => default, (d, _) => default);

        public void RemoveDependency(IComputation dependency) 
            => Dependencies?.Remove(dependency, out _);

        public void AddDependant(IComputation dependant) 
            => Dependants?.AddOrUpdate(dependant, d => default, (d, _) => default);

        public void Computed(TValue value)
        {
            if (!TryChangeState(ComputationState.Computed))
                throw Errors.WrongComputationState(ComputationState.Computing, State);
            _value = value;
            Dependants = new ConcurrentDictionary<IComputation, Unit>(
                ReferenceEqualityComparer<IComputation>.Default);
            OnComputed();
        }

        public bool Invalidate()
        {
            if (!TryChangeState(ComputationState.Invalidating))
                return false;

            var dependants = Dependants;
            Dependants = null;
            var dependencies = Dependencies;
            Dependencies = null;
            foreach (var d in dependants?.Keys ?? Enumerable.Empty<IComputation>())
                d.RemoveDependency(this);

            Interlocked.Exchange(ref _state, (int) ComputationState.Invalidated);

            OnInvalidated(dependencies?.Keys ?? Enumerable.Empty<IComputation>());
            Invalidated?.Invoke(this);
            return true;
        }

        // Protected & private methods

        protected virtual void OnComputed() { }

        protected virtual void OnInvalidated(IEnumerable<IComputation> dependencies)
        {
            foreach (var d in dependencies)
                d.Invalidate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryChangeState(ComputationState newState)
        {
            var oldState = (int) newState - 1;
            return oldState == Interlocked.CompareExchange(ref _state, (int) newState, oldState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AssertStateIs(ComputationState expectedState)
        {
            if (State != expectedState)
                throw Errors.WrongComputationState(expectedState, State);
        }

        // Equality

        public override bool Equals(object? other) 
            => Equals(other as Computation<TKey, TValue>);
        public bool Equals(IComputed? other) 
            => Equals(other as Computation<TKey, TValue>);
        public bool Equals(IComputed<TValue>? other) 
            => Equals(other as Computation<TKey, TValue>);
        public bool Equals(IComputed<TKey, TValue>? other) 
            => Equals(other as Computation<TKey, TValue>);
        public bool Equals(IComputation? other) 
            => Equals(other as Computation<TKey, TValue>);
        public bool Equals(IComputation<TKey, TValue>? other) 
            => Equals(other as Computation<TKey, TValue>);
        public bool Equals(Computation<TKey, TValue>? other) => 
            !ReferenceEquals(null, other) 
                && ReferenceEquals(Function, other.Function)
                && EqualityComparer<TKey>.Default.Equals(Key, other.Key);

        public override int GetHashCode() 
            => HashCode.Combine(Function, EqualityComparer<TKey>.Default.GetHashCode(Key));
        
        public static bool operator ==(Computation<TKey, TValue>? left, Computation<TKey, TValue>? right) => Equals(left, right);
        public static bool operator !=(Computation<TKey, TValue>? left, Computation<TKey, TValue>? right) => !Equals(left, right);
    }
}
