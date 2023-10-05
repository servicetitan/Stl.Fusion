using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Testing;

public class RpcRandomDelayMiddleware(IServiceProvider services) : RpcInboundMiddleware(services)
{
    public RandomTimeSpan Delay { get; set; } = new(0.05, 0.03);
    public Func<RpcInboundCall, TimeSpan>? DelayProvider { get; set; }

    public override Task OnBeforeCall(RpcInboundCall call)
    {
        var delay = DelayProvider is { } delayProvider
            ? delayProvider.Invoke(call)
            : Delay.Next();
        return Task.Delay(delay, call.CancellationToken);
    }
}
