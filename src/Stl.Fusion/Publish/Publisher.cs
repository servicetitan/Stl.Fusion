using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Channels;
using Stl.Fusion.Publish.Internal;
using Stl.Fusion.Publish.Messages;
using Stl.OS;
using Stl.Reflection;
using Stl.Security;
using Stl.Text;

namespace Stl.Fusion.Publish
{
    public interface IPublisher
    {
        Symbol Id { get; }
        IPublication Publish(IComputed computed, PublicationFactory? publicationFactory = null);
        IPublication? TryGet(Symbol publicationId);
    }

    public interface IPublisherImpl : IPublisher
    {
        IChannelHub<Message> ChannelHub { get; }
        bool Subscribe(Channel<Message> channel, IPublication publication, bool notify);
        ValueTask<bool> UnsubscribeAsync(Channel<Message> channel, IPublication publication);
        void OnPublicationDisposed(IPublication publication);
    }

    public abstract class PublisherBase : AsyncDisposableBase, IPublisherImpl
    {
        protected ConcurrentDictionary<(ComputedInput Input, PublicationFactory Factory), PublicationInfo> Publications { get; } 
        protected ConcurrentDictionary<Symbol, PublicationInfo> PublicationsById { get; }
        protected ConcurrentDictionary<Channel<Message>, ChannelProcessor> ChannelProcessors { get; }
        protected IGenerator<Symbol> PublicationIdGenerator { get; }
        protected bool OwnsChannelRegistry { get; }
        protected Action<Channel<Message>> OnChannelAttachedCached { get; } 
        protected Func<Channel<Message>, ValueTask> OnChannelDetachedCached { get; } 

        public Symbol Id { get; }
        public IChannelHub<Message> ChannelHub { get; }
        public PublicationFactory DefaultPublicationFactory { get; }

        protected PublisherBase(Symbol id, 
            IChannelHub<Message> channelHub,
            IGenerator<Symbol> publicationIdGenerator,
            bool ownsChannelRegistry = true,
            PublicationFactory? defaultPublicationFactory = null)
        {
            defaultPublicationFactory ??= PublicationFactoryEx.Updating;
            Id = id;
            ChannelHub = channelHub;
            OwnsChannelRegistry = ownsChannelRegistry;
            PublicationIdGenerator = publicationIdGenerator;
            DefaultPublicationFactory = defaultPublicationFactory;
            OnChannelAttachedCached = OnChannelAttached;
            OnChannelDetachedCached = OnChannelDetachedAsync;

            var concurrencyLevel = HardwareInfo.ProcessorCount << 2;
            var capacity = 7919;
            Publications = new ConcurrentDictionary<(ComputedInput Input, PublicationFactory Factory), PublicationInfo>(concurrencyLevel, capacity);
            PublicationsById = new ConcurrentDictionary<Symbol, PublicationInfo>(concurrencyLevel, capacity);
            ChannelProcessors = new ConcurrentDictionary<Channel<Message>, ChannelProcessor>(concurrencyLevel, capacity);
            ChannelHub.Attached += OnChannelAttachedCached;
            ChannelHub.Detached += OnChannelDetachedCached;
        }

        public virtual IPublication Publish(IComputed computed, PublicationFactory? publicationFactory = null)
        {
            ThrowIfDisposedOrDisposing();
            publicationFactory ??= DefaultPublicationFactory;
            var spinWait = new SpinWait();
            while (true) {
                 var pInfo = Publications.GetOrAddChecked(
                    (computed.Input, Factory: publicationFactory), 
                    (key, arg) => {
                        var (this1, computed1) = arg;
                        var publicationId = this1.PublicationIdGenerator.Next();
                        var publication1 = key.Factory.Invoke(this1, computed1, publicationId);
                        var pInfo1 = new PublicationInfo(publication1, key.Factory);
                        this1.PublicationsById[publicationId] = pInfo1;
                        pInfo1.PublicationImpl.RunAsync();
                        return pInfo1;
                    }, (this, computed));
                var publication = pInfo.Publication;
                if (publication.Touch())
                    return publication;
                spinWait.SpinOnce();
            }
        }

        public virtual IPublication? TryGet(Symbol publicationId) 
            => PublicationsById.TryGetValue(publicationId, out var info) ? info.Publication : null;

        void IPublisherImpl.OnPublicationDisposed(IPublication publication) 
            => OnPublicationDisposed(publication);
        protected virtual void OnPublicationDisposed(IPublication publication)
        {
            if (publication.Publisher != this)
                throw new ArgumentOutOfRangeException(nameof(publication));
            if (!PublicationsById.TryGetValue(publication.Id, out var publicationInfo))
                return;
            Publications.TryRemove((publicationInfo.Input, publicationInfo.Factory), publicationInfo);
            PublicationsById.TryRemove(publicationInfo.Id, publicationInfo);
        }


        // Channel-related

        protected virtual ChannelProcessor CreateChannelProcessor(Channel<Message> channel) 
            => new ChannelProcessor(channel, this);

        protected virtual void OnChannelAttached(Channel<Message> channel)
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

        protected virtual ValueTask OnChannelDetachedAsync(Channel<Message> channel)
        {
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return ValueTaskEx.CompletedTask;
            return channelProcessor.DisposeAsync();
        }

        bool IPublisherImpl.Subscribe(Channel<Message> channel, IPublication publication, bool notify) 
            => Subscribe(channel, publication, notify);
        protected bool Subscribe(Channel<Message> channel, IPublication publication, bool notify)
        {
            ThrowIfDisposedOrDisposing();
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return false;
            if (publication.Publisher != this || publication.State == PublicationState.Unpublished)
                return false;
            return channelProcessor.Subscribe(publication, notify);
        }

        ValueTask<bool> IPublisherImpl.UnsubscribeAsync(Channel<Message> channel, IPublication publication) 
            => UnsubscribeAsync(channel, publication);
        protected ValueTask<bool> UnsubscribeAsync(Channel<Message> channel, IPublication publication)
        {
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return ValueTaskEx.FalseTask;
            return channelProcessor.UnsubscribeAsync(publication);
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            ChannelHub.Attached -= OnChannelAttachedCached;
            var publications = PublicationsById;
            while (!publications.IsEmpty) {
                var tasks = publications
                    .Take(HardwareInfo.ProcessorCount * 4)
                    .ToList()
                    .Select(p => Task.Run(async () => {
                        var (_, publicationInfo) = (p.Key, p.Value);
                        await publicationInfo.Publication.DisposeAsync().ConfigureAwait(false);
                    }));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            if (OwnsChannelRegistry)
                await ChannelHub.DisposeAsync().ConfigureAwait(false);
            await base.DisposeInternalAsync(disposing).ConfigureAwait(false);
        }
    }
}
