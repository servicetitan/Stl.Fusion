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
using Stl.Reflection;
using Stl.Security;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public interface IPublisher
    {
        Symbol Id { get; }
        IChannelHub<PublicationMessage> ChannelHub { get; }
        bool OwnsChannelHub { get; }

        IPublication Publish(IComputed computed);
        IPublication? TryGet(Symbol publicationId);
        bool Subscribe(Channel<PublicationMessage> channel, IPublication publication, bool sendUpdate);
        ValueTask<bool> UnsubscribeAsync(Channel<PublicationMessage> channel, IPublication publication);
    }

    public interface IPublisherImpl : IPublisher
    {
        bool Subscribe(Channel<PublicationMessage> channel, IPublication publication, SubscribeMessage subscribeMessage);
        void OnPublicationDisposed(IPublication publication);
        void OnChannelProcessorDisposed(PublisherChannelProcessor channelProcessor);
    }

    public class Publisher : AsyncDisposableBase, IPublisherImpl
    {
        protected ConcurrentDictionary<ComputedInput, IPublication> Publications { get; } 
        protected ConcurrentDictionary<Symbol, IPublication> PublicationsById { get; }
        protected ConcurrentDictionary<Channel<PublicationMessage>, PublisherChannelProcessor> ChannelProcessors { get; }
        protected IGenerator<Symbol> PublicationIdGenerator { get; }
        protected Action<Channel<PublicationMessage>> OnChannelAttachedHandler { get; } 
        protected Func<Channel<PublicationMessage>, ValueTask> OnChannelDetachedAsyncHandler { get; }
        protected IPublicationFactory PublicationFactory { get; }
        protected Type PublicationType { get; }

        public Symbol Id { get; }
        public IChannelHub<PublicationMessage> ChannelHub { get; }
        public bool OwnsChannelHub { get; }

        public Publisher(Symbol id, 
            IChannelHub<PublicationMessage> channelHub,
            IGenerator<Symbol> publicationIdGenerator,
            Type? publicationType = null,
            bool ownsChannelHub = true)
        {
            publicationType ??= typeof(Publication<>);
            Id = id;
            ChannelHub = channelHub;
            OwnsChannelHub = ownsChannelHub;
            PublicationIdGenerator = publicationIdGenerator;
            PublicationFactory = Internal.PublicationFactory.Instance;
            PublicationType = publicationType;

            var concurrencyLevel = HardwareInfo.ProcessorCount << 2;
            var capacity = 7919;
            Publications = new ConcurrentDictionary<ComputedInput, IPublication>(concurrencyLevel, capacity);
            PublicationsById = new ConcurrentDictionary<Symbol, IPublication>(concurrencyLevel, capacity);
            ChannelProcessors = new ConcurrentDictionary<Channel<PublicationMessage>, PublisherChannelProcessor>(concurrencyLevel, capacity);

            OnChannelAttachedHandler = OnChannelAttached;
            OnChannelDetachedAsyncHandler = OnChannelDetachedAsync;
            ChannelHub.Detached += OnChannelDetachedAsyncHandler; // Must go first
            ChannelHub.Attached += OnChannelAttachedHandler;
        }

        public virtual IPublication Publish(IComputed computed)
        {
            ThrowIfDisposedOrDisposing();
            var spinWait = new SpinWait();
            while (true) {
                 var p = Publications.GetOrAddChecked(
                     computed.Input,
                     (key, arg) => {
                         var (this1, computed1) = arg;
                         var id = this1.PublicationIdGenerator.Next();
                         var p1 = this1.PublicationFactory.Create(this1.PublicationType, this1, computed1, id);
                         this1.PublicationsById[id] = p1;
                         ((IPublicationImpl) p1).RunAsync();
                         return p1;
                     }, (this, computed));
                if (p.Touch())
                    return p;
                spinWait.SpinOnce();
            }
        }

        public virtual IPublication? TryGet(Symbol publicationId) 
            => PublicationsById.TryGetValue(publicationId, out var p) ? p : null;

        void IPublisherImpl.OnPublicationDisposed(IPublication publication) 
            => OnPublicationDisposed(publication);
        protected virtual void OnPublicationDisposed(IPublication publication)
        {
            if (publication.Publisher != this)
                throw new ArgumentOutOfRangeException(nameof(publication));
            if (!PublicationsById.TryGetValue(publication.Id, out var p))
                return;
            Publications.TryRemove(publication.State.Computed.Input, p);
            PublicationsById.TryRemove(p.Id, p);
        }

        void IPublisherImpl.OnChannelProcessorDisposed(PublisherChannelProcessor channelProcessor) 
            => OnChannelProcessorDisposed(channelProcessor);
        protected virtual void OnChannelProcessorDisposed(PublisherChannelProcessor channelProcessor)
        { }

        public bool Subscribe(Channel<PublicationMessage> channel, IPublication publication, bool sendUpdate)
        {
            var message = new SubscribeMessage() {
                PublisherId = Id,
                PublicationId = Id,
                ReplicaLTag = LTag.Default,
                ReplicaIsConsistent = false,
                IsUpdateRequested = sendUpdate,
            };
            return Subscribe(channel, publication, message);
        }

        bool IPublisherImpl.Subscribe(Channel<PublicationMessage> channel, IPublication publication, SubscribeMessage subscribeMessage) 
            => Subscribe(channel, publication, subscribeMessage);
        protected virtual bool Subscribe(Channel<PublicationMessage> channel, IPublication publication, SubscribeMessage subscribeMessage)
        {
            ThrowIfDisposedOrDisposing();
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return false;
            if (publication.Publisher != this || publication.State.IsDisposed)
                return false;
            return channelProcessor.Subscribe(publication, subscribeMessage);
        }

        public virtual ValueTask<bool> UnsubscribeAsync(Channel<PublicationMessage> channel, IPublication publication)
        {
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return ValueTaskEx.FalseTask;
            return channelProcessor.UnsubscribeAsync(publication);
        }

        // Channel-related

        protected virtual PublisherChannelProcessor CreateChannelProcessor(Channel<PublicationMessage> channel) 
            => new PublisherChannelProcessor(this, channel);

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

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            ChannelHub.Attached -= OnChannelAttachedHandler; // Must go first
            ChannelHub.Detached -= OnChannelDetachedAsyncHandler;
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
            var publications = PublicationsById;
            while (!publications.IsEmpty) {
                var tasks = publications
                    .Take(HardwareInfo.ProcessorCount * 4)
                    .ToList()
                    .Select(p => {
                        var (_, publication) = (p.Key, p.Value);
                        return publication.DisposeAsync().AsTask();
                    });
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            if (OwnsChannelHub)
                await ChannelHub.DisposeAsync().ConfigureAwait(false);
            await base.DisposeInternalAsync(disposing).ConfigureAwait(false);
        }
    }
}
