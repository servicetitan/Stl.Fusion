using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Fusion.Bridge.Messages;
using Stl.OS;
using Stl.Text;

namespace Stl.Fusion.Bridge.Internal
{
    public class PublisherChannelProcessor : AsyncProcessBase
    {
        protected readonly ILoggerFactory LoggerFactory;
        protected readonly ILogger Log;
        protected readonly IPublisherImpl PublisherImpl;
        protected readonly ConcurrentDictionary<Symbol, SubscriptionProcessor> Subscriptions;
        protected object Lock => Subscriptions;

        public readonly IPublisher Publisher;
        public readonly Channel<BridgeMessage> Channel;

        public PublisherChannelProcessor(IPublisher publisher, Channel<BridgeMessage> channel,
            ILoggerFactory? loggerFactory = null)
        {
            LoggerFactory = loggerFactory ??= NullLoggerFactory.Instance;
            Log = LoggerFactory.CreateLogger(GetType());
            Publisher = publisher;
            PublisherImpl = (IPublisherImpl) publisher;
            Channel = channel;
            Subscriptions = new ConcurrentDictionary<Symbol, SubscriptionProcessor>();
        }

        protected override async Task RunInternal(CancellationToken cancellationToken)
        {
            try {
                var reader = Channel.Reader;
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!reader.TryRead(out var message))
                        continue;
                    switch (message) {
                    case ReplicaMessage rm:
                        await OnReplicaMessage(rm, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        await OnUnsupportedMessage(message, cancellationToken).ConfigureAwait(false);
                        break;
                    }
                }
            }
            finally {
                // Awaiting for disposal here = cyclic task dependency;
                // we should just ensure it starts right when this method
                // completes.
                var _ = DisposeAsync();
            }
        }

        protected virtual async ValueTask OnUnsupportedMessage(BridgeMessage message, CancellationToken cancellationToken)
        {
            if (message is ReplicaMessage rm) {
                var response = new PublicationAbsentsMessage() {
                    PublisherId = rm.PublisherId,
                    PublicationId = rm.PublicationId,
                };
                await Channel.Writer
                    .WriteAsync(response, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public virtual async ValueTask OnReplicaMessage(ReplicaMessage message, CancellationToken cancellationToken)
        {
            if (message.PublisherId != Publisher.Id) {
                await OnUnsupportedMessage(message, cancellationToken).ConfigureAwait(false);
                return;
            }
            var publicationId = message.PublicationId;
            var publication = Publisher.TryGet(publicationId);
            if (publication == null) {
                await OnUnsupportedMessage(message, cancellationToken).ConfigureAwait(false);
                return;
            }
            if (Subscriptions.TryGetValue(publicationId, out var subscriptionProcessor))
                goto subscriptionExists;
            lock (Lock) {
                // Double check locking
                if (Subscriptions.TryGetValue(publicationId, out subscriptionProcessor))
                    goto subscriptionExists;

                subscriptionProcessor = PublisherImpl.SubscriptionProcessorFactory.Create(
                    PublisherImpl.SubscriptionProcessorGeneric,
                    publication, Channel, PublisherImpl.SubscriptionExpirationTime,
                    PublisherImpl.Clock, LoggerFactory);
                Subscriptions[publicationId] = subscriptionProcessor;
            }
            subscriptionProcessor.Run()
                .ContinueWith(_ => Unsubscribe(publication, default), CancellationToken.None)
                .Ignore();
        subscriptionExists:
            await subscriptionProcessor.IncomingChannel.Writer
                .WriteAsync(message, cancellationToken).ConfigureAwait(false);
        }

        public virtual async ValueTask Unsubscribe(
            IPublication publication, CancellationToken cancellationToken)
        {
            var publicationId = publication.Id;
            if (!Subscriptions.TryRemove(publicationId, out var subscriptionProcessor))
                return;
            await subscriptionProcessor.DisposeAsync().ConfigureAwait(false);
        }

        protected virtual async Task RemoveSubscriptions()
        {
            // We can unsubscribe in parallel
            var subscriptions = Subscriptions;
            for (var i = 0; i < 2; i++) {
                while (!subscriptions.IsEmpty) {
                    var tasks = subscriptions
                        .Take(HardwareInfo.GetProcessorCountFactor(4, 4))
                        .ToList()
                        .Select(p => Task.Run(async () => {
                            var (publicationId, _) = (p.Key, p.Value);
                            var publication = Publisher.TryGet(publicationId);
                            if (publication != null)
                                await Unsubscribe(publication, default).ConfigureAwait(false);
                        }));
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                // We repeat this twice in case some new subscriptions
                // were still in process while we were unsubscribing.
                // Since we don't know for sure how long it might take,
                // we optimistically assume 10 seconds is enough for this.
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        protected override async ValueTask DisposeInternal(bool disposing)
        {
            await base.DisposeInternal(disposing).ConfigureAwait(false);
            await RemoveSubscriptions().ConfigureAwait(false);
            PublisherImpl.OnChannelProcessorDisposed(this);
        }
    }
}
