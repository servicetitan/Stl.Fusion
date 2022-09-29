using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
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

    protected readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(15);
    protected readonly TimeSpan DisposeTimeout = TimeSpan.FromSeconds(60);

    protected readonly IServiceProvider Services;
    protected IReplicatorImpl ReplicatorImpl => (IReplicatorImpl) Replicator;
    protected readonly HashSet<Symbol> Subscriptions;
    protected volatile Task<Channel<BridgeMessage>> ChannelTask;
    protected int Version;

    protected Symbol ClientId => Replicator.Id;
    protected readonly MomentClockSet Clocks;
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public readonly IReplicator Replicator;
    public readonly Symbol PublisherId;
    public readonly IMutableState<bool> IsConnected;

    public ReplicatorChannelProcessor(IReplicator replicator, Symbol publisherId, IServiceProvider services)
    {
        Services = services;
        Clocks = services.Clocks();
        Replicator = replicator;
        PublisherId = publisherId;
        Subscriptions = new HashSet<Symbol>();
        var stateFactory = Replicator.Services.StateFactory();
        IsConnected = stateFactory.NewMutable(true);
        // ReSharper disable once VirtualMemberCallInConstructor
        ChannelTask = Reconnect(null, StopToken);
    }

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        var channel = (Channel<BridgeMessage>) null!;
        while (true) {
            try {
                channel ??= await ChannelTask.ConfigureAwait(false);
                var reply = await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                await OnReply(reply, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException) {
                if (e is AggregateException ae)
                    e = ae.Flatten().InnerExceptions.SingleOrDefault() ?? e;
                if (e is OperationCanceledException)
                    ExceptionDispatchInfo.Capture(e).Throw();

                channel = null;
#pragma warning disable CS4014
                var reconnectTask = Reconnect(e, cancellationToken);
                Interlocked.Exchange(ref ChannelTask, reconnectTask);
#pragma warning restore CS4014
            }
        }
    }

    protected override Task OnStopping()
    {
        Log.LogInformation("{ClientId}: stopping...", ClientId);
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
        return Task.CompletedTask;
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
            await Clocks.CpuClock.Delay(DisposeTimeout, cancellationToken).ConfigureAwait(false);
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

    protected virtual async Task<Channel<BridgeMessage>> Reconnect(
        Exception? error, CancellationToken cancellationToken)
    {
        try {
            if (error != null)
                IsConnected.Error = error;
            else
                IsConnected.Value = false;

            if (error != null) {
                Log.LogError(error, "{ClientId}: error", ClientId);
                var delay = Replicator.Options.ReconnectDelay.Next();
                await Clocks.CpuClock.Delay(delay, cancellationToken).ConfigureAwait(false);
                Log.LogInformation("{ClientId}: reconnecting...", ClientId);
            }
            else
                Log.LogInformation("{ClientId}: connecting...", ClientId);

            var channel = await ReplicatorImpl.ChannelProvider
                .CreateChannel(PublisherId, cancellationToken)
                .ConfigureAwait(false);
            var message = await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (message is not WelcomeReply { IsAccepted: true })
                throw Errors.WrongPublisher();

            IsConnected.Value = true;
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
            return channel;
        }
        catch (PublisherException) {
            Log.LogInformation(
                "{ClientId}: publisher '{PublisherId}' is unavailable at the destination server",
                ClientId, PublisherId);
            _ = DisposeAsync();
            throw;
        }
    }

    protected async Task Send(BridgeMessage message, CancellationToken cancellationToken)
    {
        if (StopToken.IsCancellationRequested) // Disposing
            return;
        if (message is ReplicatorRequest rr)
            rr.ReplicatorId = Replicator.Id;
        try {
            var channel = await ChannelTask
                .WaitAsync(Clocks.CpuClock, SendTimeout, cancellationToken)
                .ConfigureAwait(false);
            await channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogDebug(e, "Send failed");
            throw;
        }
    }
}
