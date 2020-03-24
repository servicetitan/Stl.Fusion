using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Internal;
using Stl.Locking;

namespace Stl.Pooling
{
    public interface ISharedResourcePool<TKey, TResource>
    {
        Task<SharedResourceHandle<TKey, TResource>> TryAcquireAsync(TKey key, CancellationToken cancellationToken = default);
    }

    public abstract class SharedResourcePoolBase<TKey, TResource> : ISharedResourcePool<TKey, TResource>
        where TKey : notnull
        where TResource : class
    {
        protected const string AsyncLockFailureMessage = "AsyncLock doesn't work properly.";

        private readonly Action<TKey, TResource> _releaseResource;
        protected AsyncLock<TKey> Locks { get; }
        protected ConcurrentDictionary<TKey, (TResource? Resource, int Count)> Resources { get; }
        protected ConcurrentDictionary<TKey, CancellationTokenSource> ResourceDisposeCancellers { get; } 
        protected bool AllowConcurrentDispose { get; } = false;

        protected SharedResourcePoolBase(
            ReentryMode lockReentryMode = ReentryMode.UncheckedDeadlock,
            bool allowConcurrentDispose = false)
        {
            _releaseResource = ReleaseResource; // That's just to avoid closure allocation on every call
            Resources = new ConcurrentDictionary<TKey, (TResource? Resource, int Count)>();
            ResourceDisposeCancellers = new ConcurrentDictionary<TKey, CancellationTokenSource>();
            AllowConcurrentDispose = allowConcurrentDispose;
            Locks = new AsyncLock<TKey>(lockReentryMode);
        }

        public async Task<SharedResourceHandle<TKey, TResource>> TryAcquireAsync(TKey key, CancellationToken cancellationToken = default)
        {
            await using var @lock = await Locks.LockAsync(key, cancellationToken).ConfigureAwait(false);
            if (!Resources.TryGetValue(key, out var pair)) {
                var resource = await CreateResourceAsync(key, cancellationToken).ConfigureAwait(false);
                if (resource == null)
                    return new SharedResourceHandle<TKey, TResource>();
                if (!Resources.TryAdd(key, (resource, 1)!))
                    throw Errors.InternalError(AsyncLockFailureMessage);
                return new SharedResourceHandle<TKey, TResource>(key, resource, _releaseResource);
            }
            var newPair = (pair.Resource, pair.Count + 1);
            if (!Resources.TryUpdate(key, newPair, pair))
                throw Errors.InternalError(AsyncLockFailureMessage);
            if (pair.Count == 0) {
                ResourceDisposeCancellers.TryRemove(key, out var cts);
                cts?.Cancel();
            }
            return new SharedResourceHandle<TKey, TResource>(key, pair.Resource!, _releaseResource);
        }

        protected void ReleaseResource(TKey key, TResource resource)
        {
            Task.Run(async () => {
                var delayedDisposeCts = (CancellationTokenSource?) null;
                // ReSharper disable once MethodSupportsCancellation
                await using (await Locks.LockAsync(key).ConfigureAwait(false)) {
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
                if (delayedDisposeCts != null)
                    // Better to do this outside of lock; "fire & forget"-style launch is fine here as well. 
#pragma warning disable 4014
                    DelayedDisposeAsync(key, resource, delayedDisposeCts.Token);
#pragma warning restore 4014
            });
        }

        protected async Task DelayedDisposeAsync(TKey key, TResource resource, CancellationToken cancellationToken)
        {
            await DisposeResourceDelayAsync(key, resource, cancellationToken);
            await using (await Locks.LockAsync(key, cancellationToken).ConfigureAwait(false)) {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Resources.TryRemove(key, (resource, 0)!))
                    throw Errors.InternalError(AsyncLockFailureMessage);
                if (ResourceDisposeCancellers.TryGetValue(key, out var cts))
                    ResourceDisposeCancellers.TryRemove(key, cts);
                if (!AllowConcurrentDispose)
                    // It happens inside the lock in this case
                    await DisposeResourceAsync(key, resource).ConfigureAwait(false);
            }
            if (AllowConcurrentDispose)
                // Happens outside of the lock; "fire & forget"-style launch is fine here as well. 
#pragma warning disable 4014
                DisposeResourceAsync(key, resource);
#pragma warning restore 4014
        }

        protected abstract ValueTask<TResource?> CreateResourceAsync(TKey key, CancellationToken cancellationToken);
        protected abstract ValueTask DisposeResourceAsync(TKey key, TResource resource);
        protected abstract ValueTask DisposeResourceDelayAsync(TKey key, TResource resource, CancellationToken cancellationToken);
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
            bool allowConcurrentDispose = false) : base(lockReentryMode, allowConcurrentDispose)
        {
            _resourceFactory = resourceFactory;
            _resourceDisposer = resourceDisposer;
            _resourceDisposeDelayer = resourceDisposeDelayer 
                ?? ((key, resource, cancellationToken) => ValueTaskEx.CompletedTask); // No delay
        }

        protected override ValueTask<TResource?> CreateResourceAsync(TKey key, CancellationToken cancellationToken) 
            => _resourceFactory.Invoke(key, cancellationToken);
        protected override ValueTask DisposeResourceAsync(TKey key, TResource resource) 
            => _resourceDisposer.Invoke(key, resource);
        protected override ValueTask DisposeResourceDelayAsync(TKey key, TResource resource, CancellationToken cancellationToken) 
            => _resourceDisposeDelayer.Invoke(key, resource, cancellationToken);
    }
}

