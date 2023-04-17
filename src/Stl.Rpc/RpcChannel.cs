using Stl.Interception;
using Stl.Internal;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public class RpcChannel : Channel<RpcRequest>
{
    public Symbol Name { get; init; }
    public RpcChannelOptions Options { get; private set; } = null!;

    public IServiceProvider Services { get; }
    public ImmutableArray<RpcMiddleware> Middlewares { get; private set; } = ImmutableArray<RpcMiddleware>.Empty;
    protected ILogger Log { get; }

    public RpcChannel(IServiceProvider services)
    {
        Services = services;
        Log = services.LogFor(GetType());
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

        // Resolving middlewares
        Middlewares = Options.Middlewares
            .Select(type => (RpcMiddleware)Services.GetRequiredService(type))
            .ToImmutableArray();

        return this;
    }

    public async Task ProcessRequests(CancellationToken cancellationToken)
    {
        if (Options == null)
            throw Errors.NotInitialized();

        await foreach (var request in Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            await ProcessRequest(request, cancellationToken).ConfigureAwait(false);
    }

    // Private methods

    private async Task ProcessRequest(
        RpcRequest request,
        CancellationToken cancellationToken)
    {
        var context = new RpcContext(this, request);
        try {
            await context.InvokeNextMiddleware(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogError(e, "One of RpcMiddlewares failed");
        }
    }
}
