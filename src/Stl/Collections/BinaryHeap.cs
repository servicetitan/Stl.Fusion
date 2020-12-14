using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Stl.Collections
{
    public class BinaryHeap<TPriority, TValue> : IEnumerable<(TPriority Priority, TValue Value)>
    {
        private readonly Option<(TPriority Priority, TValue Value)> _none = default;
        private readonly IComparer<TPriority> _comparer;
        private readonly List<(TPriority Key, TValue Value)> _heap = new();

        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _heap.Count;
        }

        public BinaryHeap(IComparer<TPriority>? comparer = null) =>
            _comparer = comparer ?? Comparer<TPriority>.Default;

        public BinaryHeap(BinaryHeap<TPriority, TValue> other)
        {
            _comparer = other._comparer;
            _heap = other._heap.ToList();
        }

        public BinaryHeap(IEnumerable<(TPriority, TValue)> source, IComparer<TPriority>? comparer = null)
            : this(comparer) =>
            _heap = source.OrderBy(i => i.Item1, _comparer).ToList();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<(TPriority Priority, TValue Value)> GetEnumerator()
        {
            var copy = new BinaryHeap<TPriority, TValue>(this);
            for (;;) {
                var min = copy.ExtractMin();
                if (min.IsSome(out var result))
                    yield return result;
                else
                    break;
            }
        }

        public void Add(TPriority priority, TValue value)
        {
            _heap.Add((priority, value));
            FixBottomUp(Count - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<(TPriority Priority, TValue Value)> PeekMin()
            => Count == 0 ? _none : _heap[0];

        public Option<(TPriority Priority, TValue Value)> ExtractMin()
        {
            if (Count == 0)
                return _none;
            var result = Option.Some(_heap[0]);
            var lastIndex = Count - 1;
            _heap[0] = _heap[lastIndex];
            _heap.RemoveAt(lastIndex);
            FixTopDown(0);
            return result;
        }

        public void Clear() => _heap.Clear();

        // Private part

        private bool IsValidIndex(int index) => index < Count && index >= 0;
        private static int GetFirstChildIndex(int index) => ((index + 1) << 1) - 1;
        private static int GetParentIndex(int index) => ((index + 1) >> 1) - 1;

        private void FixTopDown(int i)
        {
            while (true) {
                var l = GetFirstChildIndex(i);
                var r = l + 1;
                var minIndex = i;
                if (IsValidIndex(l) && _comparer.Compare(_heap[l].Key, _heap[minIndex].Key) < 0)
                    minIndex = l;
                if (IsValidIndex(r) && _comparer.Compare(_heap[r].Key, _heap[minIndex].Key) < 0)
                    minIndex = r;
                if (minIndex == i)
                    break;
                (_heap[minIndex], _heap[i]) = (_heap[i], _heap[minIndex]);
                i = minIndex;
            }
        }

        private void FixBottomUp(int i)
        {
            while (i > 0) {
                var p = GetParentIndex(i);
                if (_comparer.Compare(_heap[i].Key, _heap[p].Key) > 0)
                    break;
                (_heap[p], _heap[i]) = (_heap[i], _heap[p]);
                i = p;
            }
        }
    }
}
