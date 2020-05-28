using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Bridge.Internal;
using Stl.OS;
using Stl.Reflection;
using Stl.Security;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public interface IReplicator
    {
        Symbol Id { get; }

        IReplica? TryGet(Symbol publicationId);
        IReplica<T> GetOrAdd<T>(Symbol publisherId, Symbol publicationId, 
            LTagged<Result<T>> initialOutput, bool isConsistent = true, bool requestUpdate = false);

        IComputed<bool> GetPublisherConnectionState(Symbol publisherId);
    }

    public interface IReplicatorImpl : IReplicator
    {
        IComputeRetryPolicy RetryPolicy { get; }
        IChannelProvider ChannelProvider { get; }
        TimeSpan ReconnectDelay { get; }

        void Subscribe(IReplica replica);
        void OnReplicaDisposed(IReplica replica);
    }

    public class Replicator : AsyncDisposableBase, IReplicatorImpl
    {
        public class Options
        {
            public static Symbol NewId() => "R-" + RandomStringGenerator.Default.Next();

            public Symbol Id { get; set; } = NewId();
            public IReplicaRegistry Registry { get; set; } = new ReplicaRegistry();
            public IComputeRetryPolicy RetryPolicy { get; set; } = ComputeRetryPolicy.Default;
            public TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
        }

        protected IReplicaRegistry Registry { get; }
        protected ConcurrentDictionary<Symbol, ReplicatorChannelProcessor> ChannelProcessors { get; }
        protected Func<Symbol, ReplicatorChannelProcessor> CreateChannelProcessorHandler { get; }
        public Symbol Id { get; }
        public IChannelProvider ChannelProvider { get; }
        public IComputeRetryPolicy RetryPolicy { get; }
        public TimeSpan ReconnectDelay { get; }

        public Replicator(Options options, IChannelProvider channelProvider)
        {
            Id = options.Id;
            Registry = options.Registry;
            RetryPolicy = options.RetryPolicy;
            ReconnectDelay = options.ReconnectDelay;
            ChannelProvider = channelProvider;
            ChannelProcessors = new ConcurrentDictionary<Symbol, ReplicatorChannelProcessor>();
            CreateChannelProcessorHandler = CreateChannelProcessor;
        }

        public virtual IReplica? TryGet(Symbol publicationId) 
            => Registry.TryGet(publicationId);

        public virtual IReplica<T> GetOrAdd<T>(Symbol publisherId, Symbol publicationId, 
            LTagged<Result<T>> initialOutput, bool isConsistent = true, bool requestUpdate = false)
        {
            var newReplica = new Replica<T>(
                this, publisherId, publicationId, initialOutput, isConsistent, requestUpdate);
            var replica = Registry.GetOrAdd(newReplica);
            if (replica == newReplica)
                Subscribe(replica);
            return (IReplica<T>) replica;
        }

        public IComputed<bool> GetPublisherConnectionState(Symbol publisherId) 
            => ChannelProcessors
                .GetOrAddChecked(publisherId, CreateChannelProcessorHandler)
                .StateComputedRef.Computed;

        protected virtual ReplicatorChannelProcessor GetChannelProcessor(Symbol publisherId) 
            => ChannelProcessors
                .GetOrAddChecked(publisherId, CreateChannelProcessorHandler);

        protected virtual ReplicatorChannelProcessor CreateChannelProcessor(Symbol publisherId)
        {
            var channelProcessor = new ReplicatorChannelProcessor(this, publisherId);
            channelProcessor.RunAsync().ContinueWith(_ => {
                // Since ChannelProcessor is AsyncProcessorBase desc.,
                // its disposal will shut down RunAsync as well,
                // so "subscribing" to RunAsync completion is the
                // same as subscribing to its disposal.
                ChannelProcessors.TryRemove(publisherId, channelProcessor);
            });
            return channelProcessor;
        }

        void IReplicatorImpl.Subscribe(IReplica replica) 
            => Subscribe(replica);
        protected virtual void Subscribe(IReplica replica)
        {
            if (replica.Replicator != this)
                throw new ArgumentOutOfRangeException(nameof(replica));
            GetChannelProcessor(replica.PublisherId).Subscribe(replica);
        }

        void IReplicatorImpl.OnReplicaDisposed(IReplica replica) 
            => OnReplicaDisposed(replica);
        protected virtual void OnReplicaDisposed(IReplica replica)
        {
            if (replica.Replicator != this)
                throw new ArgumentOutOfRangeException(nameof(replica));
            GetChannelProcessor(replica.PublisherId).Unsubscribe(replica);
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
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
