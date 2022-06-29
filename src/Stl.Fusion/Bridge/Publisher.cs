using Stl.Channels;
using Stl.Concurrency;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Internal;
using Stl.OS;
using Stl.Generators;

namespace Stl.Fusion.Bridge;

public interface IPublisher : IHasId<Symbol>, IHasServices
{
    PublisherOptions Options { get; }
    IChannelHub<BridgeMessage> ChannelHub { get; }
    MomentClockSet Clocks { get; }

    IPublication Publish(IComputed computed);
    IPublication? Get(Symbol publicationId);
    ValueTask Subscribe(
        Channel<BridgeMessage> channel, IPublication publication,
        bool isUpdateRequested, CancellationToken cancellationToken = default);
    ValueTask Unsubscribe(
        Channel<BridgeMessage> channel, IPublication publication,
        CancellationToken cancellationToken = default);
}

public interface IPublisherImpl : IPublisher
{
    void OnPublicationDisposed(IPublication publication);
    void OnChannelProcessorDisposed(PublisherChannelProcessor channelProcessor);
}

public record PublisherOptions
{
    public static Symbol NewId() => "P-" + RandomStringGenerator.Default.Next();

    public Symbol Id { get; init; } = NewId();
    public IChannelHub<BridgeMessage> ChannelHub { get; init; } = new ChannelHub<BridgeMessage>();
    public bool OwnsChannelHub { get; init; } = true;
    public MomentClockSet? Clocks { get; init; } = null;

    public IPublicationFactory PublicationFactory { get; init; } = Internal.PublicationFactory.Instance;
    public Type PublicationGeneric { get; init; } = typeof(Publication<>);
    public TimeSpan PublicationExpirationTime { get; init; } = TimeSpan.FromSeconds(60);
    public Generator<Symbol> PublicationIdGenerator { get; init; } = new RandomSymbolGenerator("p-");

    public ISubscriptionProcessorFactory SubscriptionProcessorFactory { get; init; } = Internal.SubscriptionProcessorFactory.Instance;
    public Type SubscriptionProcessorGeneric { get; init; } = typeof(SubscriptionProcessor<>);
    public TimeSpan SubscriptionExpirationTime { get; init; } = TimeSpan.FromSeconds(60);
}

public class Publisher : SafeAsyncDisposableBase, IPublisherImpl
{
    protected ConcurrentDictionary<ComputedInput, IPublication> Publications { get; }
    protected ConcurrentDictionary<Symbol, IPublication> PublicationsById { get; }
    protected ConcurrentDictionary<Channel<BridgeMessage>, PublisherChannelProcessor> ChannelProcessors { get; }
    protected ChannelAttachedHandler<BridgeMessage> OnChannelAttachedHandler { get; }
    protected ChannelDetachedHandler<BridgeMessage> OnChannelDetachedHandler { get; }

    public PublisherOptions Options { get; }
    public Symbol Id { get; }
    public IChannelHub<BridgeMessage> ChannelHub { get; }
    public IServiceProvider Services { get; }
    public MomentClockSet Clocks { get; }

    public Publisher(PublisherOptions options, IServiceProvider services)
    {
        Options = options;
        Services = services;
        Id = options.Id;
        ChannelHub = options.ChannelHub;
        Clocks = options.Clocks ?? Services.Clocks();

        var concurrencyLevel = HardwareInfo.GetProcessorCountPo2Factor(4);
        var capacity = OSInfo.IsWebAssembly ? 509 : 7919;

        Publications = new ConcurrentDictionary<ComputedInput, IPublication>(concurrencyLevel, capacity);
        PublicationsById = new ConcurrentDictionary<Symbol, IPublication>(concurrencyLevel, capacity);
        ChannelProcessors = new ConcurrentDictionary<Channel<BridgeMessage>, PublisherChannelProcessor>(concurrencyLevel, capacity);

        OnChannelAttachedHandler = OnChannelAttached;
        OnChannelDetachedHandler = OnChannelDetachedAsync;
        ChannelHub.Detached += OnChannelDetachedHandler; // Must go first
        ChannelHub.Attached += OnChannelAttachedHandler;
    }

    public virtual IPublication Publish(IComputed computed)
    {
        this.ThrowIfDisposedOrDisposing();
        var spinWait = new SpinWait();
        while (true) {
             var p = Publications.GetOrAddChecked(
                 computed.Input,
                 (key, arg) => {
                     var (this1, computed1) = arg;
                     var id = this1.Options.PublicationIdGenerator.Next();
                     var p1 = this1.Options.PublicationFactory.Create(
                         this1.Options.PublicationGeneric, this1, computed1, id,
                         this1.Clocks.CoarseCpuClock);
                     this1.PublicationsById[id] = p1;
                     p1.Run();
                     return p1;
                 }, (this, computed));
            if (p.Touch())
                return p;
            spinWait.SpinOnce();
        }
    }

    public virtual IPublication? Get(Symbol publicationId)
        => PublicationsById.TryGetValue(publicationId, out var p) ? p : null;

    public virtual ValueTask Subscribe(
        Channel<BridgeMessage> channel, IPublication publication,
        bool isUpdateRequested, CancellationToken cancellationToken = default)
    {
        this.ThrowIfDisposedOrDisposing();
        if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
            throw Errors.UnknownChannel(channel);
        if (publication.Publisher != this)
            throw Errors.WrongPublisher(this, publication.Publisher.Id);
        var message = new SubscribeRequest() {
            PublisherId = Id,
            PublicationId = publication.Id,
            IsUpdateRequested = isUpdateRequested,
        };
        return channelProcessor.OnReplicaRequest(message, cancellationToken);
    }

    public virtual ValueTask Unsubscribe(
        Channel<BridgeMessage> channel, IPublication publication,
        CancellationToken cancellationToken = default)
    {
        if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
            return ValueTaskExt.CompletedTask;
        return channelProcessor.Unsubscribe(publication, cancellationToken);
    }

    void IPublisherImpl.OnPublicationDisposed(IPublication publication)
        => OnPublicationDisposed(publication);
    protected virtual void OnPublicationDisposed(IPublication publication)
    {
        if (publication.Publisher != this)
            throw new ArgumentOutOfRangeException(nameof(publication));
        if (!PublicationsById.TryGetValue(publication.Id, out var p))
            return;
        Publications.TryRemove(publication.State.Computed.Input, p);
        PublicationsById.TryRemove(p.Id, p);
    }

    void IPublisherImpl.OnChannelProcessorDisposed(PublisherChannelProcessor channelProcessor)
        => OnChannelProcessorDisposed(channelProcessor);
    protected virtual void OnChannelProcessorDisposed(PublisherChannelProcessor channelProcessor)
    { }

    // Channel-related

    protected virtual PublisherChannelProcessor CreateChannelProcessor(Channel<BridgeMessage> channel)
        => new(this, channel, Services);

    protected virtual void OnChannelAttached(Channel<BridgeMessage> channel)
    {
        var channelProcessor = CreateChannelProcessor(channel);
        if (!ChannelProcessors.TryAdd(channel, channelProcessor))
            return;
        channelProcessor.Run().ContinueWith(_ => {
            // Since ChannelProcessor is AsyncProcessorBase desc.,
            // its disposal will shut down Run as well,
            // so "subscribing" to Run completion is the
            // same as subscribing to its disposal.
            ChannelProcessors.TryRemove(channel, channelProcessor);
        }, TaskScheduler.Default);
    }

    protected virtual void OnChannelDetachedAsync(
        Channel<BridgeMessage> channel, ref ArrayBuffer<ValueTask> taskCollector)
    {
        if (!ChannelProcessors.TryGetValue(channel, out var channelProcessor))
            return;
        taskCollector.Add(channelProcessor.DisposeAsync());
    }

    protected override async Task DisposeAsync(bool disposing)
    {
        if (!disposing) return;

        ChannelHub.Attached -= OnChannelAttachedHandler; // Must go first
        ChannelHub.Detached -= OnChannelDetachedHandler;
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
        var publications = PublicationsById;
        while (!publications.IsEmpty) {
            var tasks = publications
                .Take(HardwareInfo.GetProcessorCountFactor(4, 4))
                .ToList()
                .Select(p => {
                    var (_, publication) = (p.Key, p.Value);
                    return publication.DisposeAsync().AsTask();
                });
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        if (Options.OwnsChannelHub)
            await ChannelHub.DisposeAsync().ConfigureAwait(false);
    }
}
