using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        protected volatile Task<Channel<Message>> ChannelTask = null!;
        protected volatile Channel<Message> SendChannel = null!;
        protected volatile Exception? LastError;
        protected Symbol ClientId => Replicator.Id;
        protected object Lock => Subscriptions;

        public readonly IReplicator Replicator;
        public readonly Symbol PublisherId;
        public SimpleComputedInput<bool> StateComputed;

        public ReplicatorChannelProcessor(IReplicator replicator, Symbol publisherId, ILogger? log = null)
        {
            Log = log ?? NullLogger.Instance;
            Replicator = replicator;
            ReplicatorImpl = (IReplicatorImpl) replicator;
            PublisherId = publisherId;
            Subscriptions = new HashSet<Symbol>();
            StateComputed = ((SimpleComputed<bool>) SimpleComputed.New<bool>(
                ComputedOptions.NoAutoInvalidateOnError,
                (c, ct) => {
                    lock (Lock) {
                        var lastError = LastError;
                        if (lastError != null)
                            return Task.FromException<bool>(lastError);
                        return Task.FromResult(ChannelTask.IsCompleted);
                    }
                }))
                .Input;
            // ReSharper disable once VirtualMemberCallInConstructor
            Reconnect();
        }

        protected override async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            try {
                var lastChannelTask = (Task<Channel<Message>>?) null;
                var channel = (Channel<Message>) null!;
                while (true) {
                    var error = (Exception?) null;
                    try {
                        var channelTask = ChannelTask;
                        if (channelTask != lastChannelTask)
                            channel = await ChannelTask.WithFakeCancellation(cancellationToken).ConfigureAwait(false);
                        var message = await channel.Reader.ReadAsync(cancellationToken).AsTask();
                        await OnMessageAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                        if (cancellationToken.IsCancellationRequested)
                            throw;
                    }
                    catch (AggregateException e) {
                        error = e.Flatten().InnerExceptions.SingleOrDefault() ?? e;
                    }
                    catch (Exception e) {
                        error = e;
                    }

                    switch (error) {
                    case null:
                        break;
                    case OperationCanceledException oce:
                        if (cancellationToken.IsCancellationRequested)
                            throw oce;
                        break;
                    default:
                        Reconnect(error);
                        var ct = cancellationToken.IsCancellationRequested ? cancellationToken : default;
                        foreach (var publicationId in GetSubscriptions()) {
                            var replicaImpl = (IReplicaImpl?) Replicator.TryGet(publicationId);
                            replicaImpl?.ApplyFailedUpdate(error, ct);
                        }
                        break;
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

        protected List<Symbol> GetSubscriptions()
        {
            lock (Lock) {
                return Subscriptions.ToList();
            }
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
            var replica = Replicator.GetOrAdd(
                message.PublisherId, message.PublicationId,
                message.Output, message.Version, message.IsConsistent);
            if (!(replica is IReplicaImpl<T> replicaImpl))
                // Weird case: somehow replica is of different type
                return Task.CompletedTask;

            replicaImpl.ApplySuccessfulUpdate(message.Output, message.Version, message.IsConsistent);
            return Task.CompletedTask;
        }

        protected virtual void Reconnect(Exception? error = null)
        {
            var sendChannel = CreateSendChannel();
            var channelTaskSource = TaskSource.New<Channel<Message>>(true);
            var channelTask = channelTaskSource.Task;

            Channel<Message> oldSendChannel;
            lock (Lock) {
                oldSendChannel = Interlocked.Exchange(ref SendChannel, sendChannel);
                Interlocked.Exchange(ref ChannelTask, channelTask);
                Interlocked.Exchange(ref LastError, error);
            }
            try {
                oldSendChannel?.Writer.TryComplete();
            }
            catch {
                // It's better to suppress all exceptions here
            }
            StateComputed.Computed.Invalidate();

            // Connect task
            var connectTask = Task.Run(async () => {
                var cancellationToken = CancellationToken.None;
                if (error != null) {
                    Log.LogError(error, $"{ClientId}: Error.");
                    await Task.Delay(ReplicatorImpl.ReconnectDelay, cancellationToken).ConfigureAwait(false);
                    Log.LogInformation($"{ClientId}: Reconnecting...");
                }
                else
                    Log.LogInformation($"{ClientId}: Connecting...");

                var channelProvider = ReplicatorImpl.ChannelProvider;
                var channel = await channelProvider
                    .CreateChannelAsync(PublisherId, cancellationToken)
                    .ConfigureAwait(false);

                foreach (var publicationId in GetSubscriptions()) {
                    var replica = Replicator.TryGet(publicationId);
                    if (replica != null)
                        Subscribe(replica);
                    else {
                        lock (Lock) {
                            Subscriptions.Remove(publicationId);
                        }
                    }
                }

                return channel;
            });
            connectTask.ContinueWith(ct => {
                channelTaskSource.SetFromTask(ct);
                if (!ct.IsCompletedSuccessfully)
                    // LastError will be updated anyway in this case
                    return;
                lock (Lock) {
                    Interlocked.Exchange(ref LastError, null);
                }
                StateComputed.Computed.Invalidate();
            });

            // Copy task
            Task.Run(async () => {
                var cancellationToken = CancellationToken.None;
                try {
                    var sendChannelReader = sendChannel.Reader;
                    var channel = (Channel<Message>?) null;
                    while (await sendChannelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        if (sendChannel != SendChannel)
                            break;
                        if (!sendChannelReader.TryRead(out var message))
                            continue;
                        channel ??= await channelTask.ConfigureAwait(false);
                        await channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception e) {
                    Log.LogError(e, $"{ClientId}: Error.");
                }
            });
        }

        protected virtual Channel<Message> CreateSendChannel()
            => Channel.CreateUnbounded<Message>(
                new UnboundedChannelOptions() {
                    AllowSynchronousContinuations = true,
                    SingleReader = true,
                    SingleWriter = false,
                });

        protected void Send(Message message)
        {
            SendChannel.Writer.WriteAsync(message);
        }
    }
}
