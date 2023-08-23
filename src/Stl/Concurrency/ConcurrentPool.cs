using Stl.OS;
using Stl.Pooling;

namespace Stl.Concurrency;

public class ConcurrentPool<T>(Func<T> itemFactory, int capacity, int counterApproximationFactor) : IPool<T>
{
    public static int DefaultCapacity => Math.Min(64, HardwareInfo.GetProcessorCountPo2Factor(32));

    private readonly StochasticCounter _count = new(counterApproximationFactor);
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly Func<T> _itemFactory = itemFactory ?? throw new ArgumentNullException(nameof(itemFactory));

    public int Capacity { get; } = capacity;

    public ConcurrentPool(Func<T> itemFactory)
        : this(itemFactory, DefaultCapacity) { }
    public ConcurrentPool(Func<T> itemFactory, int capacity)
        : this(itemFactory, capacity, StochasticCounter.DefaultApproximationFactor) { }

    public ResourceLease<T> Rent()
    {
        if (_queue.TryDequeue(out var resource)) {
            _count.Decrement(resource!.GetHashCode());
            return new ResourceLease<T>(resource, this);
        }
        _count.Reset();
        return new ResourceLease<T>(_itemFactory(), this);
    }

    bool IResourceReleaser<T>.Release(T resource)
    {
        if (_count.ApproximateValue >= Capacity)
            return false;

        _count.Increment(resource!.GetHashCode());
        _queue.Enqueue(resource);
        return true;
    }
}
