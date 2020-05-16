using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Extensibility;
using Stl.Fusion.Bridge.Messages;

namespace Stl.Fusion.Bridge.Internal
{
    public class ReplicatorChannelProcessor : AsyncProcessBase
    {
        protected static readonly HandlerProvider<(ReplicatorChannelProcessor, CancellationToken), Task> OnUpdatedMessageAsyncHandlers =
            new HandlerProvider<(ReplicatorChannelProcessor, CancellationToken), Task>(typeof(UpdatedMessageHandler<>));

        protected class UpdatedMessageHandler<T> : HandlerProvider<(ReplicatorChannelProcessor, CancellationToken), Task>.IHandler<T>
        {
            public Task Handle(object target, (ReplicatorChannelProcessor, CancellationToken) arg) 
                => arg.Item1.OnUpdatedMessageAsync((UpdatedMessage<T>) target, arg.Item2);
        }

        public readonly Channel<PublicationMessage> Channel;
        public readonly IReplicator Replicator;
        public readonly IReplicatorImpl ReplicatorImpl;
        protected object Lock => new object();  

        public ReplicatorChannelProcessor(Channel<PublicationMessage> channel, IReplicator replicator)
        {
            Channel = channel;
            Replicator = replicator;
            ReplicatorImpl = (IReplicatorImpl) replicator;
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
            case InvalidatedMessage im:
                var replica = Replicator.TryGet(im.PublicationId);
                if (replica != null) {
                    var computed = replica.Computed;
                    if (computed.Tag == im.Tag)
                        computed.Invalidate(this);
                }
                break;
            case UpdatedMessage um:
                // Fast dispatch to OnUpdatedMessageAsync<T> 
                return OnUpdatedMessageAsyncHandlers[um.GetResultType()].Handle(um, (this, cancellationToken));
            case DisposedMessage dm:
                replica = Replicator.TryGet(dm.PublicationId);
                return replica?.DisposeAsync().AsTask() ?? Task.CompletedTask;
            case SubscribeMessage _:
            case UnsubscribeMessage _:
                // Subscribe & unsubscribe messages are ignored
                break;
            }
            return Task.CompletedTask;
        }

        protected virtual Task OnUpdatedMessageAsync<T>(UpdatedMessage<T> message, CancellationToken cancellationToken)
        {
            var initialOutput = new TaggedResult<T>(message.Output, message.Tag);
            Replicator.TryAddOrGet(message.PublisherId, message.PublicationId, initialOutput);
            return Task.CompletedTask;
        }
    }
}
