using Stl.Interception;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcConnection : WorkerBase
{
    private readonly TaskCompletionSource<Channel<RpcRequest>> _connectionSource =
        TaskCompletionSourceExt.New<Channel<RpcRequest>>();

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
    public Task<Channel<RpcRequest>> WhenConnected => _connectionSource.Task;

    public RpcConnection(Symbol name, IServiceProvider services)
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

    public RpcConnection Connect(Channel<RpcRequest> channel)
    {
        if (!_connectionSource.TrySetResult(channel))
            throw Errors.AlreadyConnected();
        return this;
    }

    public ValueTask Send(RpcBoundRequest boundRequest, CancellationToken cancellationToken)
    {
        var request = RequestBinder.FromBound(this, boundRequest);
        return Send(request, cancellationToken);
    }

    public async ValueTask Send(RpcRequest request, CancellationToken cancellationToken)
    {
        var channel = await WhenConnected.WaitAsync(cancellationToken).ConfigureAwait(false);
        await channel.Writer.WriteAsync(request, cancellationToken).ConfigureAwait(false);
    }

    // Protected methods

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var channel = await WhenConnected.WaitAsync(cancellationToken).ConfigureAwait(false);
        await foreach (var request in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            await HandleRequest(request, cancellationToken).ConfigureAwait(false);
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
            Log.LogError(e, "Failed to process request: {Request}", request);
        }
    }
}
