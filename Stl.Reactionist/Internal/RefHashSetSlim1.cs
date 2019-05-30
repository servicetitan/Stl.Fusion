using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Stl.Reactionist.Internal
{
    public struct RefHashSetSlim1<T>
        where T : class
    {
        private T _item;
        private HashSet<T> _set;
        
        private bool HasSet {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _set != null;
        }
        
        public int Count {
            get {
                if (HasSet) return _set.Count;
                if (_item == null) return 0;
                return 1;
            }
        }

        public bool Contains(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            if (HasSet) return _set.Contains(item);
            if (_item == item) return true;
            return false;
        }

        public bool Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            if (HasSet) return _set.Add(item);
            
            // Item 1
            if (_item == null) {
                _item = item;
                return true;
            }
            if (_item == item) return false;

            _set = new HashSet<T>(ReferenceEqualityComparer<T>.Default) {
                _item, item
            };
            _item = default;
            return true;
        }

        public bool Remove(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            if (HasSet) return _set.Remove(item);
            
            // Item 1
            if (_item == null) return false;
            if (_item == item) {
                _item = default;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _set = null;
            _item = default;
        }

        public IEnumerable<T> Items {
            get {
                if (HasSet) {
                    foreach (var item in _set)
                        yield return item;
                    yield break;
                }
                if (_item == null) yield break;
                yield return _item;
            }
        }
        
        public void Apply<TState>(TState state, Action<TState, T> action)
        {
            if (HasSet) {
                foreach (var item in _set)
                    action(state, item);
                return;
            }
            if (_item == null) return;
            action(state, _item);
        }
        
        
        public void Aggregate<TState>(ref TState state, Aggregator<TState, T> aggregator)
        {
            if (HasSet) {
                foreach (var item in _set)
                    aggregator(ref state, item);
                return;
            }
            if (_item == null) return;
            aggregator(ref state, _item);
        }
    }
}
