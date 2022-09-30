using System.Runtime.ExceptionServices;
using Stl.Extensibility;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge.Internal;

public class ReplicatorChannelProcessor : WorkerBase
{
    protected static readonly HandlerProvider<(ReplicatorChannelProcessor, CancellationToken), Task> OnPublicationStateReplyHandlers =
        new(typeof(PublicationStateReplyHandler<>));

    protected class PublicationStateReplyHandler<T> : HandlerProvider<(ReplicatorChannelProcessor, CancellationToken), Task>.IHandler<T>
    {
        public Task Handle(object target, (ReplicatorChannelProcessor, CancellationToken) arg)
            => arg.Item1.OnPublicationStateReply((PublicationStateReply<T>) target, arg.Item2);
    }

    protected readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(15);
    protected readonly TimeSpan DisposeTimeout = TimeSpan.FromSeconds(60);

    protected readonly IServiceProvider Services;
    protected IReplicatorImpl ReplicatorImpl => (IReplicatorImpl) Replicator;
    protected readonly HashSet<Symbol> Subscriptions;
    protected int Version;

    protected Symbol ClientId => Replicator.Id;
    protected ILogger Log { get; }

    public readonly IReplicator Replicator;
    public readonly Symbol PublisherId;
    public readonly Connector<Channel<BridgeMessage>> Connector;
    public IMomentClock Clock => Connector.Clock;

    public ReplicatorChannelProcessor(IReplicator replicator, Symbol publisherId, IServiceProvider services)
    {
        Log = services.LogFor(GetType());
        Services = services;
        Replicator = replicator;
        PublisherId = publisherId;
        Subscriptions = new HashSet<Symbol>();
        Connector = new Connector<Channel<BridgeMessage>>(Connect, Services) {
            Connected = OnConnected,
            Disconnected = OnDisconnected,
            RetryDelays = Replicator.Options.ReconnectDelays,
            Log = Log,
            LogTag = $"Replicator '{Replicator.Id}' -> Publisher '{PublisherId}'",
            LogLevel = LogLevel.Information,
        };
    }

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        Connector.Start();
        var channel = (Channel<BridgeMessage>) null!;
        while (true) {
            try {
                channel ??= await Connector.GetConnection(cancellationToken).ConfigureAwait(false);
                var reply = await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                await OnReply(reply, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException) {
                if (e is AggregateException ae)
                    e = ae.GetBaseException();
                if (e is OperationCanceledException)
                    ExceptionDispatchInfo.Capture(e).Throw();

                if (channel != null) {
                    Connector.DropConnection(channel, e);
                    channel = null;
                }
            }
        }
    }

    protected override async Task OnStopping()
    {
        var hasSubscriptions = true;
        while (hasSubscriptions) {
            var publicationIds = GetSubscriptions();
            hasSubscriptions = publicationIds.Count != 0;
            foreach (var publicationId in publicationIds) {
                var publicationRef = new PublicationRef(PublisherId, publicationId);
                var replica = Replicator.Get(publicationRef);
                if (replica != null)
                    replica.Dispose();
                else {
                    lock (Lock) {
                        Subscriptions.Remove(publicationId);
                    }
                }
            }
        }
        await Connector.DisposeAsync().ConfigureAwait(false);
    }

    public virtual void Subscribe(Replica replica, PublicationStateInfo state)
    {
        // No checks, since they're done by the only caller of this method
        var publicationId = replica.PublicationRef.PublicationId;
        lock (Lock) {
            if (Subscriptions.Add(publicationId))
                Version++;
            _ = Send(new SubscribeRequest() {
                PublisherId = PublisherId,
                PublicationId = publicationId,
                Version = state.Version,
                IsConsistent = state.IsConsistent,
            }, StopToken);
        }
    }

    public virtual void Unsubscribe(Replica replica)
    {
        // No checks, since they're done by the only caller of this method
        var publicationId = replica.PublicationRef.PublicationId;
        lock (Lock) {
            Subscriptions.Remove(publicationId);
            if (Subscriptions.Count == 0)
                StartDelayedDispose(Version, StopToken);
            _ = Send(new UnsubscribeRequest() {
                PublisherId = PublisherId,
                PublicationId = publicationId,
            }, StopToken);
        }
    }

    // Protected methods

    protected void StartDelayedDispose(int disposeVersion, CancellationToken cancellationToken)
    {
        Task.Run(async () => {
            await Clock.Delay(DisposeTimeout, cancellationToken).ConfigureAwait(false);
            lock (Lock) {
                if (Version == disposeVersion)
                    _ = DisposeAsync();
            }
        }, cancellationToken);
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
            var replica = Replicator.Get((PublisherId, pam.PublicationId));
            replica?.Dispose();
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

        var replica = Replicator.Get(psi.PublicationRef);
        replica?.UpdateUntyped(psi);
        return Task.CompletedTask;
    }

    protected virtual async Task<Channel<BridgeMessage>> Connect(CancellationToken cancellationToken)
    {
        var channel = await ReplicatorImpl.ChannelProvider
            .CreateChannel(PublisherId, cancellationToken)
            .ConfigureAwait(false);

        var welcomeReply = await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (welcomeReply is not WelcomeReply { IsAccepted: true })
            throw Errors.WrongPublisher();

        return channel;
    }

    protected virtual Task OnConnected(Channel<BridgeMessage> channel, CancellationToken cancellationToken)
    {
        foreach (var publicationId in GetSubscriptions()) {
            var publicationRef = new PublicationRef(PublisherId, publicationId);
            var replica = Replicator.Get(publicationRef);
            if (replica != null)
                _ = replica.RequestUpdateUntyped(true);
            else {
                lock (Lock) {
                    Subscriptions.Remove(publicationId);
                }
            }
        }
        return Task.CompletedTask;
    }

    protected virtual Task OnDisconnected(Channel<BridgeMessage>? channel, Exception? error, CancellationToken cancellationToken)
    {
        if (error is PublisherException) // Wrong publisher
            _ = DisposeAsync();
        return Task.CompletedTask;
    }

    protected async Task Send(BridgeMessage message, CancellationToken cancellationToken)
    {
        if (StopToken.IsCancellationRequested) // Disposing
            return;
        if (message is ReplicatorRequest rr)
            rr.ReplicatorId = Replicator.Id;

        var channel = await Connector.GetConnection(cancellationToken).ConfigureAwait(false);
        try {
            await channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Connector.DropConnection(channel, e);
            throw;
        }
    }
}
