using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Fusion.Bridge.Messages;
using Stl.Locking;

namespace Stl.Fusion.Bridge.Internal
{
    public abstract class SubscriptionProcessor : AsyncProcessBase
    {
        protected readonly ILogger Log;
        protected readonly AsyncLock AsyncLock;
        protected long MessageIndex = 1;
        protected (LTag, bool) LastSentVersion = default;

        public IPublisher Publisher => Publication.Publisher;
        public readonly IPublicationImpl Publication;
        public readonly Channel<Message> Channel;
        public readonly SubscribeMessage SubscribeMessage;

        protected SubscriptionProcessor(
            IPublicationImpl publication, Channel<Message> channel, SubscribeMessage subscribeMessage,
            ILogger? log = null)
        {
            Log = log ?? NullLogger.Instance;
            Publication = publication;
            Channel = channel;
            SubscribeMessage = subscribeMessage;
            AsyncLock = new AsyncLock(ReentryMode.CheckedPass, TaskCreationOptions.None);
        }

        public abstract ValueTask OnMessageAsync(ReplicaMessage message, CancellationToken cancellationToken);
    }

    public class SubscriptionProcessor<T> : SubscriptionProcessor
    {
        public new readonly IPublicationImpl<T> Publication;

        public SubscriptionProcessor(
            IPublicationImpl<T> publication, Channel<Message> channel, SubscribeMessage subscribeMessage)
            : base(publication, channel, subscribeMessage)
        {
            Publication = publication;
        }

        protected override async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            var publicationUseScope = Publication.Use();
            try {
                var state = Publication.State;
                await TrySendUpdateAsync(state, SubscribeMessage.IsUpdateRequested, cancellationToken)
                    .ConfigureAwait(false);
                while (!state.IsDisposed) {
                    try {
                        await state.WhenInvalidatedAsync()
                            .WithFakeCancellation(cancellationToken)
                            .ConfigureAwait(false);
                        await TrySendUpdateAsync(state, false, cancellationToken)
                            .ConfigureAwait(false);
                        await state.WhenOutdatedAsync()
                            .WithFakeCancellation(cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                        if (cancellationToken.IsCancellationRequested)
                            throw;
                    }
                    // If we're here:
                    // 1) WhenInvalidatedAsync was cancelled due to Publication.State change
                    // 2) Or it has completed + WhenOutdatedAsync completed as well.
                    state = Publication.State;
                    await TrySendUpdateAsync(state, false, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            finally {
                publicationUseScope.Dispose();
                // Awaiting for disposal here = cyclic task dependency;
                // we should just ensure it starts right when this method
                // completes.
                var _ = DisposeAsync();
            }
        }

        public override async ValueTask OnMessageAsync(ReplicaMessage message, CancellationToken cancellationToken)
        {
            using var _ = await AsyncLock.LockAsync(cancellationToken);

            var state = Publication.State;
            switch (message) {
            case SubscribeMessage sm:
                await Publication.UpdateAsync(cancellationToken).ConfigureAwait(false);
                state = Publication.State;
                await TrySendUpdateAsync(state, SubscribeMessage.IsUpdateRequested, cancellationToken)
                    .ConfigureAwait(false);
                break;
            }
        }

        public virtual async ValueTask TrySendUpdateAsync(
            IPublicationState<T> state, bool isUpdateRequested, CancellationToken cancellationToken)
        {
            using var _ = await AsyncLock.LockAsync(cancellationToken);

            if (state.IsDisposed) {
                var absentsMessage = new PublicationAbsentsMessage() {
                    IsDisposed = true,
                };
                await SendUnsafeAsync(absentsMessage, cancellationToken).ConfigureAwait(false);
                LastSentVersion = default;
                return;
            }

            var computed = state.Computed;
            var isConsistent = computed.IsConsistent; // It may change, so we want to make a snapshot here
            var version = (computed.Version, isConsistent);
            if ((!isUpdateRequested) && LastSentVersion == version)
                return;

            var message = new PublicationStateChangedMessage<T>() {
                Version = computed.Version,
            };
            if (isConsistent)
                message.Output = computed.Output;

            await SendUnsafeAsync(message, cancellationToken).ConfigureAwait(false);
            LastSentVersion = version;
        }

        protected virtual async ValueTask SendUnsafeAsync(PublicationMessage message, CancellationToken cancellationToken)
        {
            message.MessageIndex = Interlocked.Increment(ref MessageIndex);
            message.PublisherId = Publisher.Id;
            message.PublicationId = Publication.Id;

            await Channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}
