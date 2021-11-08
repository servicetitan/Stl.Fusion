namespace Stl.Concurrency;

public class GCHandlePool : IDisposable
{
    public class Options
    {
        public int Capacity { get; set; } = 1024;
        public GCHandleType HandleType { get; set; } = GCHandleType.Weak;
        public StochasticCounter OperationCounter { get; set; } = new();
    }

    private readonly ConcurrentQueue<GCHandle> _queue;
    private readonly StochasticCounter _opCounter;
    private volatile int _capacity;

    public GCHandleType HandleType { get; }

    public int Capacity {
        get => _capacity;
        set => Interlocked.Exchange(ref _capacity, value);
    }

    public GCHandlePool(GCHandleType handleType) : this(new Options() { HandleType = handleType }) { }
    public GCHandlePool(Options? options = null)
    {
        options ??= new();
        _queue = new ConcurrentQueue<GCHandle>();
        _opCounter = options.OperationCounter;
        HandleType = options.HandleType;
        Capacity = options.Capacity;
    }

#pragma warning disable MA0055
    ~GCHandlePool() => Dispose();
#pragma warning restore MA0055

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Clear();
    }

    public GCHandle Acquire(object? target)
        => Acquire(target, Thread.CurrentThread.ManagedThreadId);
    public GCHandle Acquire(object? target, int random)
    {
        if (_queue.TryDequeue(out var handle)) {
            if (random == 0)
                random = handle.GetHashCode();
            _opCounter.Decrement(random, out var _);
            if (target != null)
                handle.Target = target;
            return handle;
        }
        _opCounter.Reset();
        return GCHandle.Alloc(target, HandleType);
    }

    public bool Release(GCHandle handle)
        => Release(handle, Thread.CurrentThread.ManagedThreadId);
    public bool Release(GCHandle handle, int random)
    {
        if (_opCounter.ApproximateValue >= Capacity) {
            handle.Free();
            return false;
        }
        if (!handle.IsAllocated)
            return false;
        if (random == 0)
            random = handle.GetHashCode();
        _opCounter.Increment(random, out var _);
        _queue.Enqueue(handle);
        handle.Target = null;
        return true;
    }

    public void Clear()
    {
        while (_queue.TryDequeue(out var handle))
            handle.Free();
        _opCounter.ApproximateValue = _queue.Count;
    }
}
