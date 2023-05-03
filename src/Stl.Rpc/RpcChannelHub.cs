namespace Stl.Rpc;

public class RpcChannelHub : WorkerBase, IHasServices
{
    private Func<Symbol, RpcChannel> ChannelFactory { get; }

    public IServiceProvider Services { get; }
    public ConcurrentDictionary<Symbol, (RpcChannel Channel, Task ServeTask)> Channels { get; } = new();

    public RpcChannel this[Symbol name] => Channels.GetOrAdd(name, CreateChannel).Channel;

    public RpcChannelHub(IServiceProvider services)
        : this(services, true) { }
    protected RpcChannelHub(IServiceProvider services, bool mustStart)
    {
        Services = services;
        ChannelFactory = services.GetRequiredService<Func<Symbol, RpcChannel>>();
        if (mustStart)
            this.Start();
    }

    protected virtual (RpcChannel Channel, Task ServeTask) CreateChannel(Symbol name)
    {
        var channel = ChannelFactory.Invoke(name);
        var cancellationToken = StopToken;
        var serveTask = channel.Serve(cancellationToken);
        _ = serveTask.ContinueWith(_ => Channels.TryRemove(name, (channel, serveTask)),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return (channel, serveTask);
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        using var d = cancellationToken.ToTask();
        await d.Resource.SuppressCancellationAwait(false);
    }
}
