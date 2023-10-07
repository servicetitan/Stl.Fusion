using Stl.OS;
using Stl.Pooling;

namespace Stl.Concurrency;

public class ConcurrentPool<T>(Func<T> itemFactory, int capacity, int counterPrecision) : IPool<T>
{
    public static int DefaultCapacity => Math.Min(64, HardwareInfo.GetProcessorCountPo2Factor(32));

    private readonly ConcurrentQueue<T> _queue = new();
    private readonly Func<T> _itemFactory = itemFactory ?? throw new ArgumentNullException(nameof(itemFactory));
    private StochasticCounter _size = new(counterPrecision);

    public int Capacity { get; } = capacity;

    public ConcurrentPool(Func<T> itemFactory)
        : this(itemFactory, DefaultCapacity) { }
    public ConcurrentPool(Func<T> itemFactory, int capacity)
        : this(itemFactory, capacity, StochasticCounter.DefaultPrecision) { }

    public ResourceLease<T> Rent()
    {
        if (_queue.TryDequeue(out var resource)) {
            _size.Decrement();
            return new ResourceLease<T>(resource, this);
        }
        _size.Value = 0;
        return new ResourceLease<T>(_itemFactory(), this);
    }

    bool IResourceReleaser<T>.Release(T resource)
    {
        if (!_size.TryIncrement(Capacity))
            return false;

        _queue.Enqueue(resource);
        return true;
    }
}
