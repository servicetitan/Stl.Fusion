using Stl.Rpc.Infrastructure;

namespace Stl.CommandR.Rpc;

public class RpcOutboundCommandCallMiddleware : RpcOutboundMiddleware
{
    public static TimeSpan DefaultConnectTimeout { get; set; } = TimeSpan.FromSeconds(1.5);
    public static TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public int DefaultPriority { get; set; } = 10;

    public TimeSpan ConnectTimeout { get; set; } = DefaultConnectTimeout;
    public TimeSpan Timeout { get; set; } = DefaultTimeout;

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
        call.ConnectTimeout = ConnectTimeout;
        call.Timeout = Timeout;
    }
}
