using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Channels;
using Stl.Collections;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Bridge.Messages;
using Stl.OS;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public interface IReplicator
    {
        IChannelHub<Message> ChannelHub { get; }
        bool OwnsChannelHub { get; }

        IReplica<T> GetOrAdd<T>(Symbol publisherId, Symbol publicationId, 
            LTagged<Result<T>> initialOutput, bool isConsistent = true, bool requestUpdate = false);
        IReplica? TryGet(Symbol publicationId);
    }

    public interface IReplicatorImpl : IReplicator
    {
        IComputeRetryPolicy RetryPolicy { get; }

        bool TrySubscribe(IReplica replica, bool requestUpdate);
        void OnReplicaDisposed(IReplica replica);
        void OnChannelProcessorDisposed(ReplicatorChannelProcessor replicatorChannelProcessor);
    }

    public class Replicator : AsyncDisposableBase, IReplicatorImpl
    {
        public class Options
        {
            protected static readonly Func<Channel<Message>, Symbol> DefaultPublisherIdResolver =
                c => c is IHasId<Symbol> hasId ? hasId.Id : Symbol.Empty; 

            public IChannelHub<Message> ChannelHub { get; set; } = new ChannelHub<Message>();
            public bool OwnsChannelHub { get; set; } = true;
            public Func<Channel<Message>, Symbol> PublisherIdResolver { get; set; } = DefaultPublisherIdResolver;
            public IComputeRetryPolicy RetryPolicy { get; set; } = ComputeRetryPolicy.Default;
        }

        protected ConcurrentDictionary<Symbol, IReplica> Replicas { get; }
        protected ConcurrentDictionary<Channel<Message>, ReplicatorChannelProcessor> ChannelProcessors { get; }
        protected ConcurrentDictionary<Symbol, ReplicatorChannelProcessor> ChannelProcessorsById { get; }
        protected ChannelAttachedHandler<Message> OnChannelAttachedHandler { get; } 
        protected ChannelDetachedHandler<Message> OnChannelDetachedHandler { get; } 

        public IChannelHub<Message> ChannelHub { get; }
        public bool OwnsChannelHub { get; }
        public Func<Channel<Message>, Symbol> PublisherIdResolver { get; }
        public IComputeRetryPolicy RetryPolicy { get; }

        public Replicator(Options options)
        {
            ChannelHub = options.ChannelHub;
            OwnsChannelHub = options.OwnsChannelHub;
            PublisherIdResolver = options.PublisherIdResolver;
            RetryPolicy = options.RetryPolicy;

            Replicas = new ConcurrentDictionary<Symbol, IReplica>();
            ChannelProcessorsById = new ConcurrentDictionary<Symbol, ReplicatorChannelProcessor>();
            ChannelProcessors = new ConcurrentDictionary<Channel<Message>, ReplicatorChannelProcessor>();
            
            OnChannelAttachedHandler = OnChannelAttached;
            OnChannelDetachedHandler = OnChannelDetachedAsync;
            ChannelHub.Detached += OnChannelDetachedHandler; // Must go first
            ChannelHub.Attached += OnChannelAttachedHandler;
        }

        public virtual IReplica<T> GetOrAdd<T>(Symbol publisherId, Symbol publicationId, 
            LTagged<Result<T>> initialOutput, bool isConsistent = true, bool requestUpdate = false)
        {
            var spinWait = new SpinWait();
            IReplica? replica;
            while (true) {
                while (!Replicas.TryGetValue(publicationId, out replica)) {
                    replica = new Replica<T>(this, publisherId, publicationId, initialOutput, isConsistent, requestUpdate);
                    if (Replicas.TryAdd(publicationId, replica)) {
                        if (TrySubscribe(replica, requestUpdate))
                            return (IReplica<T>) replica;
                        // No subscription = we can't keep it
                        Replicas.TryRemove(publicationId, replica);
                    }
                    spinWait.SpinOnce();
                }
                if (ChannelProcessorsById.TryGetValue(replica.PublisherId, out var _))
                    break;
                // Missing channel processor = subscription didn't happen,
                // so we can't return this replica (yet?)
                spinWait.SpinOnce();
            }
            return (IReplica<T>) replica;
        }

        public virtual IReplica? TryGet(Symbol publicationId)
        {
            if (!Replicas.TryGetValue(publicationId, out var replica))
                return null;
            if (!ChannelProcessorsById.TryGetValue(replica.PublisherId, out var _))
                return null;
            return replica;
        }

        protected virtual void OnChannelAttached(Channel<Message> channel)
        {
            var publisherId = PublisherIdResolver.Invoke(channel);
            var channelProcessor = CreateChannelProcessor(channel, publisherId);
            if (!ChannelProcessors.TryAdd(channel, channelProcessor))
                return;
            while (!ChannelProcessorsById.TryAdd(publisherId, channelProcessor)) {
                if (ChannelProcessorsById.TryRemove(publisherId, out var oldChannelProcessor)) {
                    // We intend to just start the task here
                    oldChannelProcessor.DisposeAsync();  
                }
            }
            channelProcessor.RunAsync().ContinueWith(_ => {
                // Since ChannelProcessor is AsyncProcessorBase desc.,
                // its disposal will shut down RunAsync as well,
                // so "subscribing" to RunAsync completion is the
                // same as subscribing to its disposal.
                ChannelProcessors.TryRemove(channel, channelProcessor);
            });
        }

        protected virtual void OnChannelDetachedAsync(
            Channel<Message> channel, ref Collector<ValueTask> taskCollector)
        {
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return;
            ChannelProcessorsById.TryRemove(channelProcessor.PublisherId, channelProcessor);
            taskCollector.Add(channelProcessor.DisposeAsync());
        }

        protected virtual ReplicatorChannelProcessor CreateChannelProcessor(
            Channel<Message> channel, Symbol publisherId) 
            => new ReplicatorChannelProcessor(this, channel, publisherId);

        bool IReplicatorImpl.TrySubscribe(IReplica replica, bool requestUpdate) 
            => TrySubscribe(replica, requestUpdate);
        protected virtual bool TrySubscribe(IReplica replica, bool requestUpdate)
        {
            if (replica.Replicator != this)
                throw new ArgumentOutOfRangeException(nameof(replica));
            if (!ChannelProcessorsById.TryGetValue(replica.PublisherId, out var channelProcessor))
                return false;
            // We intend to just start the task here
            channelProcessor.SubscribeAsync(replica, requestUpdate, default);
            return true;
        }

        void IReplicatorImpl.OnReplicaDisposed(IReplica replica) 
            => OnReplicaDisposed(replica);
        protected virtual void OnReplicaDisposed(IReplica replica)
        {
            if (replica.Replicator != this)
                throw new ArgumentOutOfRangeException(nameof(replica));
            Replicas.TryRemove(replica.PublicationId, replica);
        }

        void IReplicatorImpl.OnChannelProcessorDisposed(ReplicatorChannelProcessor channelProcessor) 
            => OnChannelProcessorDisposed(channelProcessor);
        protected virtual void OnChannelProcessorDisposed(ReplicatorChannelProcessor channelProcessor) 
            => ChannelProcessorsById.TryRemove(channelProcessor.PublisherId, channelProcessor);

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            ChannelHub.Attached -= OnChannelAttachedHandler; // Must go first
            ChannelHub.Detached -= OnChannelDetachedHandler;
            var channelProcessors = ChannelProcessors;
            while (!channelProcessors.IsEmpty) {
                var tasks = channelProcessors
                    .Take(HardwareInfo.ProcessorCount * 4)
                    .ToList()
                    .Select(p => {
                        var (_, channelProcessor) = (p.Key, p.Value);
                        return channelProcessor.DisposeAsync().AsTask();
                    });
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            await base.DisposeInternalAsync(disposing).ConfigureAwait(false);
        }
    }
}
