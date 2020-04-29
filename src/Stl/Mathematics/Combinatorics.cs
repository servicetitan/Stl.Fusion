using System;
using System.Collections.Generic;
using System.Numerics;

namespace Stl.Mathematics 
{
    public static class Combinatorics
    {
        public static BigInteger Cnk(int n, int k) =>
            MathEx.Factorial(n) / (MathEx.Factorial(n - k) * MathEx.Factorial(k));

        public static IEnumerable<Memory<T>> Tails<T>(Memory<T> source, bool withEmptySubset = true)
        {
            if (withEmptySubset)
                yield return Memory<T>.Empty;
            if (source.IsEmpty)
                yield break;
            var setSize = source.Length;
            for (var i = setSize - 1; i >= 0; i--)
                yield return source.Slice(i);
        }
        
        public static IEnumerable<List<int>> KOfN(int n, int k, bool exactlyK = true)
        {
            if (k > n)
                throw new ArgumentOutOfRangeException(nameof(k));
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n));
            if (k == 0 || n == 0)
                yield break;
            var prefix = new List<int>(8);
            for (var i = 0; i < k; i++)
                prefix.Add(i);
            while (true) {
                var lastIndex = prefix.Count - 1;
                var next = prefix[lastIndex] + 1;
                if (prefix.Count == k || (next == n && !exactlyK)) 
                    yield return prefix;
                if (next < n) {
                    prefix[lastIndex] = next;
                    while (prefix.Count < k && ++next < n)
                        prefix.Add(next);
                    continue;
                }
                if (lastIndex == 0)
                    break;
                prefix.RemoveAt(lastIndex);
            }
        }
        
        public static IEnumerable<Memory<T>> Subsets<T>(Memory<T> source, bool withEmptySubset = true)
        {
            int AddSubset(Memory<T> src, Memory<T> dst, int dstIndex, ulong mask)
            {
                var srcSpan = src.Span;
                var dstSpan = dst.Span;
                while (mask != 0) {
                    var lsb = Bits.Lsb(mask);
                    var bitIndex = Bits.Index(lsb);
                    dstSpan[dstIndex++] = srcSpan[bitIndex];
                    mask ^= lsb;
                }
                return dstIndex;
            }

            const int maxBufferSize = 8;
            
            if (withEmptySubset)
                yield return Memory<T>.Empty;
            if (source.IsEmpty)
                yield break;
            var setSize = source.Length;
            var subsetCount = 1UL << setSize;
            var bufferSize = setSize * (int) MathEx.Min((ulong) maxBufferSize, subsetCount);
            
            var buffer = Memory<T>.Empty;
            var lastBufferIndex = 0;
            var bufferIndex = 0;
            for (ulong m = 1; m < subsetCount; m++) {
                if (bufferIndex + setSize >= buffer.Length) {
                    buffer = new T[bufferSize];
                    lastBufferIndex = bufferIndex = 0;
                }
                bufferIndex = AddSubset(source, buffer, bufferIndex, m);
                yield return buffer.Slice(lastBufferIndex, bufferIndex - lastBufferIndex);
                lastBufferIndex = bufferIndex;
            }
        }
    }
}
