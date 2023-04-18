using Stl.Internal;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public class RpcChannel : Channel<RpcRequest>
{
    public Symbol Name { get; init; }
    public RpcGlobalOptions GlobalOptions { get; private set; } = null!;
    public RpcChannelOptions Options { get; private set; } = null!;

    public IServiceProvider Services { get; }
    public RpcServiceRegistry ServiceRegistry { get; }
    public RpcRequestBinder RequestBinder { get; }
    public RpcRequestHandler RequestHandler { get; }
    public RpcOutboundCallTracker OutboundCalls { get; private set; }

    protected ILogger Log { get; }

    public RpcChannel(IServiceProvider services)
    {
        Services = services;
        Log = services.LogFor(GetType());

        GlobalOptions = services.GetRequiredService<RpcGlobalOptions>();
        ServiceRegistry = services.GetRequiredService<RpcServiceRegistry>();
        RequestBinder = services.GetRequiredService<RpcRequestBinder>();
        RequestHandler = services.GetRequiredService<RpcRequestHandler>();
        OutboundCalls = services.GetRequiredService<RpcOutboundCallTracker>();
    }

    public RpcChannel BindTo(Channel<RpcRequest> channel)
        => BindTo(channel.Reader, channel.Writer);

    public virtual RpcChannel BindTo(ChannelReader<RpcRequest> reader, ChannelWriter<RpcRequest> writer)
    {
        if (Options != null)
            throw Errors.AlreadyInitialized();

        Reader = reader;
        Writer = writer;

        // Applying configurators
        var optionsProvider = Services.GetRequiredService<Func<RpcChannel, RpcChannelOptions>>();
        Options = optionsProvider.Invoke(this);

        return this;
    }

    public async Task Serve(CancellationToken cancellationToken)
    {
        if (Options == null)
            throw Errors.NotInitialized();

        await foreach (var request in Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            await HandleRequest(request, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask SendRequest(RpcBoundRequest boundRequest, CancellationToken cancellationToken)
    {
        var request = RequestBinder.FromBound(boundRequest, this);
        return Writer.WriteAsync(request, cancellationToken);
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
