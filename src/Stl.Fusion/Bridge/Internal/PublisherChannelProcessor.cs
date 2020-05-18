using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Bridge.Messages;
using Stl.OS;
using Stl.Text;

namespace Stl.Fusion.Bridge.Internal
{
    public class PublisherChannelProcessor : AsyncProcessBase
    {
        public readonly IPublisher Publisher;
        public readonly IPublisherImpl PublisherImpl;
        public readonly Channel<PublicationMessage> Channel;
        public readonly ConcurrentDictionary<Symbol, SubscriptionProcessor> Subscriptions;
        protected object Lock => Subscriptions;  

        public PublisherChannelProcessor(IPublisher publisher, Channel<PublicationMessage> channel)
        {
            Publisher = publisher;
            PublisherImpl = (IPublisherImpl) publisher;
            Channel = channel;
            Subscriptions = new ConcurrentDictionary<Symbol, SubscriptionProcessor>();
        }

        protected override async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            try {
                var reader = Channel.Reader;
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!reader.TryRead(out var message))
                        continue;
                    await OnMessageAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                await DisposeAsync().ConfigureAwait(false);
            }
        }

        protected virtual Task OnMessageAsync(PublicationMessage message, CancellationToken cancellationToken)
        {
            switch (message) {
            case SubscribeMessage sm:
                if (sm.PublisherId != Publisher.Id)
                    break;
                var publication = Publisher.TryGet(sm.PublicationId);
                if (publication == null)
                    break;
                PublisherImpl.Subscribe(Channel, publication, sm);
                break;
            case UnsubscribeMessage um:
                if (um.PublisherId != Publisher.Id)
                    break;
                publication = Publisher.TryGet(um.PublicationId);
                if (publication == null)
                    break;
                var _ = PublisherImpl.UnsubscribeAsync(Channel, publication);
                break;
            }
            return Task.CompletedTask;
        }

        public virtual bool Subscribe(IPublication publication, SubscribeMessage subscribeMessage)
        {
            var publicationId = publication.Id;
            if (Subscriptions.TryGetValue(publicationId, out _))
                return false;
            SubscriptionProcessor subscriptionProcessor;
            lock (Lock) {
                // Double check locking
                if (Subscriptions.TryGetValue(publicationId, out var _))
                    return false;
                var publicationImpl = (IPublicationImpl) publication;
                subscriptionProcessor = publicationImpl.CreateSubscriptionProcessor(Channel, subscribeMessage);
                Subscriptions[publicationId] = subscriptionProcessor;
            }
            subscriptionProcessor.RunAsync()
                .ContinueWith(_ => UnsubscribeAsync(publication), CancellationToken.None);
            return true;
        }

        public virtual async ValueTask<bool> UnsubscribeAsync(IPublication publication)
        {
            var publicationId = publication.Id;
            if (!Subscriptions.TryRemove(publicationId, out var subscriptionProcessor))
                return false;
            await subscriptionProcessor.DisposeAsync().ConfigureAwait(false);
            return true;
        }

        protected virtual async Task RemoveSubscriptionsAsync()
        {
            // We can unsubscribe in parallel
            var subscriptions = Subscriptions;
            for (var i = 0; i < 2; i++) {
                while (!subscriptions.IsEmpty) {
                    var tasks = subscriptions
                        .Take(HardwareInfo.ProcessorCount * 4)
                        .ToList()
                        .Select(p => Task.Run(async () => {
                            var (publicationId, _) = (p.Key, p.Value);
                            var publication = Publisher.TryGet(publicationId);
                            if (publication != null)
                                await PublisherImpl.UnsubscribeAsync(Channel, publication).ConfigureAwait(false);
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

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            await base.DisposeInternalAsync(disposing);
            await RemoveSubscriptionsAsync().ConfigureAwait(false);
            PublisherImpl.OnChannelProcessorDisposed(this);
        }
    }
}
