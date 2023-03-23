using Stl.Concurrency;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Locking;
using Stl.OS;
using Stl.Time.Internal;
using Errors = Stl.Fusion.Internal.Errors;

namespace Stl.Fusion;

public sealed class ComputedRegistry : IDisposable
{
    public static ComputedRegistry Instance { get; set; } = new();

    public sealed record Options
    {
        internal static readonly PrimeSieve CapacityPrimeSieve;

        public static int DefaultInitialCapacity { get; }
        public static int DefaultInitialConcurrency { get; }
        public static Options Default { get; }

        public int InitialCapacity { get; init; } = DefaultInitialCapacity;
        public int ConcurrencyLevel { get; init; } = DefaultInitialConcurrency;
        public Func<AsyncLockSet<ComputedInput>>? LocksFactory { get; init; } = null;
        public GCHandlePool? GCHandlePool { get; init; } = null;

        static Options()
        {
            DefaultInitialConcurrency = HardwareInfo.GetProcessorCountPo2Factor(4);
            var capacity = HardwareInfo.GetProcessorCountPo2Factor(128, 128);
            CapacityPrimeSieve = new PrimeSieve(capacity + 1024);
            while (!CapacityPrimeSieve.IsPrime(capacity))
                capacity--;
            DefaultInitialCapacity = capacity;
            Default = new();
        }
    }

    private readonly ConcurrentDictionary<ComputedInput, GCHandle> _storage;
    private readonly GCHandlePool _gcHandlePool;
    private readonly StochasticCounter _opCounter;
    private volatile ComputedGraphPruner _graphPruner = null!;
    private volatile int _pruneCounterThreshold;
    private Task? _pruneTask;
    private object Lock => _storage;

    public IEnumerable<ComputedInput> Keys => _storage.Select(p => p.Key);
    public AsyncLockSet<ComputedInput> InputLocks { get; }
    public ComputedGraphPruner GraphPruner => _graphPruner;

    public event Action<IComputed>? OnRegister;
    public event Action<IComputed>? OnUnregister;
    public event Action<IComputed, bool>? OnAccess;

    public ComputedRegistry() : this(Options.Default) { }
    public ComputedRegistry(Options options)
    {
        _storage = new ConcurrentDictionary<ComputedInput, GCHandle>(options.ConcurrencyLevel, options.InitialCapacity);
        _gcHandlePool = options.GCHandlePool ?? new GCHandlePool(GCHandleType.Weak);
        if (_gcHandlePool.HandleType != GCHandleType.Weak)
            throw new ArgumentOutOfRangeException(
                $"{nameof(options)}.{nameof(options.GCHandlePool)}.{nameof(_gcHandlePool.HandleType)}");
        _opCounter = new StochasticCounter();
        InputLocks = options.LocksFactory?.Invoke() ?? new AsyncLockSet<ComputedInput>(
            ReentryMode.CheckedFail,
            TaskCreationOptions.RunContinuationsAsynchronously,
            options.ConcurrencyLevel, options.InitialCapacity);
        ChangeGraphPruner(new ComputedGraphPruner(new()), null!);
        UpdatePruneCounterThreshold();
    }

    public void Dispose()
        => _gcHandlePool.Dispose();

    public IComputed? Get(ComputedInput key)
    {
        var random = Randomize(key.HashCode);
        OnOperation(random);
        if (_storage.TryGetValue(key, out var handle)) {
            var value = (IComputed?) handle.Target;
            if (value != null)
                return value;

            if (_storage.TryRemove(key, handle))
                _gcHandlePool.Release(handle, random);
        }
        return null;
    }

    public void Register(IComputed computed)
    {
        // Debug.WriteLine($"{nameof(Register)}: {computed}");

        OnRegister?.Invoke(computed);
        var key = computed.Input;
        var random = Randomize(key.HashCode);
        OnOperation(random);

        var spinWait = new SpinWait();
        GCHandle? newHandle = null;
        while (computed.ConsistencyState != ConsistencyState.Invalidated) {
            if (_storage.TryGetValue(key, out var handle)) {
                var target = (IComputed?) handle.Target;
                if (target == computed) {
                    if (newHandle.HasValue)
                        _gcHandlePool.Release(newHandle.Value, random);
                    return;
                }
                if (target is { ConsistencyState: not ConsistencyState.Invalidated }) {
                    // This typically triggers Unregister - except for ReplicaMethodComputed
                    target.Invalidate();
                }
                if (_storage.TryRemove(key, handle))
                    _gcHandlePool.Release(handle, random);
            }
            else {
                newHandle ??= _gcHandlePool.Acquire(computed, random);
                if (_storage.TryAdd(key, newHandle.GetValueOrDefault()))
                    return;
            }
            spinWait.SpinOnce(); // Safe for WASM
        }
    }

    public void Unregister(IComputed computed)
    {
        // We can't remove what still could be invalidated,
        // since "usedBy" links are resolved via this registry
        if (computed.ConsistencyState != ConsistencyState.Invalidated)
            throw Errors.WrongComputedState(computed.ConsistencyState);

        OnUnregister?.Invoke(computed);
        var key = computed.Input;
        var random = Randomize(key.HashCode);
        OnOperation(random);

        if (!_storage.TryGetValue(key, out var handle))
            return;
        var target = handle.Target;
        if (target != null && !ReferenceEquals(target, computed))
            return;

        // gcHandle.Target == null (is gone, i.e. to be pruned)
        // or pointing to the right computation object
        if (!_storage.TryRemove(key, handle))
            // If another thread removed the entry, it also released the handle
            return;

        _gcHandlePool.Release(handle, random);
    }

    public void PseudoRegister(IComputed computed)
        => OnRegister?.Invoke(computed);

    public void PseudoUnregister(IComputed computed)
        => OnUnregister?.Invoke(computed);

    public void InvalidateEverything()
    {
        var keys = _storage.Keys.ToList();
        foreach (var key in keys)
            Get(key)?.Invalidate();
    }

    public Task Prune()
    {
        lock (Lock) {
            if (_pruneTask == null || _pruneTask.IsCompleted) {
                using var _ = ExecutionContextExt.SuppressFlow();
                _pruneTask = Task.Run(PruneUnsafe);
            }
            return _pruneTask;
        }
    }

    public ComputedGraphPruner ChangeGraphPruner(
        ComputedGraphPruner graphPruner,
        ComputedGraphPruner expectedGraphPruner)
    {
        var oldGraphPruner = Interlocked.CompareExchange(ref _graphPruner, graphPruner, expectedGraphPruner);
        if (oldGraphPruner != expectedGraphPruner)
            return oldGraphPruner;

        graphPruner.Start();
        return graphPruner;
    }

    public void ReportAccess(IComputed computed, bool isNew)
    {
        if (OnAccess != null && computed.Input.Function is IComputeMethodFunction)
            OnAccess.Invoke(computed, isNew);
    }

    // Private methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnOperation(int random)
    {
        if (!_opCounter.Increment(random, out var opCounterValue))
            return;

        if (opCounterValue > _pruneCounterThreshold)
            TryPrune();
    }

    private void TryPrune()
    {
        lock (Lock) {
            // Double check locking
            if (_opCounter.ApproximateValue <= _pruneCounterThreshold)
                return;
            _opCounter.ApproximateValue = 0;
            Prune();
        }
    }

    private void PruneUnsafe()
    {
        var type = GetType();
        using var activity = type.GetActivitySource().StartActivity(type, nameof(Prune));

        // Debug.WriteLine(nameof(PruneInternal));
        var randomOffset = Randomize(Thread.CurrentThread.ManagedThreadId);
        foreach (var (key, handle) in _storage) {
            if (handle.Target == null && _storage.TryRemove(key, handle))
                _gcHandlePool.Release(handle, key.HashCode + randomOffset);
        }
        lock (Lock) {
            UpdatePruneCounterThreshold();
            _opCounter.ApproximateValue = 0;
        }
    }

    private void UpdatePruneCounterThreshold()
    {
        lock (Lock) {
            // Should be called inside Lock
            var capacity = (long) _storage.GetCapacity();
            var nextThreshold = (int) Math.Min(int.MaxValue >> 1, capacity);
            _pruneCounterThreshold = nextThreshold;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Randomize(int random)
        => random + CoarseClockHelper.RandomInt32;
}
