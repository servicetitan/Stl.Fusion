using System.Runtime.ExceptionServices;
using Stl.Extensibility;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge.Internal;

public class ReplicatorChannelProcessor : WorkerBase
{
    private ILogger? _log;

    protected static readonly HandlerProvider<(ReplicatorChannelProcessor, CancellationToken), Task> OnPublicationStateReplyHandlers =
        new(typeof(PublicationStateReplyHandler<>));

    protected class PublicationStateReplyHandler<T> : HandlerProvider<(ReplicatorChannelProcessor, CancellationToken), Task>.IHandler<T>
    {
        public Task Handle(object target, (ReplicatorChannelProcessor, CancellationToken) arg)
            => arg.Item1.OnPublicationStateReply((PublicationStateReply<T>) target, arg.Item2);
    }

    protected readonly IServiceProvider Services;
    protected IReplicatorImpl ReplicatorImpl => (IReplicatorImpl) Replicator;
    protected readonly HashSet<Symbol> Subscriptions;
    protected volatile Task<Channel<BridgeMessage>> ChannelTask = null!;
    protected volatile Channel<BridgeMessage> SendChannel = null!;
    protected Symbol ClientId => Replicator.Id;
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public readonly IReplicator Replicator;
    public readonly Symbol PublisherId;
    public readonly IMutableState<bool> IsConnected;

    public ReplicatorChannelProcessor(IReplicator replicator, Symbol publisherId, IServiceProvider services)
    {
        Services = services;
        Replicator = replicator;
        PublisherId = publisherId;
        Subscriptions = new HashSet<Symbol>();
        var stateFactory = Replicator.Services.StateFactory();
        IsConnected = stateFactory.NewMutable(true);
        // ReSharper disable once VirtualMemberCallInConstructor
        Reconnect();
    }

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        try {
            var lastChannelTask = (Task<Channel<BridgeMessage>>?) null;
            var channel = (Channel<BridgeMessage>) null!;
            while (true) {
                var error = (Exception?) null;
                try {
                    var channelTask = ChannelTask;
                    if (channelTask != lastChannelTask)
                        channel = await ChannelTask.WithFakeCancellation(cancellationToken).ConfigureAwait(false);
                    var reply = await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    await OnReply(reply, cancellationToken).ConfigureAwait(false);
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
                        ExceptionDispatchInfo.Capture(oce).Throw();
                    break;
                default:
                    Reconnect(error);
                    // NOTE(AY): Starting from v2.0.73 Fusion won't
                    // tag all replicas from failed channel as inconsistent.
                    // And since Reconnect implies (re)-Subscribe for any
                    // replica, this ensures the inconsistent ones will
                    // be anyway updated on reconnect.
                    /*
                    foreach (var publicationId in GetSubscriptions()) {
                        var publicationRef = new PublicationRef(PublisherId, publicationId);
                        var replicaImpl = (IReplicaImpl?) Replicator.Get(publicationRef);
                        replicaImpl?.ApplyFailedUpdate(error, cancellationToken);
                    }
                    */
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

    public virtual void Subscribe(IReplica replica)
    {
        // No checks, since they're done by the only caller of this method
        var publicationId = replica.PublicationRef.PublicationId;
        var computed = replica.Computed;
        var isConsistent = computed.IsConsistent();
        lock (Lock) {
            Subscriptions.Add(publicationId);
            Send(new SubscribeRequest() {
                PublisherId = PublisherId,
                PublicationId = publicationId,
                Version = computed.Version,
                IsConsistent = isConsistent,
                IsUpdateRequested = replica.IsUpdateRequested,
            });
        }
    }

    public virtual void Unsubscribe(IReplica replica)
    {
        // No checks, since they're done by the only caller of this method
        var publicationId = replica.PublicationRef.PublicationId;
        lock (Lock) {
            Subscriptions.Remove(publicationId);
            Send(new UnsubscribeRequest() {
                PublisherId = PublisherId,
                PublicationId = publicationId,
            });
        }
    }

    protected List<Symbol> GetSubscriptions()
    {
        lock (Lock) {
            return Subscriptions.ToList();
        }
    }

    protected virtual Task OnReply(BridgeMessage reply, CancellationToken cancellationToken)
    {
        switch (reply) {
        case PublicationStateReply psr:
            // Fast dispatch to OnStateMessage<T>
            return OnPublicationStateReplyHandlers[psr.GetResultType()].Handle(psr, (this, cancellationToken));
        case PublicationAbsentsReply pam:
            var replica = (IReplicaImpl?) Replicator.Get((PublisherId, pam.PublicationId));
            replica?.ApplyFailedUpdate(Errors.PublicationAbsents(), default);
            break;
        }
        return Task.CompletedTask;
    }

    protected virtual Task OnPublicationStateReply<T>(PublicationStateReply<T> reply, CancellationToken cancellationToken)
    {
        // Debug.WriteLine($"#{message.MessageIndex} -> {message.Version}, {message.IsConsistent}, {message.Output.HasValue}");
        var psi = new PublicationStateInfo<T>(
            new PublicationRef(reply.PublisherId, reply.PublicationId),
            reply.Version, reply.IsConsistent,
            reply.Output.GetValueOrDefault());

        var replica = Replicator.GetOrAdd(psi);
        var replicaImpl = (IReplicaImpl<T>) replica;
        replicaImpl.ApplySuccessfulUpdate(reply.Output, psi.Version, psi.IsConsistent);
        return Task.CompletedTask;
    }

    protected virtual void Reconnect(Exception? error = null)
    {
        if (error != null)
            IsConnected.Error = error;
        else
            IsConnected.Value = false;

        var sendChannel = CreateSendChannel();
        var channelTaskSource = TaskSource.New<Channel<BridgeMessage>>(true);
        var channelTask = channelTaskSource.Task;

        Channel<BridgeMessage> oldSendChannel;
        lock (Lock) {
            oldSendChannel = Interlocked.Exchange(ref SendChannel, sendChannel);
            Interlocked.Exchange(ref ChannelTask, channelTask);
        }
        try {
            oldSendChannel?.Writer.TryComplete();
        }
        catch {
            // Intended
        }

        // Connect task
        var connectTask = Task.Run(async () => {
            var cancellationToken = CancellationToken.None;
            if (error != null) {
                Log.LogError(error, "{ClientId}: error", ClientId);
                await Task.Delay(Replicator.Options.ReconnectDelay, cancellationToken).ConfigureAwait(false);
                Log.LogInformation("{ClientId}: reconnecting...", ClientId);
            }
            else
                Log.LogInformation("{ClientId}: connecting...", ClientId);

            var channelProvider = ReplicatorImpl.ChannelProvider;
            var channel = await channelProvider
                .CreateChannel(PublisherId, cancellationToken)
                .ConfigureAwait(false);

            foreach (var publicationId in GetSubscriptions()) {
                var publicationRef = new PublicationRef(PublisherId, publicationId);
                var replica = Replicator.Get(publicationRef);
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
        connectTask.ContinueWith(connectTask1 => {
            channelTaskSource.SetFromTask(connectTask1, CancellationToken.None);
            if (connectTask1.IsCompletedSuccessfully())
                IsConnected.Value = true;
        }, TaskScheduler.Default);

        // Copy task
        Task.Run(async () => {
            var cancellationToken = CancellationToken.None;
            try {
                var sendChannelReader = sendChannel.Reader;
                var channel = (Channel<BridgeMessage>?) null;
                while (await sendChannelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                while (sendChannelReader.TryRead(out var message)) {
                    if (sendChannel != SendChannel)
                        break;
                    channel ??= await channelTask.ConfigureAwait(false);
                    await channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) {
                Log.LogError(e, "{ClientId}: error", ClientId);
            }
        });
    }

    protected virtual Channel<BridgeMessage> CreateSendChannel()
        => Channel.CreateUnbounded<BridgeMessage>(
            new UnboundedChannelOptions() {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = false,
            });

    protected void Send(BridgeMessage message)
    {
        if (message is ReplicatorRequest rr)
            rr.ReplicatorId = Replicator.Id;
        SendChannel.Writer.WriteAsync(message);
    }
}
