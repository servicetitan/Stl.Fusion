using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Stl.Async;
using Stl.Fusion.Publish.Messages;
using Stl.OS;
using Stl.Text;

namespace Stl.Fusion.Publish.Internal
{
    public class ChannelProcessor : AsyncProcessBase
    {
        public readonly Channel<Message> Channel;
        public readonly IPublisher Publisher;
        public readonly IPublisherImpl PublisherImpl;
        public readonly ConcurrentDictionary<Symbol, (Task SubscriptionTask, CancellationTokenSource StopCts)> Subscriptions;
        protected object Lock => Subscriptions;  

        public ChannelProcessor(Channel<Message> channel, IPublisher publisher)
        {
            Channel = channel;
            Publisher = publisher;
            PublisherImpl = (IPublisherImpl) publisher;
            Subscriptions = new ConcurrentDictionary<Symbol, (Task, CancellationTokenSource)>();
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

        protected virtual Task OnMessageAsync(Message message, CancellationToken cancellationToken)
        {
            switch (message) {
            case SubscribeMessage sm:
                if (sm.PublisherId != Publisher.Id)
                    break;
                var publication = Publisher.TryGet(sm.PublicationId);
                if (publication == null)
                    break;
                PublisherImpl.Subscribe(Channel, publication, true);
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

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            await base.DisposeInternalAsync(disposing);
            await RemoveSubscriptionsAsync().ConfigureAwait(false);
        }

        public virtual bool Subscribe(IPublication publication, bool notify)
        {
            var publicationId = publication.Id;
            if (Subscriptions.TryGetValue(publicationId, out _))
                return false;
            lock (Lock) {
                // Double check locking
                if (Subscriptions.TryGetValue(publicationId, out var _))
                    return false;
                var publicationImpl = (IPublicationImpl) publication;
                var stopCts = new CancellationTokenSource();
                try {
                    var subscriptionTask = publicationImpl
                        .RunSubscriptionAsync(Channel, notify, stopCts.Token)
                        .SuppressExceptions() // TODO: Add exception logging?
                        .ContinueWith(_ => UnsubscribeAsync(publication), CancellationToken.None);
                    Subscriptions[publicationId] = (subscriptionTask, stopCts);
                }
                catch {
                    try {
                        stopCts.Cancel();
                    }
                    finally {
                        stopCts?.Dispose();
                    }
                    throw;
                }
                return true;
            }
        }

        public virtual async ValueTask<bool> UnsubscribeAsync(IPublication publication)
        {
            var publicationId = publication.Id;
            if (!Subscriptions.TryRemove(publicationId, out var info))
                return false;
            var (subscriptionTask, stopCts) = info;
            if (!stopCts.IsCancellationRequested) {
                try {
                    stopCts.Cancel();
                }
                catch {
                    // We don't care about any exceptions here
                }
            }
            await subscriptionTask.ConfigureAwait(false);
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
    }
}
