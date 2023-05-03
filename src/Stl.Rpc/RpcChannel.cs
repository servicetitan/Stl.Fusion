using Stl.Interception;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcChannel : Channel<RpcRequest>
{
    private readonly TaskCompletionSource<Unit> _whenBoundSource = TaskCompletionSourceExt.New<Unit>();

    protected ILogger Log { get; }

    public IServiceProvider Services { get; }
    public RpcConfiguration Configuration { get; }
    public RpcServiceRegistry ServiceRegistry { get; }
    public RpcRequestBinder RequestBinder { get; }
    public RpcRequestHandler RequestHandler { get; }
    public RpcOutboundCallTracker OutboundCalls { get; private set; }

    public Symbol Name { get; init; }
    public Func<ArgumentList, Type, object?> ArgumentSerializer { get; init; }
    public Func<object?, Type, ArgumentList> ArgumentDeserializer { get; init; }
    public Func<RpcServiceDef, bool> LocalServiceFilter { get; init; }
    // ReSharper disable once InconsistentlySynchronizedField
    public Task WhenBound => _whenBoundSource.Task;

    public RpcChannel(Symbol name, IServiceProvider services)
    {
        Services = services;
        Log = services.LogFor(GetType());

        Configuration = services.GetRequiredService<RpcConfiguration>();
        ServiceRegistry = services.GetRequiredService<RpcServiceRegistry>();
        RequestBinder = services.GetRequiredService<RpcRequestBinder>();
        RequestHandler = services.GetRequiredService<RpcRequestHandler>();
        OutboundCalls = services.GetRequiredService<RpcOutboundCallTracker>();

        Name = name;
        ArgumentSerializer = Configuration.ArgumentSerializer;
        ArgumentDeserializer = Configuration.ArgumentDeserializer;
        LocalServiceFilter = static serviceDef => serviceDef.ImplementationType != null;
    }

    public RpcChannel BindTo(Channel<RpcRequest> channel)
        => BindTo(channel.Reader, channel.Writer);

    public virtual RpcChannel BindTo(ChannelReader<RpcRequest> reader, ChannelWriter<RpcRequest> writer)
    {
        lock (_whenBoundSource) {
            if (_whenBoundSource.Task.IsCompleted)
                throw Errors.AlreadyBound();

            Reader = reader;
            Writer = writer;
            _whenBoundSource.TrySetResult(default);
        }
        return this;
    }

    public async Task Serve(CancellationToken cancellationToken)
    {
        await WhenBound.ConfigureAwait(false);
        await foreach (var request in Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            await HandleRequest(request, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask SendRequest(RpcBoundRequest boundRequest, CancellationToken cancellationToken)
    {
        if (!WhenBound.IsCompleted)
            await WhenBound.ConfigureAwait(false);

        var request = RequestBinder.FromBound(boundRequest, this);
        await Writer.WriteAsync(request, cancellationToken).ConfigureAwait(false);
    }

    // Private methods

    private async Task HandleRequest(RpcRequest request, CancellationToken cancellationToken)
    {
        var context = new RpcRequestContext(this, request, cancellationToken);
        using var _ = context.Activate();
        try {
            await RequestHandler.Handle(context).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogError(e, "One of RpcMiddlewares failed");
        }
    }
}
