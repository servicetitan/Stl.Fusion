using Stl.Concurrency;
using Stl.OS;
using Stl.Pooling;

namespace Stl.Locking;

#pragma warning disable CA2002

public class AsyncLockSet<TKey>(LockReentryMode reentryMode, int concurrencyLevel, int capacity)
    where TKey : notnull
{
    public static int DefaultConcurrencyLevel => HardwareInfo.GetProcessorCountFactor();
    public static int DefaultCapacity => 31;

    private readonly ConcurrentDictionary<TKey, Entry> _entries = new(concurrencyLevel, capacity);
    private readonly ConcurrentPool<AsyncLock> _lockPool = new(() => new AsyncLock(reentryMode));

    public LockReentryMode ReentryMode { get; } = reentryMode;
    public int Count => _entries.Count;

    public AsyncLockSet(LockReentryMode reentryMode = LockReentryMode.Unchecked)
        : this(reentryMode, DefaultConcurrencyLevel, DefaultCapacity) { }

    public ValueTask<Releaser> Lock(TKey key, CancellationToken cancellationToken = default)
    {
        // This has to be non-async method, otherwise AsyncLocals
        // created inside it won't be available in caller's ExecutionContext.
        var (asyncLock, entry) = PrepareLock(key);
        try {
            var task = asyncLock.Lock(cancellationToken);
            return ToReleaserTask(entry, task);
        }
        catch {
            entry.EndUse();
            throw;
        }
    }

    // Private methods

    private (AsyncLock, Entry) PrepareLock(TKey key)
    {
        var spinWait = new SpinWait();
        while (true) {
            var entry = _entries.GetOrAdd(key, static (key1, self) => new Entry(self, key1), this);
            var asyncLock = entry.TryBeginUse();
            if (asyncLock != null)
                return (asyncLock, entry);
            spinWait.SpinOnce(); // Safe for WASM
        }
    }

    private static async ValueTask<Releaser> ToReleaserTask(Entry entry, ValueTask<AsyncLock.Releaser> task)
    {
        try {
            var releaser = await task.ConfigureAwait(false);
            return new Releaser(entry, releaser);
        }
        catch {
            entry.EndUse();
            throw;
        }
    }

    // Nested types

    private sealed class Entry
    {
        private readonly AsyncLockSet<TKey> _owner;
        private readonly TKey _key;
        private readonly ResourceLease<AsyncLock> _lease;
        private volatile AsyncLock? _asyncLock;
        private int _useCount;

        public Entry(AsyncLockSet<TKey> owner, TKey key)
        {
            _owner = owner;
            _key = key;
            _lease = owner._lockPool.Rent();
            _asyncLock = _lease.Resource;
        }

        public AsyncLock? TryBeginUse()
        {
            lock (this) {
                var asyncLock = _asyncLock;
                if (asyncLock != null)
                    ++_useCount;
                return asyncLock;
            }
        }

        public void EndUse()
        {
            var mustRelease = false;
            lock (this) {
                if (_asyncLock != null && --_useCount == 0) {
                    _asyncLock = null;
                    mustRelease = true;
                }
            }
            if (mustRelease) {
                _owner._entries.TryRemove(_key, this);
                _lease.Dispose();
            }
        }
    }

    // Nested types

    public readonly struct Releaser(object entry, AsyncLock.Releaser releaser) : IAsyncLockReleaser
    {
        private readonly Entry _entry = (Entry)entry;

        public void MarkLockedLocally()
            => releaser.MarkLockedLocally();

        public void Dispose()
        {
            releaser.Dispose();
            _entry.EndUse();
        }
    }
}
