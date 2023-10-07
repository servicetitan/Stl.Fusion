namespace Stl.Concurrency;

public sealed class GCHandlePool(GCHandlePool.Options settings) : IDisposable
{
    public record Options
    {
        public static Options Default { get; } = new();

        public int Capacity { get; init; } = 1024;
        public GCHandleType HandleType { get; init; } = GCHandleType.Weak;
        public int OperationCounterPrecision { get; init; } = StochasticCounter.DefaultPrecision;
    }

    private readonly ConcurrentQueue<GCHandle> _queue = new();
    private StochasticCounter _opCounter = new(settings.OperationCounterPrecision);

    public GCHandleType HandleType { get; } = settings.HandleType;
    public int Capacity { get; } = settings.Capacity;

    public GCHandlePool() : this(Options.Default) { }
    public GCHandlePool(GCHandleType handleType)
        : this(Options.Default with { HandleType = handleType }) { }

#pragma warning disable MA0055
    ~GCHandlePool() => Dispose();
#pragma warning restore MA0055

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Clear();
    }

    public GCHandle Acquire(object? target)
        => Acquire(target, _opCounter.NextRandom());
    public GCHandle Acquire(object? target, int random)
    {
        if (_queue.TryDequeue(out var handle)) {
            _opCounter.Decrement(random);
            if (target != null)
                handle.Target = target;
            return handle;
        }

        _opCounter.Value = 0;
        return GCHandle.Alloc(target, HandleType);
    }

    public bool Release(GCHandle handle)
        => Release(handle, _opCounter.NextRandom());
    public bool Release(GCHandle handle, int random)
    {
        if (!_opCounter.TryIncrement(Capacity, random)) {
            handle.Free();
            return false;
        }
        if (!handle.IsAllocated)
            return false;

        handle.Target = null;
        _queue.Enqueue(handle);
        return true;
    }

    public void Clear()
    {
        while (_queue.TryDequeue(out var handle))
            handle.Free();
        _opCounter.Value = 0;
    }
}
