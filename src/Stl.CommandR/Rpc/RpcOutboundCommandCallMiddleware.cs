using Stl.Rpc.Infrastructure;

namespace Stl.CommandR.Rpc;

public class RpcOutboundCommandCallMiddleware : RpcOutboundMiddleware
{
    public const int DefaultPriority = 10;

    public int ConnectTimeoutMs { get; set; } = 1500;
    public int TimeoutMs { get; set; } = 10_000;

    public RpcOutboundCommandCallMiddleware(IServiceProvider services)
        : base(services)
        => Priority = DefaultPriority;

    public override void PrepareCall(RpcOutboundContext context)
    {
        var methodDef = context.MethodDef;
        var parameterTypes = methodDef!.ParameterTypes;
        if (parameterTypes.Length is < 1 or > 2)
            return;

        if (!typeof(ICommand).IsAssignableFrom(parameterTypes[0]))
            return;

        var call = context.Call!;
        call.ConnectTimeoutMs = ConnectTimeoutMs;
        call.TimeoutMs = TimeoutMs;
    }
}
