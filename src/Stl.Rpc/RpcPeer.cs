using Stl.Interception;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public class RpcPeer : WorkerBase
{
    private ILogger? _log;
    private IMomentClock? _clock;
    private volatile AsyncEvent<Channel<RpcMessage>?> _whenConnected = new(null, true);

    protected IServiceProvider Services => Hub.Services;
    protected ILogger Log => _log ??= Services.LogFor(GetType());
    protected IMomentClock Clock => _clock ??= Services.Clocks().CpuClock;

    public RpcHub Hub { get; }
    public Symbol Name { get; init; }
    public Func<ArgumentList, Type, object?> ArgumentSerializer { get; init; }
    public Func<object?, Type, ArgumentList> ArgumentDeserializer { get; init; }
    public Func<RpcServiceDef, bool> LocalServiceFilter { get; init; }
    public RetryDelaySeq ReconnectDelays { get; init; } = new();

    public RpcPeer(RpcHub hub, Symbol name)
    {
        Hub = hub;
        Name = name;
        ArgumentSerializer = Hub.Configuration.ArgumentSerializer;
        ArgumentDeserializer = Hub.Configuration.ArgumentDeserializer;
        LocalServiceFilter = static _ => true;
    }

    public ValueTask Send(RpcBoundRequest boundRequest, CancellationToken cancellationToken)
    {
        var request = Hub.RequestBinder.FromBound(this, boundRequest);
        return Send(request, cancellationToken);
    }

    public async ValueTask Send(RpcMessage message, CancellationToken cancellationToken)
    {
        var channel = await GetConnection(cancellationToken).ConfigureAwait(false);
        await channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }

    // Protected methods

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var tryIndex = 0;
        while (true) {
            try {
                if (tryIndex > 0)
                    await Clock.Delay(ReconnectDelays[tryIndex], cancellationToken).ConfigureAwait(false);
                var channel = await Connect(cancellationToken).ConfigureAwait(false);
                tryIndex = 0;
                await foreach (var request in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                    await HandleRequest(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException) {
                DropConnection();
                if (tryIndex == 0)
                    Log.LogError(e, "'{Name}': Request processing failed, reconnecting...", Name);
                else
                    Log.LogError(e, "'{Name}': Couldn't connect, retrying (#{TryIndex})...", Name, tryIndex);
                tryIndex++;
            }
        }
    }

    protected async Task<Channel<RpcMessage>> Connect(CancellationToken cancellationToken)
    {
        await Task.Yield();
        throw new NotSupportedException();
    }

    protected void DropConnection()
    {
        lock (Lock)
            _whenConnected = _whenConnected.SetNext(null);
    }

    protected async ValueTask<Channel<RpcMessage>> GetConnection(CancellationToken cancellationToken)
    {
        var whenConnected = _whenConnected;
        while (true) {
            // ReSharper disable once MethodSupportsCancellation
            var whenNext = whenConnected.WhenNext();
            if (!whenNext.IsCompleted && whenConnected.Value != null)
                return whenConnected.Value;

            whenConnected = await whenNext.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    // Private methods

    private async Task HandleRequest(RpcMessage message, CancellationToken cancellationToken)
    {
        var context = new RpcRequestContext(this, message, cancellationToken);
        using var _ = context.Activate();
        try {
            await Hub.RequestHandler.Handle(context).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            Log.LogError(e, "Failed to process request: {Request}", message);
        }
    }
}
