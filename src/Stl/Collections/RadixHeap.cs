using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Stl.Mathematics;

namespace Stl.Collections
{
    public class RadixHeap<T>
    {
        private static readonly Dictionary<T, long> Empty = new Dictionary<T, long>();
        private readonly Dictionary<T, long>[] _buckets;

        public long MinPriority { get; private set; }
        public bool IsEmpty {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buckets[0].Count == 0;
        }

        public RadixHeap(int bucketCount = 65, long minPriority = long.MinValue)
        {
            MinPriority = minPriority;
            _buckets = new Dictionary<T, long>[bucketCount];
            for (var i = 0; i < _buckets.Length; i++)
                _buckets[i] = new Dictionary<T, long>();
        }

        public bool Add(T value, long priority)
        {
            if (priority < MinPriority)
                throw new ArgumentOutOfRangeException(nameof(priority));
            if (IsEmpty) {
                MinPriority = priority;
                _buckets[0].Add(value, priority);
                return true;
            }
            var i = GetBucketIndex(priority);
            return _buckets[i].TryAdd(value, priority);
        }

        public bool Remove(T value, long priority)
        {
            var i = GetBucketIndex(priority);
            var bucket = _buckets[i];
            var result = bucket.Remove(value);
            if (i == 0 && result && bucket.Count == 0)
                UpdateMinPriority();
            return result;
        }

        public bool TryRemoveMin(out T value, out long priority)
        {
            var bucket = _buckets[0];
            if (bucket.Count == 0) {
                value = default!;
                priority = default;
                return false;
            }
            var pair = bucket.First();
            (value, priority) = (pair.Key, pair.Value);
            bucket.Remove(value);
            UpdateMinPriority();
            return true;
        }

        public IReadOnlyDictionary<T, long> RemoveAllMin()
        {
            var bucket = _buckets[0];
            if (bucket.Count == 0)
                return Empty;
            _buckets[0] = new Dictionary<T, long>();
            UpdateMinPriority();
            return bucket;
        }

        private void UpdateMinPriority()
        {
            for (var i = 0; i < _buckets.Length; i++) {
                var bucket = _buckets[i];
                if (bucket.Count != 0) {
                    _buckets[i] = new Dictionary<T, long>();
                    var mp = long.MaxValue;
                    foreach (var (_, priority) in bucket)
                        if (mp > priority)
                            mp = priority;
                    MinPriority = mp;
                    foreach (var (value, priority) in bucket) {
                        var i1 = GetBucketIndex(priority);
                        _buckets[i1].TryAdd(value, priority);
                    }
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetBucketIndex(long priority)
        {
            var xor = MinPriority ^ priority;
            return xor == 0 ? 0 : 1 + Bits.MsbIndex((ulong) xor);
        }
    }
}
