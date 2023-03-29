using Stl.Fusion.Bridge.Messages;
using Stl.Internal;
using Stl.Locking;

namespace Stl.Fusion.Bridge.Internal;

public class PublisherChannelProcessor : WorkerBase
{
    private ILogger? _log;

    protected readonly IServiceProvider Services;
    protected IPublisherImpl PublisherImpl => (IPublisherImpl) Publisher;
    protected readonly Dictionary<Symbol, SubscriptionProcessor> Subscriptions = new();
    protected readonly AsyncLock ReplyLock = new(ReentryMode.UncheckedDeadlock);
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public readonly IPublisher Publisher;
    public readonly Channel<BridgeMessage> Channel;

    public PublisherChannelProcessor(
        IPublisher publisher,
        Channel<BridgeMessage> channel,
        IServiceProvider services)
    {
        Services = services;
        Publisher = publisher;
        Channel = channel;
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        try {
            var reader = Channel.Reader;
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            while (reader.TryRead(out var request)) {
                switch (request) {
                case ReplicaRequest rr:
                    await OnReplicaRequest(rr, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    await OnUnsupportedRequest(request, cancellationToken).ConfigureAwait(false);
                    break;
                }
            }
        }
        finally {
            // Awaiting for disposal here = cyclic task dependency;
            // we should just ensure it starts right when this method
            // completes.
            _ = DisposeAsync();
        }
    }

    protected virtual async ValueTask OnUnsupportedRequest(BridgeMessage request, CancellationToken cancellationToken)
    {
        if (request is not ReplicaRequest rr)
            return;

        var reply = new PublicationAbsentsReply() {
            PublisherId = rr.PublisherId,
            PublicationId = rr.PublicationId,
        };
        await Reply(reply, cancellationToken).ConfigureAwait(false);
    }

    public virtual async ValueTask OnReplicaRequest(ReplicaRequest request, CancellationToken cancellationToken)
    {
        if (request.PublisherId != Publisher.Id) {
            await OnUnsupportedRequest(request, cancellationToken).ConfigureAwait(false);
            return;
        }

        var publicationId = request.PublicationId;
        var publication = Publisher.Get(publicationId);
        while (publication?.TryTouch() == true) {
            SubscriptionProcessor? subscriptionProcessor;
            lock (Subscriptions) {
                subscriptionProcessor = Subscriptions.GetValueOrDefault(publicationId);
                if (subscriptionProcessor == null) {
                    if (StopToken.IsCancellationRequested) // No new sub. processors on disposal!
                        throw Errors.AlreadyDisposedOrDisposing();

                    var publisherOptions = Publisher.Options;
                    subscriptionProcessor = publisherOptions.SubscriptionProcessorFactory.Create(
                        publisherOptions.SubscriptionProcessorGeneric,
                        publication, this, publisherOptions.SubscriptionExpirationTime,
                        Publisher.Clocks, Services);
                    Subscriptions[publicationId] = subscriptionProcessor;
                    _ = subscriptionProcessor.Run()
                        .ContinueWith(_ => Unsubscribe(publication, default), TaskScheduler.Default);
                }
            }

            try {
                // Forwarding the message to sub. processor
                await subscriptionProcessor.IncomingChannel.Writer
                    .WriteAsync(request, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (ChannelClosedException) {
                // Already disposed, let's retry
            }
        }

        // Couldn't find or touch the publication
        await OnUnsupportedRequest(request, cancellationToken).ConfigureAwait(false);
    }

    public virtual async ValueTask Unsubscribe(
        IPublication publication, CancellationToken cancellationToken)
    {
        if (StopToken.IsCancellationRequested)
            return; // DisposeAsyncCore is running, it will unsubscribe everything anyway

        var publicationId = publication.Id;
        SubscriptionProcessor? subscriptionProcessor;
        lock (Subscriptions) {
            if (!Subscriptions.Remove(publicationId, out subscriptionProcessor))
                return;
        }
        await subscriptionProcessor.DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask Reply(PublicationReply reply, CancellationToken cancellationToken)
    {
        using var _ = await ReplyLock.Lock(cancellationToken).ConfigureAwait(false);
        await Channel.Writer.WriteAsync(reply, cancellationToken).ConfigureAwait(false);
    }

    protected override async Task DisposeAsyncCore()
    {
        await base.DisposeAsyncCore().ConfigureAwait(false);

        // Disposing subscriptions
        while (true) {
            List<SubscriptionProcessor> subscriptionProcessors;
            lock (Subscriptions) {
                subscriptionProcessors = Subscriptions.Values.ToList();
                Subscriptions.Clear();
            }
            if (subscriptionProcessors.Count == 0)
                break;
            await subscriptionProcessors
                .Select(sp => sp.DisposeAsync().AsTask())
                .Collect()
                .ConfigureAwait(false);
        }

        PublisherImpl.OnChannelProcessorDisposed(this);
    }
}
