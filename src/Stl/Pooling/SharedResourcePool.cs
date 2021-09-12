using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Concurrency;
using Stl.Internal;
using Stl.Locking;

namespace Stl.Pooling
{
    public interface ISharedResourcePool<TKey, TResource>
    {
        Task<SharedResourceHandle<TKey, TResource>> TryAcquire(TKey key, CancellationToken cancellationToken = default);
    }

    public abstract class SharedResourcePoolBase<TKey, TResource> : ISharedResourcePool<TKey, TResource>
        where TKey : notnull
        where TResource : class
    {
        protected const string AsyncLockFailureMessage = "AsyncLock doesn't work properly.";

        private readonly Func<TKey, TResource, ValueTask> _cachedReleaser;
        protected AsyncLockSet<TKey> LocksSet { get; }
        protected ConcurrentDictionary<TKey, (TResource? Resource, int Count)> Resources { get; }
        protected ConcurrentDictionary<TKey, CancellationTokenSource> ResourceDisposeCancellers { get; }
        protected bool UseConcurrentDispose { get; } = false;

        protected SharedResourcePoolBase(
            ReentryMode lockReentryMode = ReentryMode.UncheckedDeadlock,
            bool useConcurrentDispose = false)
        {
            _cachedReleaser = ReleaseResource; // That's just to avoid closure allocation on every call
            Resources = new ConcurrentDictionary<TKey, (TResource? Resource, int Count)>();
            ResourceDisposeCancellers = new ConcurrentDictionary<TKey, CancellationTokenSource>();
            UseConcurrentDispose = useConcurrentDispose;
            LocksSet = new AsyncLockSet<TKey>(lockReentryMode);
        }

        public async Task<SharedResourceHandle<TKey, TResource>> TryAcquire(TKey key, CancellationToken cancellationToken = default)
        {
            using var @lock = await LocksSet.Lock(key, cancellationToken).ConfigureAwait(false);
            if (!Resources.TryGetValue(key, out var pair)) {
                var resource = await CreateResource(key, cancellationToken).ConfigureAwait(false);
                if (resource == null)
                    return new SharedResourceHandle<TKey, TResource>();
                if (!Resources.TryAdd(key, (resource, 1)!))
                    throw Errors.InternalError(AsyncLockFailureMessage);
                return new SharedResourceHandle<TKey, TResource>(key, resource, _cachedReleaser);
            }
            var newPair = (pair.Resource, pair.Count + 1);
            if (!Resources.TryUpdate(key, newPair, pair))
                throw Errors.InternalError(AsyncLockFailureMessage);
            if (pair.Count == 0) {
                ResourceDisposeCancellers.TryRemove(key, out var cts);
                cts?.Cancel();
            }
            return new SharedResourceHandle<TKey, TResource>(key, pair.Resource!, _cachedReleaser);
        }

        protected async ValueTask ReleaseResource(TKey key, TResource resource)
        {
            var delayedDisposeCts = (CancellationTokenSource?) null;
            try {
                // ReSharper disable once MethodSupportsCancellation
                using (await LocksSet.Lock(key).ConfigureAwait(false)) {
                    if (!Resources.TryGetValue(key, out var pair))
                        return;
                    var newPair = (pair.Resource, Count: pair.Count - 1 );
                    if (newPair.Count == 0) {
                        delayedDisposeCts = new CancellationTokenSource();
                        ResourceDisposeCancellers[key] = delayedDisposeCts;
                    }
                    if (!Resources.TryUpdate(key, newPair, pair))
                        throw Errors.InternalError(AsyncLockFailureMessage);
                }
                // This can be done outside of the lock
                if (delayedDisposeCts != null)
                    await DelayedDisposeResource(key, resource, delayedDisposeCts.Token)
                        .SuppressCancellation()
                        .ConfigureAwait(false);
            }
            catch (Exception e) {
                Debug.WriteLine($"Exception inside Dispose / Dispose async: {e}");
            }
            finally {
                delayedDisposeCts?.Dispose();
            }
        }

        protected async Task DelayedDisposeResource(TKey key, TResource resource, CancellationToken cancellationToken)
        {
            await DisposeResourceDelay(key, resource, cancellationToken);
            using (await LocksSet.Lock(key, cancellationToken).ConfigureAwait(false)) {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Resources.TryRemove(key, (resource, 0)!))
                    throw Errors.InternalError(AsyncLockFailureMessage);
                if (ResourceDisposeCancellers.TryGetValue(key, out var cts))
                    ResourceDisposeCancellers.TryRemove(key, cts);
                if (!UseConcurrentDispose)
                    // It happens inside the lock in this case
                    await DisposeResource(key, resource).ConfigureAwait(false);
            }
            // This can be done outside of the lock
            if (UseConcurrentDispose)
                // ReSharper disable once MethodSupportsCancellation
                _ = Task.Run(() => DisposeResource(key, resource));
        }

        protected abstract ValueTask<TResource?> CreateResource(TKey key, CancellationToken cancellationToken);
        protected abstract ValueTask DisposeResource(TKey key, TResource resource);
        protected abstract ValueTask DisposeResourceDelay(TKey key, TResource resource, CancellationToken cancellationToken);
    }

    public sealed class SharedResourcePool<TKey, TResource> : SharedResourcePoolBase<TKey, TResource>
        where TKey : notnull
        where TResource : class
    {
        private readonly Func<TKey, CancellationToken, ValueTask<TResource?>> _resourceFactory;
        private readonly Func<TKey, TResource, ValueTask> _resourceDisposer;
        private readonly Func<TKey, TResource, CancellationToken, ValueTask> _resourceDisposeDelayer;

        public SharedResourcePool(
            Func<TKey, CancellationToken, ValueTask<TResource?>> resourceFactory,
            Func<TKey, TResource,ValueTask> resourceDisposer,
            Func<TKey, TResource, CancellationToken, ValueTask>? resourceDisposeDelayer = null,
            ReentryMode lockReentryMode = ReentryMode.UncheckedDeadlock,
            bool useConcurrentDispose = false) : base(lockReentryMode, useConcurrentDispose)
        {
            _resourceFactory = resourceFactory;
            _resourceDisposer = resourceDisposer;
            _resourceDisposeDelayer = resourceDisposeDelayer
                ?? ((key, resource, cancellationToken) => ValueTaskEx.CompletedTask); // No delay
        }

        protected override ValueTask<TResource?> CreateResource(TKey key, CancellationToken cancellationToken)
            => _resourceFactory.Invoke(key, cancellationToken);
        protected override ValueTask DisposeResource(TKey key, TResource resource)
            => _resourceDisposer.Invoke(key, resource);
        protected override ValueTask DisposeResourceDelay(TKey key, TResource resource, CancellationToken cancellationToken)
            => _resourceDisposeDelayer.Invoke(key, resource, cancellationToken);
    }
}

