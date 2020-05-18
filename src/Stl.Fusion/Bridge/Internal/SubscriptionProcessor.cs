using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Bridge.Messages;

namespace Stl.Fusion.Bridge.Internal
{
    public abstract class SubscriptionProcessor : AsyncProcessBase
    {
        protected bool ReplicaIsConsistent;
        protected LTag ReplicaLTag; 
        protected long MessageIndex = 1;
        protected object Lock => this;

        public IPublisher Publisher => Publication.Publisher;
        public readonly IPublicationImpl Publication;
        public readonly Channel<PublicationMessage> Channel;
        public readonly SubscribeMessage SubscribeMessage;

        protected SubscriptionProcessor(
            IPublicationImpl publication, Channel<PublicationMessage> channel, SubscribeMessage subscribeMessage)
        {
            Publication = publication;
            Channel = channel;
            SubscribeMessage = subscribeMessage;
            ReplicaLTag = subscribeMessage.ReplicaLTag;
            ReplicaIsConsistent = subscribeMessage.ReplicaIsConsistent;
        }

        public abstract ValueTask OnMessageAsync(ReplicaMessage message, CancellationToken cancellationToken);
    }

    public class SubscriptionProcessor<T> : SubscriptionProcessor
    {
        public new readonly IPublicationImpl<T> Publication;

        public SubscriptionProcessor( 
            IPublicationImpl<T> publication, Channel<PublicationMessage> channel, SubscribeMessage subscribeMessage)
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
                        var invalidatedBy = await state.InvalidatedAsync()
                            .WithFakeCancellation(cancellationToken)
                            .ConfigureAwait(false);
                        await TrySendUpdateAsync(state, false, cancellationToken)
                            .ConfigureAwait(false);
                        await state.OutdatedAsync()
                            .WithFakeCancellation(cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                        if (cancellationToken.IsCancellationRequested)
                            throw;
                        // => InvalidatedAsync was cancelled due to Publication.State change;
                        // => OutdatedAsync is already completed too.
                    }
                    state = Publication.State;
                    await TrySendUpdateAsync(state, false, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            finally {
                publicationUseScope.Dispose();
                await DisposeAsync().ConfigureAwait(false);
            }
        }

        public override async ValueTask OnMessageAsync(ReplicaMessage message, CancellationToken cancellationToken)
        {
            var state = Publication.State;
            lock (Lock) {
                (ReplicaLTag, ReplicaIsConsistent) = (message.ReplicaLTag, message.ReplicaIsConsistent);
            }
            switch (message) {
            case SubscribeMessage sm:
                await Publication.UpdateAsync(cancellationToken).ConfigureAwait(false);
                state = Publication.State;
                await TrySendUpdateAsync(state, SubscribeMessage.IsUpdateRequested, cancellationToken)
                    .ConfigureAwait(false);
                break;
            }
        }

        public virtual ValueTask TrySendUpdateAsync(
            IPublicationState<T> state, bool isUpdateRequested, CancellationToken cancellationToken)
        {
            if (state.IsDisposed)
                return SendAsync(new DisposedMessage(), cancellationToken);
            
            var computed = state.Computed;
            var computedIsConsistent = computed.IsConsistent; // May change at any moment to false, so...
            var computedVersion = (computed.LTag, computedIsConsistent);

            LTag replicaLTag;
            bool replicaIsConsistent;
            lock (Lock) {
                (replicaLTag, replicaIsConsistent) = (ReplicaLTag, ReplicaIsConsistent);
            }
            var replicaVersion = (replicaLTag, replicaIsConsistent);
            var isUpdated = replicaVersion != computedVersion;
            if (!(isUpdated || isUpdateRequested))
                return ValueTaskEx.CompletedTask;

            var message = new StateChangeMessage<T>() {
                ReplicaLTag = replicaLTag,
                ReplicaIsConsistent = replicaIsConsistent,
                NewLTag = computed.LTag,
                NewIsConsistent = computedIsConsistent,
            };
            if (isUpdated && computedIsConsistent) {
                message.HasOutput = true;
                message.Output = computed.Output;
            }
            
            return SendAsync(message, cancellationToken);
        }

        protected virtual async ValueTask SendAsync(PublicationMessage? message, CancellationToken cancellationToken)
        {
            if (message == null)
                return;

            message.MessageIndex = Interlocked.Increment(ref MessageIndex);
            message.PublisherId = Publisher.Id;
            message.PublicationId = Publication.Id;

            await Channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}
