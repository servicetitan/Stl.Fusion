using Stl.Concurrency;
using Stl.Fusion.Bridge.Internal;
using Stl.Generators;

namespace Stl.Fusion.Bridge;

public interface IReplicator : IHasId<Symbol>, IHasServices
{
    ReplicatorOptions Options { get; }

    Replica<T> AddOrUpdate<T>(PublicationStateInfo<T> state);

    IState<bool> GetPublisherConnectionState(Symbol publisherId);
}

public interface IReplicatorImpl : IReplicator
{
    IChannelProvider ChannelProvider { get; }

    ValueTask Subscribe(Replica replica);
    void OnReplicaDisposed(Replica replica);
}

public record ReplicatorOptions
{
    public static Symbol NewId() => "R-" + RandomStringGenerator.Default.Next();

    public Symbol Id { get; init; } = NewId();
    public RetryDelaySeq ReconnectDelays { get; init; } = new(3, 15);
    public IChannelProvider? ChannelProvider { get; init; }
}

public class Replicator : SafeAsyncDisposableBase, IReplicatorImpl
{
    protected readonly TimeSpan SubscribeRetryDelay = TimeSpan.FromMilliseconds(50);

    protected ConcurrentDictionary<Symbol, ReplicatorChannelProcessor> ChannelProcessors { get; }
    protected Func<Symbol, ReplicatorChannelProcessor> CreateChannelProcessorHandler { get; }
    protected IMomentClock Clock { get; }

    public ReplicatorOptions Options { get; }
    public Symbol Id { get; }
    public IServiceProvider Services { get; }
    public IChannelProvider ChannelProvider { get; }

    public Replicator(ReplicatorOptions options, IServiceProvider services)
    {
        Options = options;
        Id = Options.Id;
        Services = services;
        Clock = services.Clocks().CpuClock;
        ChannelProvider = options.ChannelProvider ?? Services.GetRequiredService<IChannelProvider>();

        ChannelProcessors = new ConcurrentDictionary<Symbol, ReplicatorChannelProcessor>();
        CreateChannelProcessorHandler = CreateChannelProcessor;
    }

    public Replica<T> AddOrUpdate<T>(PublicationStateInfo<T> state)
    {
        var (replica, isNew) = ReplicaRegistry.Instance.GetOrRegister(
            state.PublicationRef,
            () => new Replica<T>(state, this));
        if (isNew && state.IsConsistent)
            _ = replica.RequestUpdateUntyped(); // Any new inconsistent replica should subscribe for invalidations
        else
            replica.UpdateUntyped(state); // Otherwise we update it
        return (Replica<T>) replica;
    }

    public IState<bool> GetPublisherConnectionState(Symbol publisherId)
        => GetChannelProcessor(publisherId).Connector.IsConnected;

    protected virtual ReplicatorChannelProcessor GetChannelProcessor(Symbol publisherId)
        => ChannelProcessors
            .GetOrAdd(publisherId, CreateChannelProcessorHandler);

    protected virtual ReplicatorChannelProcessor CreateChannelProcessor(Symbol publisherId)
    {
        var channelProcessor = new ReplicatorChannelProcessor(this, publisherId);
        _ = channelProcessor.Run().ContinueWith(_ => {
            // Since ChannelProcessor is WorkerBase desc.,
            // its disposal will shut down Run as well,
            // so "subscribing" to Run completion is the
            // same as subscribing to its disposal.
            ChannelProcessors.TryRemove(publisherId, channelProcessor);
        }, default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return channelProcessor;
    }

    ValueTask IReplicatorImpl.Subscribe(Replica replica)
        => Subscribe(replica);
    protected virtual async ValueTask Subscribe(Replica replica)
    {
        if (replica.Replicator != this)
            throw new ArgumentOutOfRangeException(nameof(replica));

        while (true) {
            var state = replica.UntypedState;
            if (state == null) // Replica is disposed, so no subscription is needed
                break;

            var channelProcessor = GetChannelProcessor(replica.PublicationRef.PublisherId);
            if (channelProcessor.Subscribe(state))
                break;

            // If we're here, channelProcessor is either disposed or disposing, so we have to retry
            await Clock.Delay(SubscribeRetryDelay).ConfigureAwait(false);
            var whenRunning = channelProcessor.WhenRunning;
            if (whenRunning != null)
                await whenRunning.ConfigureAwait(false);
            var whenDisposed = channelProcessor.WhenDisposed;
            if (whenDisposed != null)
                await whenDisposed.ConfigureAwait(false);
        }
    }

    void IReplicatorImpl.OnReplicaDisposed(Replica replica)
        => OnReplicaDisposed(replica);
    protected virtual void OnReplicaDisposed(Replica replica)
    {
        if (replica.Replicator != this)
            throw new ArgumentOutOfRangeException(nameof(replica));

        if (WhenDisposed != null)
            return;

        if (replica.IsUpdateRequested) // Otherwise it is invalidated / not subscribed anyway
            GetChannelProcessor(replica.PublicationRef.PublisherId).Unsubscribe(replica.PublicationRef.PublicationId);
    }

    protected override async Task DisposeAsync(bool disposing)
    {
        // Intentionally ignore disposing flag here

        var channelProcessors = ChannelProcessors;
        while (!channelProcessors.IsEmpty) {
            await channelProcessors
                .Select(p => {
                    var (_, channelProcessor) = (p.Key, p.Value);
                    return channelProcessor.DisposeAsync().AsTask();
                })
                .Collect()
                .ConfigureAwait(false);
        }
    }
}
