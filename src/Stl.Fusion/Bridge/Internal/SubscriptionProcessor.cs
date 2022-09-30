using Stl.Fusion.Bridge.Messages;

namespace Stl.Fusion.Bridge.Internal;

public abstract class SubscriptionProcessor : WorkerBase
{
    private ILogger? _log;

    protected readonly IServiceProvider Services;
    protected readonly MomentClockSet Clocks;
    protected readonly TimeSpan ExpirationTime;
    protected long MessageIndex;
    protected (LTag Version, bool IsConsistent) LastSentVersion;
    protected LTag LastSentOutputVersion;
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public IPublisher Publisher => Publication.Publisher;
    public readonly IPublication Publication;
    public readonly PublisherChannelProcessor PublisherChannelProcessor;
    public readonly Channel<ReplicaRequest> IncomingChannel;

    protected SubscriptionProcessor(
        IPublication publication,
        PublisherChannelProcessor publisherChannelProcessor,
        TimeSpan expirationTime,
        MomentClockSet clocks,
        IServiceProvider services)
    {
        Services = services;
        Clocks = clocks;
        Publication = publication;
        PublisherChannelProcessor = publisherChannelProcessor;
        IncomingChannel = Channel.CreateBounded<ReplicaRequest>(new BoundedChannelOptions(16));
        ExpirationTime = expirationTime;
    }
}

public class SubscriptionProcessor<T> : SubscriptionProcessor
{
    public new readonly Publication<T> Publication;

    public SubscriptionProcessor(
        Publication<T> publication,
        PublisherChannelProcessor publisherChannelProcessor,
        TimeSpan expirationTime,
        MomentClockSet clocks,
        IServiceProvider services)
        : base(publication, publisherChannelProcessor, expirationTime, clocks, services)
        => Publication = publication;

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        var publicationUseScope = Publication.Use();
        var state = Publication.State;
        var incomingChannelReader = IncomingChannel.Reader;

        try {
            var incomingMessageTask = incomingChannelReader.ReadAsync(cancellationToken).AsTask();
            while (true) {
                // Awaiting for new SubscribeMessage
                var incomingMessage = await incomingMessageTask
                    .WaitAsync(ExpirationTime, cancellationToken)
                    .ConfigureAwait(false);

                // Maybe sending an update
                if (incomingMessage is UnsubscribeRequest)
                    return;
                if (incomingMessage is not SubscribeRequest sm)
                    continue;

                if (MessageIndex == 0) {
                    LastSentVersion = (sm.Version, sm.IsConsistent);
                    LastSentOutputVersion = sm.Version;
                }

                if (!sm.IsConsistent) { 
                    // Subscribe with IsInconsistent flag = request update
                    await Publication.Update(cancellationToken).ConfigureAwait(false);
                    state = Publication.State;
                }

                var computed = state.Computed;
                var isUpdateNeeded = sm.Version != computed.Version || sm.IsConsistent != computed.IsConsistent();
                await TrySendUpdate(state, isUpdateNeeded, cancellationToken).ConfigureAwait(false);

                incomingMessageTask = incomingChannelReader.ReadAsync(cancellationToken).AsTask();
                // If we know for sure the last sent version is inconsistent,
                // we don't need to wait till the moment it gets invalidated -
                // it's client's time to act & request the update.
                if (!LastSentVersion.IsConsistent)
                    continue;

                // Awaiting for invalidation or new message - whatever happens first;
                // CreateLinkedTokenSource is needed to make sure we truly cancel
                // WhenInvalidated(...) & remove the OnInvalidated handler. 
                var cts = cancellationToken.CreateLinkedTokenSource();
                try {
                    var whenInvalidatedTask = computed.WhenInvalidated(cts.Token);
                    var completedTask = await Task
                        .WhenAny(whenInvalidatedTask, incomingMessageTask)
                        .ConfigureAwait(false);
                    // WhenAny doesn't throw, and we need to make sure
                    // we exit right here in this task is cancelled. 
                    cancellationToken.ThrowIfCancellationRequested();
                    if (completedTask == incomingMessageTask)
                        continue;
                }
                finally {
                    cts.CancelAndDisposeSilently();
                }

                // And finally, sending the invalidation message
                await TrySendUpdate(state, false, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            var log = Services.LogFor(GetType());
            if (e is TimeoutException)
                log.LogDebug(e, "No incoming messages");
            else
                log.LogError(e, "Subscription processing failed");
        }
        finally {
            publicationUseScope.Dispose();
            IncomingChannel.Writer.TryComplete();
            // Awaiting for disposal here = cyclic task dependency;
            // we should just ensure it starts right when this method
            // completes.
            _ = DisposeAsync();
        }
    }

    protected virtual async ValueTask TrySendUpdate(
        PublicationState<T>? state, bool mustUpdate, CancellationToken cancellationToken)
    {
        if (state == null || state.IsDisposed) {
            var absentsMessage = new PublicationAbsentsReply();
            await Reply(absentsMessage, cancellationToken).ConfigureAwait(false);
            LastSentVersion = default;
            LastSentOutputVersion = default;
            return;
        }

        var computed = state.Computed;
        var isConsistent = computed.IsConsistent(); // It may change, so we want to make a snapshot here
        var version = (computed.Version, isConsistent);
        if (!mustUpdate && LastSentVersion == version)
            return;

        var mustSendOutput = LastSentOutputVersion != computed.Version;
        var reply = mustSendOutput
            ? PublicationStateReply<T>.New(computed.Output)
            : new PublicationStateReply<T>();
        reply.Version = computed.Version;
        reply.IsConsistent = isConsistent;

        await Reply(reply, cancellationToken).ConfigureAwait(false);
        LastSentVersion = version;
        if (mustSendOutput)
            LastSentOutputVersion = computed.Version;
    }

    protected virtual async ValueTask Reply(PublicationReply reply, CancellationToken cancellationToken)
    {
        reply.MessageIndex = ++MessageIndex;
        reply.PublisherId = Publisher.Id;
        reply.PublicationId = Publication.Id;
        await PublisherChannelProcessor.Reply(reply, cancellationToken).ConfigureAwait(false);
    }
}
