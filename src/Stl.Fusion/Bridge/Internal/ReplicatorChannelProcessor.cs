using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Extensibility;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Internal;
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

        protected readonly ILogger Log;
        protected readonly IReplicatorImpl ReplicatorImpl;
        protected readonly HashSet<Symbol> Subscriptions;
        protected Channel<Message>? SendChannel;
        protected Channel<Message>? Channel;
        protected Symbol ClientId => Replicator.Id;
        protected object Lock => Subscriptions;

        public readonly IReplicator Replicator;
        public readonly Symbol PublisherId;

        public ReplicatorChannelProcessor(IReplicator replicator, Symbol publisherId, ILogger? log = null)
        {
            Log = log ?? NullLogger.Instance;
            Replicator = replicator;
            ReplicatorImpl = (IReplicatorImpl) replicator;
            PublisherId = publisherId;
            Subscriptions = new HashSet<Symbol>();
        }

        protected override async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            var tasks = new Task<Message>[2];
            try {
                Task<Message>? channelReadTask = null;
                Task<Message>? sendChannelReadTask = null;
                while (true) {
                    try {
                        var channel = await GetChannelAsync(cancellationToken);

                        // Here we await for either channel.Reader or 
                        sendChannelReadTask ??= GetSendChannel().Reader.ReadAsync(cancellationToken).AsTask();
                        channelReadTask ??= channel.Reader.ReadAsync(cancellationToken).AsTask();
                        tasks[0] = sendChannelReadTask;
                        tasks[1] = channelReadTask; 
                        var completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);

                        var message = await completedTask.ConfigureAwait(false);
                        if (completedTask == sendChannelReadTask) {
                            sendChannelReadTask = null;
                            await channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                        }
                        else if (completedTask == channelReadTask) {
                            channelReadTask = null;
                            await OnMessageAsync(message, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) {
                        if (cancellationToken.IsCancellationRequested)
                            throw;
                    }
                    catch (Exception e) {
                        Log.LogError($"{ClientId}: Error: {e.GetType().Name}, {e.Message}", e);

                        // Reset state + cancel all pending requests
                        List<Symbol> subscriptions;
                        lock (Lock) {
                            channelReadTask = null;
                            sendChannelReadTask = null;
                            Channel = null;
                            SendChannel = null;
                            subscriptions = Subscriptions.ToList();
                        }
                        var ct = cancellationToken.IsCancellationRequested ? cancellationToken : default;
                        foreach (var publicationId in subscriptions) {
                            var replicaImpl = (IReplicaImpl?) Replicator.TryGet(publicationId);
                            replicaImpl?.ApplyFailedUpdate(e, ct);
                        }

                        await Task.Delay(ReplicatorImpl.ReconnectDelay, cancellationToken).ConfigureAwait(false);
                        Log.LogInformation($"{ClientId}: Reconnecting...");
                    }
                }
            }
            finally {
                // Awaiting for disposal here = cyclic task dependency;
                // we should just ensure it starts right when this method
                // completes.
                var _ = DisposeAsync();
            }
        }

        public virtual void Subscribe(IReplica replica)
        {
            // No checks, since they're done by the only caller of this method
            lock (Lock) {
                Subscriptions.Add(replica.PublicationId);
                Send(new SubscribeMessage() {
                    PublisherId = PublisherId,
                    PublicationId = replica.PublicationId,
                    IsUpdateRequested = replica.IsUpdateRequested,
                });
            }
        }

        public virtual void Unsubscribe(IReplica replica)
        {
            // No checks, since they're done by the only caller of this method
            lock (Lock) {
                Subscriptions.Remove(replica.PublicationId);
                Send(new UnsubscribeMessage() {
                    PublisherId = PublisherId,
                    PublicationId = replica.PublicationId,
                });
            }
        }

        protected virtual async ValueTask<Channel<Message>> GetChannelAsync(
            CancellationToken cancellationToken)
        {
            lock (Lock) {
                var channel = Channel;
                if (channel != null)
                    return channel;
            }

            var channelProvider = ReplicatorImpl.ChannelProvider;
            var newChannel = await channelProvider
                .CreateChannelAsync(PublisherId, cancellationToken)
                .ConfigureAwait(false);
            
            lock (Lock) {
                var channel = Channel;
                if (channel != null)
                    return channel;
                Channel = newChannel;
                SendChannel = null;
            }

            // 2. And queue new subscribe messages
            List<Symbol> subscriptions;
            lock (Lock) {
                subscriptions = Subscriptions.ToList();
            }
            foreach (var publicationId in subscriptions) {
                var replica = Replicator.TryGet(publicationId);
                if (replica != null)
                    Subscribe(replica);
                else {
                    lock (Lock) {
                        Subscriptions.Remove(publicationId);
                    }
                }
            }

            return newChannel;
        }

        protected virtual Task OnMessageAsync(Message message, CancellationToken cancellationToken)
        {
            switch (message) {
            case PublicationStateChangedMessage scm:
                // Fast dispatch to OnUpdatedMessageAsync<T> 
                return OnStateChangeMessageAsyncHandlers[scm.GetResultType()].Handle(scm, (this, cancellationToken));
            case PublicationAbsentsMessage pam:
                var replica = (IReplicaImpl?) Replicator.TryGet(pam.PublicationId);
                replica?.ApplyFailedUpdate(Errors.PublicationAbsents(), default);
                break;
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

            replicaImpl.ApplySuccessfulUpdate(lTaggedOutput, message.NewIsConsistent);
            return Task.CompletedTask;
        }
        
        protected virtual Channel<Message> CreateSendChannel() 
            => System.Threading.Channels.Channel.CreateUnbounded<Message>(
                new UnboundedChannelOptions() {
                    AllowSynchronousContinuations = true,
                    SingleReader = true,
                    SingleWriter = true,
                });

        protected virtual Channel<Message> GetSendChannel()
        {
            lock (Lock) {
                return SendChannel ??= CreateSendChannel();
            }
        }

        protected void Send(Message message) 
            => GetSendChannel().Writer.WriteAsync(message);
    }
}
