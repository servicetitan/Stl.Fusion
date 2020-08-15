using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Channels;
using Stl.Collections;
using Stl.Concurrency;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Internal;
using Stl.OS;
using Stl.Generators;
using Stl.Text;
using Stl.Time;

namespace Stl.Fusion.Bridge
{
    public interface IPublisher : IHasId<Symbol>
    {
        IChannelHub<Message> ChannelHub { get; }
        bool OwnsChannelHub { get; }

        IPublication Publish(IComputed computed);
        IPublication? TryGet(Symbol publicationId);
        ValueTask SubscribeAsync(
            Channel<Message> channel, IPublication publication,
            bool isUpdateRequested, CancellationToken cancellationToken = default);
        ValueTask UnsubscribeAsync(
            Channel<Message> channel, IPublication publication,
            CancellationToken cancellationToken = default);
    }

    public interface IPublisherImpl : IPublisher
    {
        IPublicationFactory PublicationFactory { get; }
        ISubscriptionProcessorFactory SubscriptionProcessorFactory { get; }
        Type PublicationGeneric { get; }
        Type SubscriptionProcessorGeneric { get; }
        TimeSpan PublicationExpirationTime { get; }
        TimeSpan SubscriptionExpirationTime { get; }
        Generator<Symbol> PublicationIdGenerator { get; }
        IMomentClock Clock { get; }

        void OnPublicationDisposed(IPublication publication);
        void OnChannelProcessorDisposed(PublisherChannelProcessor channelProcessor);
    }

    public class Publisher : AsyncDisposableBase, IPublisherImpl
    {
        public class Options
        {
            public static Symbol NewId() => "P-" + RandomStringGenerator.Default.Next();

            public Symbol Id { get; set; } = NewId();
            public IChannelHub<Message> ChannelHub { get; set; } = new ChannelHub<Message>();
            public bool OwnsChannelHub { get; set; } = true;
            public IPublicationFactory PublicationFactory { get; set; } = Internal.PublicationFactory.Instance;
            public Type PublicationGeneric { get; set; } = typeof(Publication<>);
            public TimeSpan PublicationExpirationTime { get; set; } = TimeSpan.FromSeconds(60);
            public Generator<Symbol> PublicationIdGenerator { get; set; } = new RandomSymbolGenerator("p-");
            public ISubscriptionProcessorFactory SubscriptionProcessorFactory { get; set; } = Internal.SubscriptionProcessorFactory.Instance;
            public Type SubscriptionProcessorGeneric { get; set; } = typeof(SubscriptionProcessor<>);
            public TimeSpan SubscriptionExpirationTime { get; set; } = TimeSpan.FromSeconds(60);
            public IMomentClock Clock { get; set; } = CoarseCpuClock.Instance;
        }

        protected ConcurrentDictionary<ComputedInput, IPublication> Publications { get; }
        protected ConcurrentDictionary<Symbol, IPublication> PublicationsById { get; }
        protected ConcurrentDictionary<Channel<Message>, PublisherChannelProcessor> ChannelProcessors { get; }
        protected ChannelAttachedHandler<Message> OnChannelAttachedHandler { get; }
        protected ChannelDetachedHandler<Message> OnChannelDetachedHandler { get; }

        public Symbol Id { get; }
        public IChannelHub<Message> ChannelHub { get; }
        public bool OwnsChannelHub { get; }
        public IPublicationFactory PublicationFactory { get; }
        public Type PublicationGeneric { get; }
        public TimeSpan PublicationExpirationTime { get; }
        public Generator<Symbol> PublicationIdGenerator { get; }
        public ISubscriptionProcessorFactory SubscriptionProcessorFactory { get; }
        public Type SubscriptionProcessorGeneric { get; }
        public TimeSpan SubscriptionExpirationTime { get; }
        public IMomentClock Clock { get; }

        public Publisher(Options options)
        {
            Id = options.Id;
            ChannelHub = options.ChannelHub;
            OwnsChannelHub = options.OwnsChannelHub;

            PublicationFactory = options.PublicationFactory;
            PublicationGeneric = options.PublicationGeneric;
            PublicationExpirationTime = options.PublicationExpirationTime;
            PublicationIdGenerator = options.PublicationIdGenerator;
            SubscriptionProcessorFactory = options.SubscriptionProcessorFactory;
            SubscriptionProcessorGeneric = options.SubscriptionProcessorGeneric;
            SubscriptionExpirationTime = options.SubscriptionExpirationTime;
            Clock = options.Clock;

            var concurrencyLevel = HardwareInfo.ProcessorCount << 2;
            var capacity = 7919;
            Publications = new ConcurrentDictionary<ComputedInput, IPublication>(concurrencyLevel, capacity);
            PublicationsById = new ConcurrentDictionary<Symbol, IPublication>(concurrencyLevel, capacity);
            ChannelProcessors = new ConcurrentDictionary<Channel<Message>, PublisherChannelProcessor>(concurrencyLevel, capacity);

            OnChannelAttachedHandler = OnChannelAttached;
            OnChannelDetachedHandler = OnChannelDetachedAsync;
            ChannelHub.Detached += OnChannelDetachedHandler; // Must go first
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
                         var p1 = this1.PublicationFactory.Create(this1.PublicationGeneric, this1, computed1, id, Clock);
                         this1.PublicationsById[id] = p1;
                         p1.RunAsync();
                         return p1;
                     }, (this, computed));
                if (p.Touch())
                    return p;
                spinWait.SpinOnce();
            }
        }

        public virtual IPublication? TryGet(Symbol publicationId)
            => PublicationsById.TryGetValue(publicationId, out var p) ? p : null;

        public virtual ValueTask SubscribeAsync(
            Channel<Message> channel, IPublication publication,
            bool isUpdateRequested, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposedOrDisposing();
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                throw Errors.UnknownChannel(channel);
            if (publication.Publisher != this)
                throw Errors.WrongPublisher(this, publication.Publisher.Id);
            var message = new SubscribeMessage() {
                PublisherId = Id,
                PublicationId = publication.Id,
                IsUpdateRequested = isUpdateRequested,
            };
            return channelProcessor.OnReplicaMessageAsync(message, cancellationToken);
        }

        public virtual ValueTask UnsubscribeAsync(
            Channel<Message> channel, IPublication publication,
            CancellationToken cancellationToken = default)
        {
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return ValueTaskEx.CompletedTask;
            return channelProcessor.UnsubscribeAsync(publication, cancellationToken);
        }

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

        // Channel-related

        protected virtual PublisherChannelProcessor CreateChannelProcessor(Channel<Message> channel)
            => new PublisherChannelProcessor(this, channel);

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

        protected virtual void OnChannelDetachedAsync(
            Channel<Message> channel, ref Collector<ValueTask> taskCollector)
        {
            if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
                return;
            taskCollector.Add(channelProcessor.DisposeAsync());
        }

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
