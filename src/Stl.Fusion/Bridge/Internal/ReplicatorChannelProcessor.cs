using System;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Extensibility;
using Stl.Fusion.Bridge.Messages;
using Stl.Text;

namespace Stl.Fusion.Bridge.Internal
{
    public class ReplicatorChannelProcessor : AsyncProcessBase
    {
        protected static readonly HandlerProvider<(ReplicatorChannelProcessor, CancellationToken), Task> OnStateChangeMessageAsyncHandlers =
            new HandlerProvider<(ReplicatorChannelProcessor, CancellationToken), Task>(typeof(UpdatedMessageHandler<>));

        protected class UpdatedMessageHandler<T> : HandlerProvider<(ReplicatorChannelProcessor, CancellationToken), Task>.IHandler<T>
        {
            public Task Handle(object target, (ReplicatorChannelProcessor, CancellationToken) arg) 
                => arg.Item1.OnStateChangedMessageAsync((PublicationStateChangedMessage<T>) target, arg.Item2);
        }

        public readonly IReplicator Replicator;
        public readonly IReplicatorImpl ReplicatorImpl;
        public readonly Channel<Message> Channel;
        public readonly Symbol PublisherId;
        protected object Lock => new object();  

        public ReplicatorChannelProcessor(IReplicator replicator, Channel<Message> channel, Symbol publisherId)
        {
            Replicator = replicator;
            ReplicatorImpl = (IReplicatorImpl) replicator;
            Channel = channel;
            PublisherId = publisherId;
        }

        public ValueTask SubscribeAsync(IReplica replica, bool requestUpdate, CancellationToken cancellationToken)
        {
            // No checks, since they're done by the only caller of this method
            // if (replica.Replicator != Replicator || replica.PublisherId != PublisherId)
            //     throw new ArgumentOutOfRangeException(nameof(replica));

            var computed = replica.Computed;
            var subscribeMessage = new SubscribeMessage() {
                PublisherId = PublisherId,
                PublicationId = replica.PublicationId,
                IsUpdateRequested = requestUpdate,
            };
            return Channel.Writer.WriteAsync(subscribeMessage, cancellationToken);
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
                // Awaiting for disposal here = cyclic task dependency;
                // we should just ensure it starts right when this method
                // completes.
                var _ = DisposeAsync();
            }
        }

        protected virtual Task OnMessageAsync(Message message, CancellationToken cancellationToken)
        {
            switch (message) {
            case PublicationStateChangedMessage scm:
                // Fast dispatch to OnUpdatedMessageAsync<T> 
                return OnStateChangeMessageAsyncHandlers[scm.GetResultType()].Handle(scm, (this, cancellationToken));
            case PublicationDisposedMessage dm:
                var replica = Replicator.TryGet(dm.PublicationId);
                return replica?.DisposeAsync().AsTask() ?? Task.CompletedTask;
            }
            return Task.CompletedTask;
        }

        protected virtual Task OnStateChangedMessageAsync<T>(PublicationStateChangedMessage<T> message, CancellationToken cancellationToken)
        {
            var output = default(Result<T>);
            if (message.HasOutput) {
                output = message.OutputErrorType == null
                    ? new Result<T>(message.OutputValue, null)
                    : new Result<T>(
                        default!,
                        new TargetInvocationException(message.OutputErrorMessage,
                            new ApplicationException(message.OutputErrorMessage)
                        ));
            }
            var lTaggedOutput = new LTagged<Result<T>>(output, message.NewLTag);
            var replica = Replicator.GetOrAdd(message.PublisherId, message.PublicationId, lTaggedOutput);
            if (!(replica is IReplicaImpl<T> replicaImpl))
                // Weird case: somehow replica is of different type
                return Task.CompletedTask; 

            try {
                var computed = replica.Computed;
                if (message.NewLTag != computed.LTag) {
                    // LTags don't match => this is update + maybe invalidation
                    replicaImpl.ChangeState(computed, lTaggedOutput, message.NewIsConsistent);
                    return Task.CompletedTask; // Wrong type
                }

                // LTags are equal, so it could be only invalidation
                if (message.NewIsConsistent == false)
                    // There is a check that invalidation can happen only once, so...
                    computed.Invalidate(Replicator);

                return Task.CompletedTask;
            }
            finally {
                replicaImpl.CompleteUpdateRequest();
            }
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            await base.DisposeInternalAsync(disposing);
            ReplicatorImpl.OnChannelProcessorDisposed(this);
        }
    }
}
