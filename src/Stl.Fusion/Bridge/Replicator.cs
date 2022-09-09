using Stl.Concurrency;
using Stl.Fusion.Bridge.Internal;
using Stl.Generators;

namespace Stl.Fusion.Bridge;

public interface IReplicator : IHasId<Symbol>, IHasServices
{
    ReplicatorOptions Options { get; }

    Replica? Get(PublicationRef publicationRef);
    Replica<T> GetOrAdd<T>(
        ComputedOptions computedOptions, PublicationStateInfo<T> publicationStateInfo, bool requestUpdate = false);

    IState<bool> GetPublisherConnectionState(Symbol publisherId);
}

public interface IReplicatorImpl : IReplicator
{
    IChannelProvider ChannelProvider { get; }

    void Subscribe(Replica replica);
    void OnReplicaDisposed(Replica replica);
}

public record ReplicatorOptions
{
    public static Symbol NewId() => "R-" + RandomStringGenerator.Default.Next();

    public Symbol Id { get; init; } = NewId();
    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(10);
    public IChannelProvider? ChannelProvider { get; init; }
}

public class Replicator : SafeAsyncDisposableBase, IReplicatorImpl
{
    protected ConcurrentDictionary<Symbol, ReplicatorChannelProcessor> ChannelProcessors { get; }
    protected Func<Symbol, ReplicatorChannelProcessor> CreateChannelProcessorHandler { get; }

    public ReplicatorOptions Options { get; }
    public Symbol Id { get; }
    public IServiceProvider Services { get; }
    public IChannelProvider ChannelProvider { get; }

    public Replicator(ReplicatorOptions options, IServiceProvider services)
    {
        Options = options;
        Id = Options.Id;
        Services = services;
        ChannelProvider = options.ChannelProvider ?? Services.GetRequiredService<IChannelProvider>();

        ChannelProcessors = new ConcurrentDictionary<Symbol, ReplicatorChannelProcessor>();
        CreateChannelProcessorHandler = CreateChannelProcessor;
    }

    public Replica? Get(PublicationRef publicationRef)
        => ReplicaRegistry.Instance.Get(publicationRef);

    public Replica<T> GetOrAdd<T>(
        ComputedOptions computedOptions, PublicationStateInfo<T> publicationStateInfo, bool requestUpdate = false)
    {
        var (replica, isNew) = ReplicaRegistry.Instance.GetOrRegister(publicationStateInfo.PublicationRef,
            () => new Replica<T>(computedOptions, publicationStateInfo, this, requestUpdate));
        if (isNew)
            Subscribe(replica);
        return (Replica<T>) replica;
    }

    public IState<bool> GetPublisherConnectionState(Symbol publisherId)
        => GetChannelProcessor(publisherId).IsConnected;

    protected virtual ReplicatorChannelProcessor GetChannelProcessor(Symbol publisherId)
        => ChannelProcessors
            .GetOrAddChecked(publisherId, CreateChannelProcessorHandler);

    protected virtual ReplicatorChannelProcessor CreateChannelProcessor(Symbol publisherId)
    {
        var channelProcessor = new ReplicatorChannelProcessor(this, publisherId, Services);
        channelProcessor.Run().ContinueWith(_ => {
            // Since ChannelProcessor is WorkerBase desc.,
            // its disposal will shut down Run as well,
            // so "subscribing" to Run completion is the
            // same as subscribing to its disposal.
            ChannelProcessors.TryRemove(publisherId, channelProcessor);
        }, default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return channelProcessor;
    }

    void IReplicatorImpl.Subscribe(Replica replica)
        => Subscribe(replica);
    protected virtual void Subscribe(Replica replica)
    {
        if (replica.Replicator != this)
            throw new ArgumentOutOfRangeException(nameof(replica));
        GetChannelProcessor(replica.PublicationRef.PublisherId).Subscribe(replica);
    }

    void IReplicatorImpl.OnReplicaDisposed(Replica replica)
        => OnReplicaDisposed(replica);
    protected virtual void OnReplicaDisposed(Replica replica)
    {
        if (WhenDisposed != null)
            return;
        if (replica.Replicator != this)
            throw new ArgumentOutOfRangeException(nameof(replica));
        GetChannelProcessor(replica.PublicationRef.PublisherId).Unsubscribe(replica);
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
