using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Stl.Collections
{
    public class BinaryHeap<T> : IEnumerable<T>
    {
        private readonly IComparer<T> _comparer;
        private readonly List<T> _heap = new List<T>();

        public int Count => _heap.Count;
        public T Min => _heap[0];

        public BinaryHeap(IComparer<T>? comparer = null) => 
            _comparer = comparer ?? Comparer<T>.Default;

        public BinaryHeap(BinaryHeap<T> other)
        {
            _comparer = other._comparer;
            _heap = other._heap.ToList();
        }

        public BinaryHeap(IEnumerable<T> source, IComparer<T>? comparer = null) : this(comparer) => 
            _heap = source.OrderBy(i => i, _comparer).ToList();

        public override string ToString() => $"{GetType().Name} of [{string.Join(", ", this)}]";

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator()
        {
            var copy = new BinaryHeap<T>(this);
            while (copy.Count > 0)
                yield return copy.RemoveMin();
        }

        public T RemoveMin()
        {
            var min = Min;
            var lastIndex = Count - 1;
            _heap[0] = _heap[lastIndex];
            _heap.RemoveAt(lastIndex);
            FixTopDown(0);
            return min;
        }

        public void Add(T item)
        {
            _heap.Add(item);
            FixBottomUp(Count - 1);
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
                if (IsValidIndex(l) && _comparer.Compare(_heap[l], _heap[minIndex]) < 0)
                    minIndex = l;
                if (IsValidIndex(r) && _comparer.Compare(_heap[r], _heap[minIndex]) < 0)
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
                if (_comparer.Compare(_heap[i], _heap[p]) > 0)
                    break;
                (_heap[p], _heap[i]) = (_heap[i], _heap[p]);
                i = p;
            }
        }
    }
}
