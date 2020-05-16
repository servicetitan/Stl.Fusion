using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Channels;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Bridge.Messages;
using Stl.OS;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public interface IReplicator
    {
        IChannelHub<PublicationMessage> ChannelHub { get; }
        bool OwnsChannelHub { get; }

        IReplica<T> TryAddOrGet<T>(Symbol publisherId, Symbol publicationId, TaggedResult<T> initialOutput);
        IReplica? TryGet(Symbol publicationId);
    }

    public interface IReplicatorImpl : IReplicator
    {
        void OnReproductionDisposed(IReplica replica);
    }

    public class Replicator : AsyncDisposableBase, IReplicatorImpl
    {
        private ConcurrentDictionary<Symbol, IReplica> Replicas { get; }
        protected ConcurrentDictionary<Channel<PublicationMessage>, ReplicatorChannelProcessor> ChannelProcessors { get; }
        protected Action<Channel<PublicationMessage>> OnChannelAttachedCached { get; } 
        protected Func<Channel<PublicationMessage>, ValueTask> OnChannelDetachedAsyncCached { get; } 

        public IChannelHub<PublicationMessage> ChannelHub { get; }
        public bool OwnsChannelHub { get; }

        public Replicator(IChannelHub<PublicationMessage> channelHub, bool ownsChannelHub = true)
        {
            ChannelHub = channelHub;
            OwnsChannelHub = ownsChannelHub;
            Replicas = new ConcurrentDictionary<Symbol, IReplica>();
            ChannelProcessors = new ConcurrentDictionary<Channel<PublicationMessage>, ReplicatorChannelProcessor>();
            
            OnChannelAttachedCached = OnChannelAttached;
            OnChannelDetachedAsyncCached = OnChannelDetachedAsync;
            ChannelHub.Detached += OnChannelDetachedAsyncCached; // Must go first
            ChannelHub.Attached += OnChannelAttachedCached;
        }

        public virtual IReplica<T> TryAddOrGet<T>(Symbol publisherId, Symbol publicationId, TaggedResult<T> initialOutput)
        {
            var spinWait = new SpinWait();
            IReplica? replica; 
            while (!Replicas.TryGetValue(publicationId, out replica)) {
                replica = new Replica<T>(this, publisherId, publicationId, initialOutput);
                if (Replicas.TryAdd(publicationId, replica))
                    break;
                spinWait.SpinOnce();
            }
            return (IReplica<T>) replica;
        }

        public virtual IReplica? TryGet(Symbol publicationId) 
            => Replicas.TryGetValue(publicationId, out var replica) ? replica : null;

        protected virtual void OnChannelAttached(Channel<PublicationMessage> channel)
        {
            var channelProcessor = CreateChannelProcessor(channel);
            if (!ChannelProcessors.TryAdd(channel, channelProcessor))
                return;
            channelProcessor.RunAsync().ContinueWith(_ => {
                // Since ChannelProcessor is AsyncProcessorBase desc.,
                // its disposal will shut down RunAsync as well,
                // so "subscribing" to RunAsync completion is the
                // same as subscribing to its disposal.
                ChannelProcessors.TryRemove(channel, channelProcessor);
            });
        }

        protected virtual ValueTask OnChannelDetachedAsync(Channel<PublicationMessage> channel)
        {
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return ValueTaskEx.CompletedTask;
            return channelProcessor.DisposeAsync();
        }

        protected virtual ReplicatorChannelProcessor CreateChannelProcessor(Channel<PublicationMessage> channel) 
            => new ReplicatorChannelProcessor(channel, this);

        void IReplicatorImpl.OnReproductionDisposed(IReplica replica) 
            => OnReproductionDisposed(replica);
        protected virtual void OnReproductionDisposed(IReplica replica)
        {
            if (replica.Replicator != this)
                throw new ArgumentOutOfRangeException(nameof(replica));
            Replicas.TryRemove(replica.PublicationId, replica);
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            ChannelHub.Attached -= OnChannelAttachedCached; // Must go first
            ChannelHub.Detached -= OnChannelDetachedAsyncCached;
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
