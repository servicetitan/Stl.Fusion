using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Stl.Reactionist.Internal
{
    public struct HashSetSlim1<T>
        where T : notnull
    {
        private int _count;
        private T _item;
        private HashSet<T>? _set;
        
        private bool HasSet {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _set != null;
        }
        
        public int Count {
            get {
                if (HasSet) return _set!.Count;
                return _count;
            }
        }

        public bool Contains(T item)
        {
            if (HasSet) return _set!.Contains(item);
            if (_count >= 1 && EqualityComparer<T>.Default.Equals(_item, item)) return true;
            return false;
        }

        public bool Add(T item)
        {
            if (HasSet) return _set!.Add(item);
            
            // Item 1
            if (_count < 1) {
                _item = item;
                _count++;
                return true;
            }
            if (EqualityComparer<T>.Default.Equals(_item, item)) return true;

            _set = new HashSet<T> {
                _item, item
            };
            _item = default!;
            _count = -1;
            return true;
        }

        public bool Remove(T item)
        {
            if (HasSet) return _set!.Remove(item);
            
            // Item 1
            if (_count < 1) return false;
            if (EqualityComparer<T>.Default.Equals(_item, item)) {
                _item = default!;
                _count--;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _set = null;
            _item = default!;
            _count = 0;
        }

        public IEnumerable<T> Items {
            get {
                if (HasSet) {
                    foreach (var item in _set!)
                        yield return item;
                    yield break;
                }
                if (_count < 1) yield break;
                yield return _item;
            }
        }
        
        public void Apply<TState>(TState state, Action<TState, T> action)
        {
            if (HasSet) {
                foreach (var item in _set!)
                    action(state, item);
                return;
            }
            if (_count < 1) return;
            action(state, _item);
        }
        
        public void Aggregate<TState>(ref TState state, Aggregator<TState, T> aggregator)
        {
            if (HasSet) {
                foreach (var item in _set!)
                    aggregator(ref state, item);
                return;
            }
            if (_count < 1) return;
            aggregator(ref state, _item);
        }
    }
}
