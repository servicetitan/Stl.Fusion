using Stl.Internal;

namespace Stl.Collections;

public class RadixHeapSet<T> : IEnumerable<(long Priority, T Value)>
    where T : notnull
{
    private static readonly Option<(long Priority, T Value)> None = Option<(long Priority, T Value)>.None;
    private static readonly Dictionary<T, long> Empty = new();
    private readonly Dictionary<T, int> _bucketIndexes;
    private readonly Dictionary<T, long>[] _buckets;

    public int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _bucketIndexes.Count;
    }
    public long MinPriority { get; private set; }

    public RadixHeapSet(int bucketCount = 65, long minPriority = 0)
    {
        MinPriority = minPriority;
        _bucketIndexes = new Dictionary<T, int>();
        _buckets = new Dictionary<T, long>[bucketCount];
        for (var i = 0; i < _buckets.Length; i++)
            _buckets[i] = new Dictionary<T, long>();
    }

    public RadixHeapSet(RadixHeapSet<T> other)
    {
        MinPriority = other.MinPriority;
        _bucketIndexes = new Dictionary<T, int>(other._bucketIndexes);
        _buckets = new Dictionary<T, long>[other._buckets.Length];
        for (var i = 0; i < _buckets.Length; i++)
            _buckets[i] = new Dictionary<T, long>(other._buckets[i]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<(long Priority, T Value)> GetEnumerator()
    {
        var copy = new RadixHeapSet<T>(this);
        while (true) {
            var min = copy.ExtractMin();
            if (min.IsSome(out var result))
                yield return result;
            else
                break;
        }
    }
    public bool Add(long priority, T value)
    {
        var index = GetBucketIndex(priority);
        if (!_bucketIndexes.TryAdd(value, index))
            return false;
        _buckets[index].Add(value, priority);
        return true;
    }

    public void AddOrUpdate(long priority, T value)
    {
        var index = GetBucketIndex(priority);
        var bucket = _buckets[index];
        if (_bucketIndexes.TryGetValue(value, out var oldIndex)) {
            if (oldIndex == index) {
                bucket[value] = priority;
                return;
            }
            _buckets[oldIndex].Remove(value);
        }
        _bucketIndexes[value] = index;
        bucket.Add(value, priority);
    }

    public bool AddOrUpdateToLower(long priority, T value)
    {
        var index = GetBucketIndex(priority);
        var bucket = _buckets[index];
        if (_bucketIndexes.TryGetValue(value, out var oldIndex)) {
            if (oldIndex == index) {
                var oldPriority = bucket[value];
                if (oldPriority <= priority)
                    return false;
                bucket[value] = priority;
                return true;
            }
            else {
                var oldBucket = _buckets[oldIndex];
                var oldPriority = oldBucket[value];
                if (oldPriority <= priority)
                    return false;
                oldBucket.Remove(value);
            }
        }
        _bucketIndexes[value] = index;
        bucket.Add(value, priority);
        return true;
    }

    public bool AddOrUpdateToHigher(long priority, T value)
    {
        var index = GetBucketIndex(priority);
        var bucket = _buckets[index];
        if (_bucketIndexes.TryGetValue(value, out var oldIndex)) {
            if (oldIndex == index) {
                var oldPriority = bucket[value];
                if (oldPriority >= priority)
                    return false;
                bucket[value] = priority;
                return true;
            }
            else {
                var oldBucket = _buckets[oldIndex];
                var oldPriority = oldBucket[value];
                if (oldPriority >= priority)
                    return false;
                oldBucket.Remove(value);
            }
        }
        _bucketIndexes[value] = index;
        bucket.Add(value, priority);
        return true;
    }

    public bool Remove(T value, out long priority)
    {
        if (!_bucketIndexes.Remove(value, out var index)) {
            priority = default;
            return false;
        }
        _buckets[index].Remove(value, out priority);
        return true;
    }

    public Option<(long Priority, T Value)> PeekMin()
    {
        UpdateMinPriority();
        if (Count == 0)
            return None;
        var bucket = _buckets[0];
        var result = bucket.First();
        return Option.Some((result.Value, result.Key));
    }

    public IReadOnlyDictionary<T, long> PeekMinSet()
    {
        UpdateMinPriority();
        return _buckets[0];
    }

    public Option<(long Priority, T Value)> ExtractMin()
    {
        UpdateMinPriority();
        if (Count == 0)
            return None;
        var bucket = _buckets[0];
        var result = bucket.First();
        bucket.Remove(result.Key);
        _bucketIndexes.Remove(result.Key);
        return Option.Some((result.Value, result.Key));
    }

    public IReadOnlyDictionary<T, long> ExtractMinSet()
    {
        UpdateMinPriority();
        return ExtractBucket0();
    }

    public IReadOnlyDictionary<T, long> ExtractMinSet(long priority)
    {
        if (priority < MinPriority || Count == 0)
            return Empty;
        if (priority == MinPriority)
            return ExtractBucket0();
        for (var index = 0; index < _buckets.Length; index++) {
            var bucket = _buckets[index];
            if (bucket.Count != 0) {
                if (index == 0)
                    throw new ArgumentOutOfRangeException(nameof(priority));
                var minPriority = long.MaxValue;
                foreach (var (_, p) in bucket)
                    if (p < minPriority) minPriority = p;
                if (priority > minPriority)
                    throw new ArgumentOutOfRangeException(nameof(priority));
                MinPriority = priority;
                _buckets[index] = new Dictionary<T, long>();
                foreach (var (value, p) in bucket) {
                    var i = GetBucketIndexUnchecked(p);
                    _bucketIndexes[value] = i;
                    _buckets[i].Add(value, p);
                }
                return ExtractBucket0();
            }
        }
        throw Errors.InternalError(
            $"{GetType().GetName()}: internal structure is corrupted.");
    }

    // Private methods

    private void UpdateMinPriority()
    {
        if (Count == 0)
            return;
        for (var index = 0; index < _buckets.Length; index++) {
            var bucket = _buckets[index];
            if (bucket.Count != 0) {
                if (index == 0)
                    // No need for update
                    return;
                var minPriority = long.MaxValue;
                foreach (var (_, p) in bucket)
                    if (p < minPriority) minPriority = p;
                MinPriority = minPriority;
                _buckets[index] = new Dictionary<T, long>();
                foreach (var (value, p) in bucket) {
                    var i = GetBucketIndexUnchecked(p);
                    _bucketIndexes[value] = i;
                    _buckets[i].Add(value, p);
                }
                return;
            }
        }
        throw Errors.InternalError(
            $"{GetType().GetName()}: internal structure is corrupted.");
    }

    private IReadOnlyDictionary<T, long> ExtractBucket0()
    {
        var bucket = _buckets[0];
        if (bucket.Count != 0) {
            _buckets[0] = new Dictionary<T, long>();
            foreach (var pair in bucket)
                _bucketIndexes.Remove(pair.Key);
        }
        return bucket;
    }

    private int GetBucketIndex(long priority)
    {
        if (priority < MinPriority)
            throw new ArgumentOutOfRangeException(nameof(priority));
        var xor = MinPriority ^ priority;
        return xor == 0 ? 0 : 1 + Bits.MsbIndex((ulong) xor);
    }

    private int GetBucketIndexUnchecked(long priority)
    {
        var xor = MinPriority ^ priority;
        return xor == 0 ? 0 : 1 + Bits.MsbIndex((ulong) xor);
    }
}
