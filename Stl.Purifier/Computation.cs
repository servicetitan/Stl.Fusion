using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Collections.Slim;
using Stl.Purifier.Internal;

namespace Stl.Purifier
{
    public enum ComputationState
    {
        Computing = 0,
        Computed,
        Invalidated,
    }

    public interface IComputed : IEquatable<IComputed>
    {
        IFunction Func { get; }
        object Key { get; }
        object? Value { get; }
        ComputationState State { get; }
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
        new IFunction<TKey, TValue> Func { get; }
        new TKey Key { get; }
    }

    public interface IComputation : IComputed, IEquatable<IComputation>
    {
        ReadOnlySpan<IComputation> Dependencies { get; }
        public void AddDependency(IComputation dependency);
    }

    public interface IComputation<TKey, TValue> : IComputation, IComputed<TKey, TValue>, IEquatable<IComputation<TKey, TValue>>
        where TKey : notnull
    {
    }

    public abstract class ComputationBase<TKey, TValue> : IComputation<TKey, TValue>,
        IEquatable<ComputationBase<TKey, TValue>> 
        where TKey : notnull
    {
        private int _state;
        private TValue _value;
        private HashSetSlim2<IComputation> _dependenciesHashSet;
        private IMemoryOwner<IComputation>? _dependencies;
        private int _dependencyCount;

        protected object Lock => this;

        #region "Untyped" versions of properties

        IFunction IComputed.Func => Func;
        // ReSharper disable once HeapView.BoxingAllocation
        object IComputed.Key => Key;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IComputed.Value => Value;
        public IFunction<TKey, TValue> Func { get; }

        #endregion
        
        public ComputationState State => (ComputationState) _state;
        public TKey Key { get; }

        public TValue Value {
            get {
                AssertStateIs(ComputationState.Computed);
                return _value;
            }
        }
        public ReadOnlySpan<IComputation> Dependencies {
            get {
                if (_dependencies == null)
                    return ReadOnlySpan<IComputation>.Empty;
                return _dependencies.Memory.Span.Slice(0, _dependencyCount);
            }
        }

        public event Action<IComputation>? Invalidated;

        protected ComputationBase(IFunction<TKey, TValue> func, TKey key)
        {
            Func = func;
            Key = key;
        }

        public override string ToString() 
            => $"{GetType().Name}(Func: {Func}, Key: {Key}, State: {State})";

        public void AddDependency(IComputation dependency)
        {
            lock (Lock) {
                AssertStateIs(ComputationState.Computing);
                _dependenciesHashSet.Add(dependency);
            }
        }

        public void Computed(TValue value)
        {
            if (!TryChangeState(ComputationState.Computed))
                throw Errors.WrongComputationState(ComputationState.Computing, State);

            _value = value;
            _dependencyCount = _dependenciesHashSet.Count;
            if (_dependencyCount != 0) {
                _dependencies = MemoryPool<IComputation>.Shared.Rent(_dependenciesHashSet.Count);
                _dependenciesHashSet.CopyTo(_dependencies.Memory.Span);
                _dependenciesHashSet.Clear();
            }

            OnComputed();
        }

        public bool Invalidate()
        {
            if (!TryChangeState(ComputationState.Invalidated))
                return false;

            OnInvalidate();

            foreach (var d in Dependencies)
                d.Invalidate();
            Invalidated?.Invoke(this);

            var dependencies = _dependencies;
            if (dependencies != null) {
                (_dependencies, _dependencyCount) = (null, 0);
                dependencies.Memory.Span.Fill(null!);
                dependencies.Dispose();
            }

            return true;
        }

        // Protected & private methods

        protected virtual void OnComputed() { }
        protected virtual void OnInvalidate() { }

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
            => Equals(other as ComputationBase<TKey, TValue>);
        public bool Equals(IComputed? other) 
            => Equals(other as ComputationBase<TKey, TValue>);
        public bool Equals(IComputed<TValue>? other) 
            => Equals(other as ComputationBase<TKey, TValue>);
        public bool Equals(IComputed<TKey, TValue>? other) 
            => Equals(other as ComputationBase<TKey, TValue>);
        public bool Equals(IComputation? other) 
            => Equals(other as ComputationBase<TKey, TValue>);
        public bool Equals(IComputation<TKey, TValue>? other) 
            => Equals(other as ComputationBase<TKey, TValue>);
        public bool Equals(ComputationBase<TKey, TValue>? other) => 
            !ReferenceEquals(null, other) 
                && ReferenceEquals(Func, other.Func)
                && EqualityComparer<TKey>.Default.Equals(Key, other.Key);

        public override int GetHashCode() 
            => HashCode.Combine(Func, EqualityComparer<TKey>.Default.GetHashCode(Key));
        
        public static bool operator ==(ComputationBase<TKey, TValue>? left, ComputationBase<TKey, TValue>? right) => Equals(left, right);
        public static bool operator !=(ComputationBase<TKey, TValue>? left, ComputationBase<TKey, TValue>? right) => !Equals(left, right);
    }
}
