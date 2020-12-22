using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stl.Async;
using Stl.Fusion.Bridge.Messages;
using Stl.Time;

namespace Stl.Fusion.Bridge.Internal
{
    public abstract class SubscriptionProcessor : AsyncProcessBase
    {
        protected readonly ILogger Log;
        protected readonly IMomentClock Clock;
        protected readonly TimeSpan ExpirationTime;
        protected long MessageIndex;
        protected (LTag Version, bool IsConsistent) LastSentVersion;

        public IPublisher Publisher => Publication.Publisher;
        public readonly IPublication Publication;
        public readonly Channel<BridgeMessage> OutgoingChannel;
        public readonly Channel<ReplicaMessage> IncomingChannel;

        protected SubscriptionProcessor(
            IPublication publication,
            Channel<BridgeMessage> outgoingChannel,
            TimeSpan expirationTime,
            IMomentClock clock,
            ILoggerFactory loggerFactory)
        {
            Log = loggerFactory.CreateLogger(GetType());
            Clock = clock;
            Publication = publication;
            OutgoingChannel = outgoingChannel;
            IncomingChannel = Channel.CreateBounded<ReplicaMessage>(new BoundedChannelOptions(16));
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
            IMomentClock clock,
            ILoggerFactory loggerFactory)
            : base(publication, outgoingChannel, expirationTime, clock, loggerFactory)
            => Publication = publication;

        protected override async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            var publicationUseScope = Publication.Use();
            var state = Publication.State;
            var incomingChannelReader = IncomingChannel.Reader;

            var currentCts = (CancellationTokenSource?) null;
            await using var _ = cancellationToken.Register(() => currentCts?.Cancel());
            try {
                var incomingMessageTask = incomingChannelReader.ReadAsync(cancellationToken).AsTask();
                while (true) {
                    // Awaiting for new SubscribeMessage
                    var messageOpt = await incomingMessageTask
                        .WithTimeout(Clock, ExpirationTime, cancellationToken)
                        .ConfigureAwait(false);
                    if (!messageOpt.IsSome(out var incomingMessage))
                        break; // Timeout

                    // Maybe sending an update
                    var isHardUpdateRequested = false;
                    var isSoftUpdateRequested = false;
                    if (incomingMessage is SubscribeMessage sm) {
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
                            await Publication.UpdateAsync(cts.Token).ConfigureAwait(false);
                            state = Publication.State;
                        }
                        finally {
                            currentCts = null;
                            cts.Dispose();
                        }
                    }
                    await TrySendUpdateAsync(state, isSoftUpdateRequested | isHardUpdateRequested, cancellationToken)
                        .ConfigureAwait(false);

                    incomingMessageTask = incomingChannelReader.ReadAsync(cancellationToken).AsTask();
                    // If we know for sure the last sent version is inconsistent,
                    // we don't need to wait till the moment it gets invalidated -
                    // it's client's time to act & request the update.
                    if (!LastSentVersion.IsConsistent)
                        continue;

                    // Awaiting for state change
                    var whenInvalidatedTask = state.WhenInvalidatedAsync();
                    var completedTask = await Task
                        .WhenAny(whenInvalidatedTask, incomingMessageTask)
                        .ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (completedTask == incomingMessageTask)
                        continue;

                    // And finally, sending the invalidation message
                    await TrySendUpdateAsync(state, false, cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                publicationUseScope.Dispose();
                // Awaiting for disposal here = cyclic task dependency;
                // we should just ensure it starts right when this method
                // completes.
                DisposeAsync().Ignore();
            }
        }

        protected virtual async ValueTask TrySendUpdateAsync(
            IPublicationState<T>? state, bool isUpdateRequested, CancellationToken cancellationToken)
        {
            if (state == null || state.IsDisposed) {
                var absentsMessage = new PublicationAbsentsMessage() {
                    IsDisposed = true,
                };
                await SendAsync(absentsMessage, cancellationToken).ConfigureAwait(false);
                LastSentVersion = default;
                return;
            }

            var computed = state.Computed;
            var isConsistent = computed.IsConsistent(); // It may change, so we want to make a snapshot here
            var version = (computed.Version, isConsistent);
            if ((!isUpdateRequested) && LastSentVersion == version)
                return;

            var message = new PublicationStateMessage<T>() {
                Version = computed.Version,
                IsConsistent = isConsistent,
            };
            if (isConsistent || LastSentVersion.Version != computed.Version)
                message.Output = computed.Output;

            await SendAsync(message, cancellationToken).ConfigureAwait(false);
            LastSentVersion = version;
        }

        protected virtual async ValueTask SendAsync(PublicationMessage message, CancellationToken cancellationToken)
        {
            message.MessageIndex = ++MessageIndex;
            message.PublisherId = Publisher.Id;
            message.PublicationId = Publication.Id;

            await OutgoingChannel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}
