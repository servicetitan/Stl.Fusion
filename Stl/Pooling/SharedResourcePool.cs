using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Stl.Internal;
using Stl.Locking;

namespace Stl.Pooling
{
    public interface ISharedResourcePool<TKey, TResource>
    {
        Task<SharedResourceHandle<TKey, TResource>> TryAcquireAsync(TKey key, CancellationToken cancellationToken = default);
        
        public async Task<SharedResourceHandle<TKey, TResource>> AcquireAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var handle = await TryAcquireAsync(key, cancellationToken).ConfigureAwait(false);
            return handle.IsValid ? handle : throw new KeyNotFoundException();
        }
    }

    public abstract class SharedResourcePoolBase<TKey, TResource> : ISharedResourcePool<TKey, TResource>
        where TKey : notnull
        where TResource : class
    {
        protected const string AsyncLockFailureMessage = "AsyncLock doesn't work properly.";

        private readonly Action<TKey, TResource> _removeResource;
        protected AsyncLock<TKey> Locks { get; }
        protected ConcurrentDictionary<TKey, (TResource? Resource, int Count)> Resources { get; } 

        protected SharedResourcePoolBase(ReentryMode lockReentryMode = ReentryMode.UncheckedDeadlock)
        {
            _removeResource = RemoveResource;
            Locks = new AsyncLock<TKey>(lockReentryMode);
            Resources =  new ConcurrentDictionary<TKey, (TResource? Resource, int Count)>();
        }

        public async Task<SharedResourceHandle<TKey, TResource>> TryAcquireAsync(TKey key, CancellationToken cancellationToken = default)
        {
            await using var @lock = await Locks.LockAsync(key, cancellationToken).ConfigureAwait(false);
            if (!Resources.TryGetValue(key, out var pair)) {
                var resource = await CreateResourceAsync(key).ConfigureAwait(false);
                if (resource == null)
                    return new SharedResourceHandle<TKey, TResource>();
                if (!Resources.TryAdd(key, (resource, 1)!))
                    throw Errors.InternalError(AsyncLockFailureMessage);
                return new SharedResourceHandle<TKey, TResource>(key, resource, _removeResource);
            }
            var newPair = (pair.Resource, pair.Count + 1);
            if (!Resources.TryUpdate(key, newPair, pair))
                throw Errors.InternalError(AsyncLockFailureMessage);
            return new SharedResourceHandle<TKey, TResource>(key, pair.Resource!, _removeResource);
        }

        protected void RemoveResource(TKey key, TResource resource)
        {
            Task.Run(async () => {
                var mustDispose = false;
                await using (var @lock = await Locks.LockAsync(key).ConfigureAwait(false)) {
                    if (!Resources.TryGetValue(key, out var pair))
                        return;
                    var newPair = (pair.Resource, Count: pair.Count - 1 );
                    if (newPair.Count == 0) {
                        if (!Resources.TryRemove(key, pair))
                            throw Errors.InternalError(AsyncLockFailureMessage);
                        mustDispose = true;
                    }
                    else {
                        if (!Resources.TryUpdate(key, newPair, pair))
                            throw Errors.InternalError(AsyncLockFailureMessage);
                    }
                }
                if (mustDispose)
                    // Better to do this outside of lock 
                    await DisposeResourceAsync(key, resource).ConfigureAwait(false);
            });
        }

        protected abstract ValueTask<TResource?> CreateResourceAsync(TKey key);
        protected abstract ValueTask DisposeResourceAsync(TKey key, TResource resource);
    }

    public sealed class SharedResourcePool<TKey, TResource> : SharedResourcePoolBase<TKey, TResource>
        where TKey : notnull
        where TResource : class
    {
        private readonly Func<TKey, ValueTask<TResource?>> _resourceFactory;
        private readonly Func<TKey, TResource, ValueTask> _resourceDisposer;

        public SharedResourcePool(
            Func<TKey, ValueTask<TResource?>> resourceFactory, 
            Func<TKey, TResource, ValueTask> resourceDisposer, 
            ReentryMode lockReentryMode = ReentryMode.UncheckedDeadlock) : base(lockReentryMode)
        {
            _resourceFactory = resourceFactory;
            _resourceDisposer = resourceDisposer;
        }

        protected override ValueTask<TResource?> CreateResourceAsync(TKey key) 
            => _resourceFactory.Invoke(key);
        protected override ValueTask DisposeResourceAsync(TKey key, TResource resource) 
            => _resourceDisposer.Invoke(key, resource);
    }
}
