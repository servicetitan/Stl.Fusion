using Stl.Concurrency;
using Stl.Fusion.Bridge.Internal;
using Stl.OS;
using Stl.Generators;

namespace Stl.Fusion.Bridge;

public interface IReplicator : IHasId<Symbol>, IHasServices
{
    ReplicatorOptions Options { get; }

    IReplica? Get(PublicationRef publicationRef);
    IReplica<T> GetOrAdd<T>(PublicationStateInfo<T> publicationStateInfo, bool requestUpdate = false);

    IState<bool> GetPublisherConnectionState(Symbol publisherId);
}

public interface IReplicatorImpl : IReplicator
{
    IChannelProvider ChannelProvider { get; }

    void Subscribe(IReplica replica);
    void OnReplicaDisposed(IReplica replica);
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

    public IReplica? Get(PublicationRef publicationRef)
        => ReplicaRegistry.Instance.Get(publicationRef);

    public IReplica<T> GetOrAdd<T>(PublicationStateInfo<T> publicationStateInfo, bool requestUpdate = false)
    {
        var (replica, isNew) = ReplicaRegistry.Instance.GetOrRegister(publicationStateInfo.PublicationRef,
            () => new Replica<T>(this, publicationStateInfo, requestUpdate));
        if (isNew)
            Subscribe(replica);
        return (IReplica<T>) replica;
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
            // Since ChannelProcessor is AsyncProcessorBase desc.,
            // its disposal will shut down Run as well,
            // so "subscribing" to Run completion is the
            // same as subscribing to its disposal.
            ChannelProcessors.TryRemove(publisherId, channelProcessor);
        }, TaskScheduler.Default);
        return channelProcessor;
    }

    void IReplicatorImpl.Subscribe(IReplica replica)
        => Subscribe(replica);
    protected virtual void Subscribe(IReplica replica)
    {
        if (replica.Replicator != this)
            throw new ArgumentOutOfRangeException(nameof(replica));
        GetChannelProcessor(replica.PublicationRef.PublisherId).Subscribe(replica);
    }

    void IReplicatorImpl.OnReplicaDisposed(IReplica replica)
        => OnReplicaDisposed(replica);
    protected virtual void OnReplicaDisposed(IReplica replica)
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
            var tasks = channelProcessors
                .Take(HardwareInfo.GetProcessorCountFactor(4, 4))
                .ToList()
                .Select(p => {
                    var (_, channelProcessor) = (p.Key, p.Value);
                    return channelProcessor.DisposeAsync().AsTask();
                });
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
