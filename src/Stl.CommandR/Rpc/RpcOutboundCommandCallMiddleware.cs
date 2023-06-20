using Stl.Rpc.Infrastructure;

namespace Stl.CommandR.Rpc;

public class RpcOutboundCommandCallMiddleware : RpcOutboundMiddleware
{
    public const int DefaultPriority = 10;

    public int ConnectionTimeoutMs { get; set; } = 1000;
    public int TimeoutMs { get; set; } = 10_000;

    public RpcOutboundCommandCallMiddleware(IServiceProvider services)
        : base(services)
        => Priority = DefaultPriority;

    public override void PrepareCall(RpcOutboundContext context)
    {
        var methodDef = context.MethodDef;
        var remoteParameterTypes = methodDef!.RemoteParameterTypes;
        if (remoteParameterTypes.Length != 1)
            return;

        if (!typeof(ICommand).IsAssignableFrom(remoteParameterTypes[0]))
            return;

        var call = context.Call!;
        call.ConnectTimeoutMs = ConnectionTimeoutMs;
        call.TimeoutMs = TimeoutMs;
    }
}
