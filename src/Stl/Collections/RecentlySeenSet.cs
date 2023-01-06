namespace Stl.Collections;

public sealed class RecentlySeenSet<T>
{
    private readonly BinaryHeap<Moment, T> _heap;
    private readonly HashSet<T> _set;

    public int Capacity { get; }
    public TimeSpan Duration { get; }
    public IMomentClock Clock { get; }

    public RecentlySeenSet(int capacity, TimeSpan duration, IMomentClock? clock = null)
    {
        Capacity = capacity;
        Duration = duration;
        Clock = clock ?? MomentClockSet.Default.SystemClock;

        _heap = new BinaryHeap<Moment, T>(capacity + 1); // we may add one extra item, so "+ 1"
        _set = new HashSet<T>();
    }

    public bool TryAdd(T item)
        => TryAdd(item, Clock.Now);

    public bool TryAdd(T item, Moment timestamp)
    {
        if (!_set.Add(item))
            return false;

        _heap.Add(timestamp, item);
        Prune();
        return true;
    }

    public void Prune()
    {
        // Removing some items while there are too many
        while (_set.Count >= Capacity) {
            if (_heap.ExtractMin().IsSome(out var value))
                _set.Remove(value.Value);
            else
                break;
        }

        // Removing too old operations
        var minTimestamp = Clock.Now - Duration;
        while (_heap.PeekMin().IsSome(out var value) && value.Priority < minTimestamp) {
            _heap.ExtractMin();
            _set.Remove(value.Value);
        }
    }
}
