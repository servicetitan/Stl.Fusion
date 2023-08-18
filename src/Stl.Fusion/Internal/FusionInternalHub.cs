using Stl.CommandR.Interception;
using Stl.Fusion.Interception;
using Stl.Rpc;
using Stl.Versioning;

namespace Stl.Fusion.Internal;

public sealed class FusionInternalHub(IServiceProvider services) : IHasServices
{
    private VersionGenerator<LTag>? _lTagVersionGenerator;
    private VersionGenerator<long>? _longVersionGenerator;
    private CommandServiceInterceptor? _commandServiceInterceptor;
    private ComputeServiceInterceptor? _computeServiceInterceptor;

    public IServiceProvider Services { get; } = services;
    public MomentClockSet Clocks { get; } = services.Clocks();
    public ComputedOptionsProvider ComputedOptionsProvider { get; } = services.GetRequiredService<ComputedOptionsProvider>();

    public VersionGenerator<LTag> LTagVersionGenerator
        => _lTagVersionGenerator ??= services.GetRequiredService<VersionGenerator<LTag>>();
    public VersionGenerator<long> LongVersionGenerator
        => _longVersionGenerator ??= services.GetRequiredService<VersionGenerator<long>>();

    public CommandServiceInterceptor CommandServiceInterceptor
        => _commandServiceInterceptor ??= Services.GetRequiredService<CommandServiceInterceptor>();
    public ComputeServiceInterceptor ComputeServiceInterceptor
        => _computeServiceInterceptor ??= Services.GetRequiredService<ComputeServiceInterceptor>();

    public ConcurrentDictionary<Symbol, RpcPeer> Peers { get; } = new();
}
