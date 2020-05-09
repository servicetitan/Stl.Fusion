using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Publish.Messages;
using Stl.OS;
using Stl.Text;

namespace Stl.Fusion.Publish.Internal
{
    public class ChannelProcessor : AsyncProcessBase, IPublicationHandler
    {
        public readonly Channel<Message> Channel;
        public readonly IPublisher Publisher;
        public readonly IPublisherImpl PublisherImpl;
        public readonly ConcurrentDictionary<Symbol, Unit> Subscriptions; 

        public ChannelProcessor(Channel<Message> channel, IPublisher publisher)
        {
            Channel = channel;
            Publisher = publisher;
            PublisherImpl = (IPublisherImpl) publisher;
            Subscriptions = new ConcurrentDictionary<Symbol, Unit>();
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
                return PublisherImpl.SubscribeAsync(Channel, publication, true, cancellationToken);
            case UnsubscribeMessage um:
                if (um.PublisherId != Publisher.Id)
                    break;
                publication = Publisher.TryGet(um.PublicationId);
                if (publication == null)
                    break;
                return PublisherImpl.UnsubscribeAsync(Channel, publication, true, cancellationToken);
            }
            return Task.CompletedTask;
        }

        Task IPublicationHandler.OnStateChangedAsync(
            IPublication publication, PublicationState previousState, Message? message,
            CancellationToken cancellationToken) 
            => OnStateChangedAsync(publication, previousState, message, cancellationToken);

        protected async Task OnStateChangedAsync(
            IPublication publication, PublicationState previousState, Message? message,
            CancellationToken cancellationToken)
        {
            if (message == null)
                return; 
            var writer = Channel.Writer;
            await writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            await base.DisposeInternalAsync(disposing);
            await RemoveSubscriptionsAsync().ConfigureAwait(false);
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
                                await PublisherImpl.UnsubscribeAsync(Channel, publication, false).ConfigureAwait(false);
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
