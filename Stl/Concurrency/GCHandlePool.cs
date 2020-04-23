using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace Stl.Concurrency
{
    public class GCHandlePool : IDisposable
    {
        public static readonly int DefaultCapacity = 1024;

        private readonly ConcurrentBag<GCHandle> _handles;
        private readonly StochasticCounter _counter;
        private volatile int _capacity;

        public GCHandleType HandleType { get; }

        public int Capacity {
            get => _capacity;
            set => Interlocked.Exchange(ref _capacity, value);
        }

        public GCHandlePool(GCHandleType handleType) : this(handleType, DefaultCapacity) { }
        public GCHandlePool(GCHandleType handleType, int capacity, 
            int counterApproximationFactor = StochasticCounter.DefaultApproximationFactor)
        {
            _handles = new ConcurrentBag<GCHandle>();
            _counter = new StochasticCounter(counterApproximationFactor);
            HandleType = handleType;
            Capacity = capacity;
        }

        ~GCHandlePool() => Clear();

        public void Dispose()
        {
            Clear();
            GC.SuppressFinalize(this);
        }

        public GCHandle Acquire(object? target, int random)
        {
            if (_handles.TryTake(out var handle)) {
                if (random == 0)
                    random = handle.GetHashCode();
                _counter.Decrement(random, out var _);
                if (target != null)
                    handle.Target = target;
                return handle;
            }
            return GCHandle.Alloc(target, HandleType);
        }

        public bool Release(GCHandle handle, int random)
        {
            if (_counter.ApproximateValue >= Capacity) {
                handle.Free();
                return false;
            }
            if (!handle.IsAllocated)
                return false;
            if (random == 0)
                random = handle.GetHashCode();
            handle.Target = null;
            _handles.Add(handle);
            _counter.Increment(random, out var _);
            return true;
        }

        public void Clear()
        {
            while (_handles.TryTake(out var handle))
                handle.Free();
            _counter.ApproximateValue = _handles.Count;
        }
    }
}
