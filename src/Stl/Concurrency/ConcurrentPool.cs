using Stl.OS;
using Stl.Pooling;

namespace Stl.Concurrency;

public class ConcurrentPool<T> : IPool<T>
{
    public static int DefaultCapacity => Math.Min(64, HardwareInfo.GetProcessorCountPo2Factor(32));

    private readonly StochasticCounter _count;
    private readonly ConcurrentQueue<T> _queue;
    private readonly Func<T> _itemFactory;

    public int Capacity { get; }

    public ConcurrentPool(Func<T> itemFactory)
        : this(itemFactory, DefaultCapacity) { }
    public ConcurrentPool(Func<T> itemFactory, int capacity)
        : this(itemFactory, capacity, StochasticCounter.DefaultApproximationFactor) { }
    public ConcurrentPool(Func<T> itemFactory, int capacity, int counterApproximationFactor)
    {
        Capacity = capacity;
        _count = new StochasticCounter(counterApproximationFactor);
        _queue = new ConcurrentQueue<T>();
        _itemFactory = itemFactory ?? throw new ArgumentNullException(nameof(itemFactory));
    }

    public ResourceLease<T> Rent()
    {
        if (_queue.TryDequeue(out var resource)) {
            _count.Decrement(resource!.GetHashCode(), out _);
            return new ResourceLease<T>(resource, this);
        }
        _count.Reset();
        return new ResourceLease<T>(_itemFactory(), this);
    }

    bool IResourceReleaser<T>.Release(T resource)
    {
        if (_count.ApproximateValue >= Capacity)
            return false;
        _count.Increment(resource!.GetHashCode(), out _);
        _queue.Enqueue(resource);
        return true;
    }
}
