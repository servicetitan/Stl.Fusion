using System.Runtime.ExceptionServices;
using Microsoft.Toolkit.HighPerformance;
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

    protected static readonly TimeSpan DelayedDisposeTimeout = TimeSpan.FromSeconds(60);

    protected ILogger Log { get; }
    protected IReplicatorImpl ReplicatorImpl => (IReplicatorImpl) Replicator;
    protected readonly HashSet<Symbol> Subscriptions;

    public readonly IReplicator Replicator;
    public readonly Symbol PublisherId;
    public readonly Connector<Channel<BridgeMessage>> Connector;
    public IMomentClock Clock => Connector.Clock;

    public ReplicatorChannelProcessor(IReplicator replicator, Symbol publisherId)
    {
        var services = replicator.Services;
        Log = services.LogFor(GetType());
        Replicator = replicator;
        PublisherId = publisherId;
        Subscriptions = new HashSet<Symbol>();
        Connector = new Connector<Channel<BridgeMessage>>(Connect, services.StateFactory()) {
            Connected = OnConnected,
            Disconnected = OnDisconnected,
            ReconnectDelays = Replicator.Options.ReconnectDelays,
            Clock = services.Clocks().CpuClock,
            Log = Log,
            LogTag = $"Replicator '{Replicator.Id}' -> Publisher '{PublisherId}'",
            LogLevel = LogLevel.Information,
        };
        StartDelayedDispose();
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
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

    protected override async Task OnStop()
    {
        var replicas = new List<Replica>();
        lock (Lock) {
            foreach (var publicationId in Subscriptions) {
                var publicationRef = new PublicationRef(PublisherId, publicationId);
                if (publicationRef.Resolve() is { } replica)
                    replicas.Add(replica);
            }
            Subscriptions.Clear();
        }

        foreach (var replica in replicas)
            replica.Dispose();
        await Connector.DisposeAsync().ConfigureAwait(false);
    }

    public virtual bool Subscribe(PublicationStateInfo state)
    {
        // No checks, since they're done by the only caller of this method
        var publicationId = state.PublicationRef.PublicationId;
        lock (Lock) {
            if (StopToken.IsCancellationRequested)
                return false;

            if (Subscriptions.Add(publicationId)) {
                if (Subscriptions.Count == 1)
                    DelayedAction.Instances.Remove(new DelayedAction(this, null));
            }
            _ = Send(new SubscribeRequest() {
                PublisherId = PublisherId,
                PublicationId = publicationId,
                Version = state.Version,
                IsConsistent = state.IsConsistent,
            }, StopToken);
        }
        return true;
    }

    public virtual bool Unsubscribe(Symbol publicationId)
    {
        // No checks, since they're done by the only caller of this method
        lock (Lock) {
            if (StopToken.IsCancellationRequested)
                return false;

            if (Subscriptions.Remove(publicationId)) {
                if (Subscriptions.Count == 0)
                    StartDelayedDispose();
            }
            _ = Send(new UnsubscribeRequest() {
                PublisherId = PublisherId,
                PublicationId = publicationId,
            }, StopToken);
        }
        return true;
    }

    // Protected methods

    protected void StartDelayedDispose()
    {
        var disposeAt = DelayedAction.Clock.Now + DelayedDisposeTimeout;
        var delayedAction = new DelayedAction(this, static target => {
            var self = (ReplicatorChannelProcessor) target;
            lock (self.Lock) {
                if (self.WhenDisposed != null)
                    return;
                if (self.Subscriptions.Count != 0)
                    return;

                self.Dispose();
            }
        });
        DelayedAction.Instances.AddOrUpdateToLater(delayedAction, disposeAt);
    }

    protected virtual Task OnReply(BridgeMessage reply, CancellationToken cancellationToken)
    {
        switch (reply) {
        case PublicationStateReply psr:
            // Fast dispatch to OnStateMessage<T>
            return OnPublicationStateReplyHandlers[psr.GetResultType()].Handle(psr, (this, cancellationToken));
        case PublicationAbsentsReply pam:
            var publicationRef = new PublicationRef(PublisherId, pam.PublicationId);
            var replica = publicationRef.Resolve();
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
            reply.Output);

        var replica = psi.PublicationRef.Resolve();
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
        lock (Lock) {
            foreach (var publicationId in Subscriptions) {
                var publicationRef = new PublicationRef(PublisherId, publicationId);
                if (publicationRef.Resolve() is { } replica)
                    _ = replica.RequestUpdateUntyped(true);
                else
                    Subscriptions.Remove(publicationId);
            }
            if (Subscriptions.Count == 0)
                StartDelayedDispose();
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
