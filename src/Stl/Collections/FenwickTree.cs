using System;
using System.Collections.Generic;

namespace Stl.Collections
{
    public class FenwickTree<T>
    {
        private T[] _nodes;

        public int Count => _nodes.Length - 1;
        public Func<T, T, T> Addition { get; }

        public FenwickTree(FenwickTree<T> source)
        {
            _nodes = new T[source._nodes.Length];
            Array.Copy(source._nodes, _nodes, _nodes.Length);
            Addition = source.Addition;
        }

        public FenwickTree(int count, Func<T, T, T> addition)
        {
            _nodes = new T[count + 1];
            Addition = addition;
        }

        public FenwickTree(T[] source, Func<T, T, T> addition)
        {
            _nodes = new T[source.Length + 1];
            Addition = addition;
            for (var index = 0; index < source.Length; index++)
                Increment(index, source[index]);
        }

        public FenwickTree(IReadOnlyList<T> source, Func<T, T, T> addition)
        {
            _nodes = new T[source.Count + 1];
            Addition = addition;
            for (var index = 0; index < source.Count; index++)
                Increment(index, source[index]);
        }

        public void Increment(int index, T value)
        {
            index++;
            while (index < _nodes.Length) {
                _nodes[index] = Addition(_nodes[index], value);
                index += index & -index; // Add LSB
            }
        }

        public T GetSum(int index)
        {
            var sum = (T) default!;
            if (index < 0)
                return sum;
            index++;
            if (index >= _nodes.Length)
                index = _nodes.Length - 1;
            while (index > 0) {
                sum = Addition(sum, _nodes[index]);
                index -= index & -index;
            }
            return sum;
        }
    }
}
