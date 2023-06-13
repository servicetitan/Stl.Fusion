using Stl.CommandR.Interception;
using Stl.Fusion.Interception;
using Stl.Rpc;
using Stl.Versioning;

namespace Stl.Fusion.Internal;

public sealed class FusionInternalHub : IHasServices
{
    private CommandServiceInterceptor? _commandServiceInterceptor;
    private ComputeServiceInterceptor? _computeServiceInterceptor;

    public IServiceProvider Services { get; }
    public MomentClockSet Clocks { get; }
    public VersionGenerator<LTag> LTagVersionGenerator { get; }
    public VersionGenerator<long> LongVersionGenerator { get; }
    public ComputedOptionsProvider ComputedOptionsProvider { get; }
    public CommandServiceInterceptor CommandServiceInterceptor
        => _commandServiceInterceptor ??= Services.GetRequiredService<CommandServiceInterceptor>();
    public ComputeServiceInterceptor ComputeServiceInterceptor
        => _computeServiceInterceptor ??= Services.GetRequiredService<ComputeServiceInterceptor>();

    public ConcurrentDictionary<Symbol, RpcPeer> Peers { get; } = new();

    public FusionInternalHub(IServiceProvider services)
    {
        Services = services;
        Clocks = services.Clocks();
        LTagVersionGenerator = services.GetRequiredService<VersionGenerator<LTag>>();
        LongVersionGenerator = services.GetRequiredService<VersionGenerator<long>>();
        ComputedOptionsProvider = services.GetRequiredService<ComputedOptionsProvider>();
    }
}
