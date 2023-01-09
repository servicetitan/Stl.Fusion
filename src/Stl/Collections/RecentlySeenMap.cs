namespace Stl.Collections;

public sealed class RecentlySeenMap<TKey, TValue>
{
    private readonly BinaryHeap<Moment, TKey> _heap;
    private readonly Dictionary<TKey, TValue> _map;

    public int Capacity { get; }
    public TimeSpan Duration { get; }
    public IMomentClock Clock { get; }

    public RecentlySeenMap(int capacity, TimeSpan duration, IMomentClock? clock = null)
    {
        Capacity = capacity;
        Duration = duration;
        Clock = clock ?? MomentClockSet.Default.SystemClock;

        _heap = new BinaryHeap<Moment, TKey>(capacity + 1); // we may add one extra item, so "+ 1"
        _map = new Dictionary<TKey, TValue>(capacity + 1); // we may add one extra item, so "+ 1"
    }

    public bool TryGet(TKey key, out TValue existingValue)
        => _map.TryGetValue(key, out existingValue);

    public bool TryAdd(TKey key, TValue value = default!)
        => TryAdd(key, Clock.Now, value);

    public bool TryAdd(TKey key, Moment timestamp, TValue value = default!)
    {
        if (!_map.TryAdd(key, value))
            return false;

        _heap.Add(timestamp, key);
        Prune();
        return true;
    }

    public void Prune()
    {
        // Removing some items while there are too many
        while (_map.Count >= Capacity) {
            if (_heap.ExtractMin().IsSome(out var key))
                _map.Remove(key.Value);
            else
                break;
        }

        // Removing too old operations
        var minTimestamp = Clock.Now - Duration;
        while (_heap.PeekMin().IsSome(out var entry) && entry.Priority < minTimestamp) {
            _heap.ExtractMin();
            _map.Remove(entry.Value);
        }
    }
}
