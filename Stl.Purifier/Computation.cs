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

    public abstract class Computation : IEquatable<Computation>
    {
        private HashSetSlim2<Computation> _dependenciesHashSet;
        private IMemoryOwner<Computation>? _dependencies;
        private int _dependencyCount;
        private int _state;

        protected object Lock => this;

        public abstract IAsyncFunc UntypedFunc { get; }
        public abstract object UntypedKey { get; }
        public ComputationState State => (ComputationState) _state;
        public ReadOnlySpan<Computation> Dependencies {
            get {
                if (_dependencies == null)
                    return ReadOnlySpan<Computation>.Empty;
                return _dependencies.Memory.Span.Slice(0, _dependencyCount);
            }
        }
        public event Action<Computation>? Invalidated;

        public override string ToString() 
            => $"{GetType().Name}(Func: {UntypedFunc}, Key: {UntypedKey}, State: {State})";

        public void AddDependency(Computation dependency)
        {
            lock (Lock) {
                AssertStateIs(ComputationState.Computing);
                _dependenciesHashSet.Add(dependency);
            }
        }

        public bool Invalidate()
        {
            if (!TryChangeState(ComputationState.Computed, ComputationState.Invalidated))
                return false;

            var dependencies = _dependencies;
            if (dependencies != null) {
                (_dependencies, _dependencyCount) = (null, 0);
                dependencies.Memory.Span.Fill(null!);
                dependencies.Dispose();
            }
            return true;
        }

        public abstract ValueTask<Option<object>> TryGetUntypedValue();

        // Equality

        public abstract bool Equals(Computation? other);
        public override bool Equals(object? obj) 
            => Equals(obj as Computation);
        public override int GetHashCode() 
            => throw new NotImplementedException();
        public static bool operator ==(Computation? left, Computation? right) 
            => Equals(left, right);
        public static bool operator !=(Computation? left, Computation? right) 
            => !Equals(left, right);

        // Protected & private

        protected bool TryChangeState(ComputationState expectedState, ComputationState newState)
        {
            var isChanged = (int) expectedState == Interlocked.CompareExchange(
                ref _state, (int) newState, (int) expectedState);
            if (isChanged)
                OnStateChanged();
            return isChanged;
        }

        protected virtual void OnStateChanged()
        {
            switch (State) {
            case ComputationState.Computed:
                // Copying dependencies to ~ zero-allocation buffer
                _dependencyCount = _dependenciesHashSet.Count;
                if (_dependencyCount != 0) {
                    _dependencies = MemoryPool<Computation>.Shared.Rent(_dependenciesHashSet.Count);
                    _dependenciesHashSet.CopyTo(_dependencies.Memory.Span);
                    _dependenciesHashSet.Clear();
                }
                break;
            case ComputationState.Invalidated:
                Invalidated?.Invoke(this);
                break;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AssertStateIs(ComputationState expectedState)
        {
            if (State != expectedState)
                throw Errors.WrongComputationState(expectedState, State);
        }
    }

    public abstract class Computation<TKey, TValue> : Computation, IEquatable<Computation<TKey, TValue>> 
        where TKey : notnull
    {
        protected TValue Value { get; set; }

        public IAsyncFunc<TKey, TValue> Func { get; }
        public TKey Key { get; }
        public override IAsyncFunc UntypedFunc => Func;
        public override object UntypedKey => Key;

        public Computation(IAsyncFunc<TKey, TValue> func, TKey key)
        {
            Func = func;
            Key = key;
            Value = default!;
        }

        public abstract ValueTask<Option<TValue>> TryGetValue();
        public override async ValueTask<Option<object>> TryGetUntypedValue() 
            => await TryGetValue().ConfigureAwait(false);

        public void Computed(TValue value)
        {
            AssertStateIs(ComputationState.Computing);
            Value = value;
            if (!TryChangeState(ComputationState.Computing, ComputationState.Computed))
                throw Errors.WrongComputationState(ComputationState.Computing, State);
        }

        // Equality

        public override bool Equals(Computation? other) 
            => other is Computation<TKey, TValue> c
                && ReferenceEquals(Func, c.Func)
                && EqualityComparer<TKey>.Default.Equals(Key, c.Key);
        public bool Equals(Computation<TKey, TValue>? other) => 
            !ReferenceEquals(null, other) 
                && ReferenceEquals(Func, other.Func)
                && EqualityComparer<TKey>.Default.Equals(Key, other.Key);
        public override bool Equals(object? obj) 
            => Equals(obj as Computation<TKey, TValue>);

        public override int GetHashCode() 
            => HashCode.Combine(Func, EqualityComparer<TKey>.Default.GetHashCode(Key));
        
        public static bool operator ==(Computation<TKey, TValue>? left, Computation<TKey, TValue>? right) => Equals(left, right);
        public static bool operator !=(Computation<TKey, TValue>? left, Computation<TKey, TValue>? right) => !Equals(left, right);

        // Protected & private

        protected override void OnStateChanged()
        {
            base.OnStateChanged();
            if (State == ComputationState.Invalidated) {
                foreach (var d in Dependencies)
                    d.Invalidate();
            }
        }
    }
}
