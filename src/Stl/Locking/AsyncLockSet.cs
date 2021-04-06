using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Stl.Collections;
using Stl.Concurrency;
using Stl.OS;
using Stl.Pooling;

namespace Stl.Locking
{
    public interface IAsyncLockSet<in TKey>
        where TKey : notnull
    {
        ReentryMode ReentryMode { get; }
        int AcquiredLockCount { get; }
        bool IsLocked(TKey key);
        bool? IsLockedLocally(TKey key);
        ValueTask<IDisposable> Lock(TKey key, CancellationToken cancellationToken = default);
    }

    public class AsyncLockSet<TKey> : IAsyncLockSet<TKey>
        where TKey : notnull
    {
        public static int DefaultConcurrencyLevel => HardwareInfo.GetProcessorCountFactor();
        public static int DefaultCapacity => OSInfo.IsWebAssembly ? 31 : 509;

        private readonly ConcurrentDictionary<TKey, Entry> _entries;
        private readonly ConcurrentPool<AsyncLock> _lockPool;

        public ReentryMode ReentryMode { get; }
        public int AcquiredLockCount => _entries.Count;

        public AsyncLockSet(
            TaskCreationOptions taskCreationOptions = TaskCreationOptions.RunContinuationsAsynchronously)
            : this(ReentryMode.CheckedFail, taskCreationOptions) { }
        public AsyncLockSet(
            ReentryMode reentryMode,
            TaskCreationOptions taskCreationOptions = TaskCreationOptions.RunContinuationsAsynchronously)
            : this(reentryMode, taskCreationOptions, DefaultConcurrencyLevel, DefaultCapacity) { }
        public AsyncLockSet(
            ReentryMode reentryMode,
            TaskCreationOptions taskCreationOptions,
            int concurrencyLevel, int capacity)
        {
            ReentryMode = reentryMode;
            _entries = new ConcurrentDictionary<TKey, Entry>(concurrencyLevel, capacity);
            _lockPool = new ConcurrentPool<AsyncLock>(
                () => new AsyncLock(ReentryMode, taskCreationOptions));
        }

        public bool IsLocked(TKey key)
        {
            if (_entries.TryGetValue(key, out var entry))
                return false;
            return entry?.AsyncLock?.IsLocked ?? false;
        }

        public bool? IsLockedLocally(TKey key)
        {
            if (ReentryMode == ReentryMode.UncheckedDeadlock)
                return null;
            if (_entries.TryGetValue(key, out var entry))
                return false;
            return entry!.AsyncLock?.IsLockedLocally;
        }

        ValueTask<IDisposable> IAsyncLockSet<TKey>.Lock(
            TKey key, CancellationToken cancellationToken)
        {
            // This has to be non-async method, otherwise AsyncLocals
            // created inside it won't be available in caller's ExecutionContext.
            var (asyncLock, entry) = PrepareLock(key);
            try {
                var task = asyncLock.Lock(cancellationToken);
                return ToReleaserTaskSlow(entry, task);
            }
            catch {
                entry.EndUse();
                throw;
            }
        }

        public ValueTask<Releaser> Lock(
            TKey key, CancellationToken cancellationToken = default)
        {
            // This has to be non-async method, otherwise AsyncLocals
            // created inside it won't be available in caller's ExecutionContext.
            var (asyncLock, entry) = PrepareLock(key);
            try {
                var task = asyncLock.Lock(cancellationToken);
                return ToReleaserTaskFast(entry, task);
            }
            catch {
                entry.EndUse();
                throw;
            }
        }

        private (AsyncLock, Entry) PrepareLock(TKey key)
        {
            var spinWait = new SpinWait();
            for (;;) {
                var entry = _entries.GetOrAdd(key, (key1, self) => new Entry(self, key1), this);
                var asyncLock = entry.TryBeginUse();
                if (asyncLock != null)
                    return (asyncLock, entry);
                spinWait.SpinOnce();
            }
        }

        private async ValueTask<Releaser> ToReleaserTaskFast(Entry entry, ValueTask<AsyncLock.Releaser> task)
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

        private async ValueTask<IDisposable> ToReleaserTaskSlow(Entry entry, ValueTask<AsyncLock.Releaser> task)
        {
            try {
                var releaser = await task.ConfigureAwait(false);
                // ReSharper disable once HeapView.BoxingAllocation
                return new Releaser(entry, releaser);
            }
            catch {
                entry.EndUse();
                throw;
            }
        }

        private class Entry
        {
            private readonly AsyncLockSet<TKey> _owner;
            private readonly TKey _key;
            private readonly ResourceLease<AsyncLock> _lease;
            private volatile AsyncLock? _asyncLock;
            private int _useCount;

            public AsyncLock? AsyncLock => _asyncLock;

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
                    if (_asyncLock != null && 0 == --_useCount) {
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

        public struct Releaser : IDisposable
        {
            private readonly Entry _entry;
            private AsyncLock.Releaser _asyncLockReleaser;

            public Releaser(object entry, AsyncLock.Releaser asyncLockReleaser)
            {
                _entry = (Entry) entry;
                _asyncLockReleaser = asyncLockReleaser;
            }

            public void Dispose()
            {
                try {
                    _asyncLockReleaser.Dispose();
                }
                finally {
                    _entry.EndUse();
                }
            }
        }
    }
}
