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

        IPublication Publish(IComputed computed, Type? publicationType = null);
        IPublication? TryGet(Symbol publicationId);
        bool Subscribe(Channel<PublicationMessage> channel, IPublication publication, bool notify);
        ValueTask<bool> UnsubscribeAsync(Channel<PublicationMessage> channel, IPublication publication);
    }

    public interface IPublisherImpl : IPublisher
    {
        void OnPublicationDisposed(IPublication publication);
    }

    public class Publisher : AsyncDisposableBase, IPublisherImpl
    {
        protected ConcurrentDictionary<(ComputedInput Input, Type PublicationType), IPublication> Publications { get; } 
        protected ConcurrentDictionary<Symbol, IPublication> PublicationsById { get; }
        protected ConcurrentDictionary<Channel<PublicationMessage>, PublisherChannelProcessor> ChannelProcessors { get; }
        protected IGenerator<Symbol> PublicationIdGenerator { get; }
        protected Action<Channel<PublicationMessage>> OnChannelAttachedCached { get; } 
        protected Func<Channel<PublicationMessage>, ValueTask> OnChannelDetachedAsyncCached { get; } 

        public Symbol Id { get; }
        public IChannelHub<PublicationMessage> ChannelHub { get; }
        public bool OwnsChannelHub { get; }
        public IPublicationFactory PublicationFactory { get; }
        public Type DefaultPublicationType { get; }

        public Publisher(Symbol id, 
            IChannelHub<PublicationMessage> channelHub,
            IGenerator<Symbol> publicationIdGenerator,
            IPublicationFactory? publicationFactory = null,
            Type? defaultPublicationType = null,
            bool ownsChannelHub = true)
        {
            publicationFactory ??= Internal.PublicationFactory.Instance;
            defaultPublicationType ??= typeof(Publication<>);
            Id = id;
            ChannelHub = channelHub;
            OwnsChannelHub = ownsChannelHub;
            PublicationIdGenerator = publicationIdGenerator;
            PublicationFactory = publicationFactory;
            DefaultPublicationType = defaultPublicationType;

            var concurrencyLevel = HardwareInfo.ProcessorCount << 2;
            var capacity = 7919;
            Publications = new ConcurrentDictionary<(ComputedInput, Type), IPublication>(concurrencyLevel, capacity);
            PublicationsById = new ConcurrentDictionary<Symbol, IPublication>(concurrencyLevel, capacity);
            ChannelProcessors = new ConcurrentDictionary<Channel<PublicationMessage>, PublisherChannelProcessor>(concurrencyLevel, capacity);

            OnChannelAttachedCached = OnChannelAttached;
            OnChannelDetachedAsyncCached = OnChannelDetachedAsync;
            ChannelHub.Detached += OnChannelDetachedAsyncCached; // Must go first
            ChannelHub.Attached += OnChannelAttachedCached;
        }

        public virtual IPublication Publish(IComputed computed, Type? publicationType = null)
        {
            ThrowIfDisposedOrDisposing();
            publicationType ??= DefaultPublicationType;
            var spinWait = new SpinWait();
            while (true) {
                 var p = Publications.GetOrAddChecked(
                    (computed.Input, PublicationType: publicationType), 
                    (key, arg) => {
                        var (this1, computed1) = arg;
                        var publicationType1 = key.PublicationType;
                        var id = this1.PublicationIdGenerator.Next();
                        var p1 = this1.PublicationFactory.Create(publicationType1, this1, computed1, id);
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
            Publications.TryRemove((p.Computed.Input, p.PublicationType), p);
            PublicationsById.TryRemove(p.Id, p);
        }

        public bool Subscribe(Channel<PublicationMessage> channel, IPublication publication, bool notify)
        {
            ThrowIfDisposedOrDisposing();
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return false;
            if (publication.Publisher != this || publication.State == PublicationState.Disposed)
                return false;
            return channelProcessor.Subscribe(publication, notify);
        }

        public ValueTask<bool> UnsubscribeAsync(Channel<PublicationMessage> channel, IPublication publication)
        {
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return ValueTaskEx.FalseTask;
            return channelProcessor.UnsubscribeAsync(publication);
        }

        // Channel-related

        protected virtual PublisherChannelProcessor CreateChannelProcessor(Channel<PublicationMessage> channel) 
            => new PublisherChannelProcessor(channel, this);

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
