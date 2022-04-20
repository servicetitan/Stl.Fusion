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
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public IPublisher Publisher => Publication.Publisher;
    public readonly IPublication Publication;
    public readonly Channel<BridgeMessage> OutgoingChannel;
    public readonly Channel<ReplicaRequest> IncomingChannel;

    protected SubscriptionProcessor(
        IPublication publication,
        Channel<BridgeMessage> outgoingChannel,
        TimeSpan expirationTime,
        MomentClockSet clocks,
        IServiceProvider services)
    {
        Services = services;
        Clocks = clocks;
        Publication = publication;
        OutgoingChannel = outgoingChannel;
        IncomingChannel = Channel.CreateBounded<ReplicaRequest>(new BoundedChannelOptions(16));
        ExpirationTime = expirationTime;
    }
}

public class SubscriptionProcessor<T> : SubscriptionProcessor
{
    public new readonly IPublication<T> Publication;

    public SubscriptionProcessor(
        IPublication<T> publication,
        Channel<BridgeMessage> outgoingChannel,
        TimeSpan expirationTime,
        MomentClockSet clocks,
        IServiceProvider services)
        : base(publication, outgoingChannel, expirationTime, clocks, services)
        => Publication = publication;

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        var publicationUseScope = Publication.Use();
        var state = Publication.State;
        var incomingChannelReader = IncomingChannel.Reader;

        var currentCts = (CancellationTokenSource?) null;
        // ReSharper disable once AccessToModifiedClosure
        await using var registered = cancellationToken.Register(() => currentCts?.Cancel())
            .ToAsyncDisposableAdapter().ConfigureAwait(false);
        try {
            var incomingMessageTask = incomingChannelReader.ReadAsync(cancellationToken).AsTask();
            while (true) {
                // Awaiting for new SubscribeMessage
                var messageOpt = await incomingMessageTask
                    .WithTimeout(Clocks.CoarseCpuClock, ExpirationTime, cancellationToken)
                    .ConfigureAwait(false);
                if (!messageOpt.IsSome(out var incomingMessage))
                    break; // Timeout

                // Maybe sending an update
                var isHardUpdateRequested = false;
                var isSoftUpdateRequested = false;
                if (incomingMessage is SubscribeRequest sm) {
                    if (MessageIndex == 0)
                        LastSentVersion = (sm.Version, sm.IsConsistent);
                    isHardUpdateRequested |= sm.IsUpdateRequested;
                    // Generally the version should match; if it's not the case, it could be due to
                    // reconnect / lost message / something similar.
                    isSoftUpdateRequested |= sm.Version != state.Computed.Version;
                }
                if (isHardUpdateRequested) {
                    // We do only explicit state updates
                    var cts = new CancellationTokenSource();
                    currentCts = cts;
                    try {
                        await Publication.Update(cts.Token).ConfigureAwait(false);
                        state = Publication.State;
                    }
                    finally {
                        currentCts = null;
                        cts.Dispose();
                    }
                }
                await TrySendUpdate(state, isSoftUpdateRequested | isHardUpdateRequested, cancellationToken)
                    .ConfigureAwait(false);

                incomingMessageTask = incomingChannelReader.ReadAsync(cancellationToken).AsTask();
                // If we know for sure the last sent version is inconsistent,
                // we don't need to wait till the moment it gets invalidated -
                // it's client's time to act & request the update.
                if (!LastSentVersion.IsConsistent)
                    continue;

                // Awaiting for state change
                var whenInvalidatedTask = state.WhenInvalidated();
                var completedTask = await Task
                    .WhenAny(whenInvalidatedTask, incomingMessageTask)
                    .ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (completedTask == incomingMessageTask)
                    continue;

                // And finally, sending the invalidation message
                await TrySendUpdate(state, false, cancellationToken).ConfigureAwait(false);
            }
        }
        finally {
            publicationUseScope.Dispose();
            // Awaiting for disposal here = cyclic task dependency;
            // we should just ensure it starts right when this method
            // completes.
            _ = DisposeAsync();
        }
    }

    protected virtual async ValueTask TrySendUpdate(
        IPublicationState<T>? state, bool isUpdateRequested, CancellationToken cancellationToken)
    {
        if (state == null || state.IsDisposed) {
            var absentsMessage = new PublicationAbsentsReply();
            await Send(absentsMessage, cancellationToken).ConfigureAwait(false);
            LastSentVersion = default;
            return;
        }

        var computed = state.Computed;
        var isConsistent = computed.IsConsistent(); // It may change, so we want to make a snapshot here
        var version = (computed.Version, isConsistent);
        if ((!isUpdateRequested) && LastSentVersion == version)
            return;

        var reply = isConsistent || LastSentVersion.Version != computed.Version
            ? PublicationStateReply<T>.New(computed.Output)
            : new PublicationStateReply<T>();
        reply.Version = computed.Version;
        reply.IsConsistent = isConsistent;

        await Send(reply, cancellationToken).ConfigureAwait(false);
        LastSentVersion = version;
    }

    protected virtual async ValueTask Send(PublicationReply reply, CancellationToken cancellationToken)
    {
        reply.MessageIndex = ++MessageIndex;
        reply.PublisherId = Publisher.Id;
        reply.PublicationId = Publication.Id;

        await OutgoingChannel.Writer.WriteAsync(reply, cancellationToken).ConfigureAwait(false);
    }
}
