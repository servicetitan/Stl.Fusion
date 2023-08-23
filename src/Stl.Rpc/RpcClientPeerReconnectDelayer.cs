using Cysharp.Text;
using Stl.Net;

namespace Stl.Rpc;

public class RpcClientPeerReconnectDelayer : RetryDelayer, IHasServices
{
    private RpcHub? _hub;
    private ILogger? _log;

    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public IServiceProvider Services { get; }
    public RpcHub Hub => _hub ??= Services.RpcHub();

    public RpcClientPeerReconnectDelayer(IServiceProvider services)
    {
        Services = services;
        Clock = Services.Clocks().CpuClock;
        Delays = RetryDelaySeq.Exp(1, 60);
    }

    public virtual RetryDelay GetDelay(
        RpcClientPeer peer, int tryIndex, Exception? lastError,
        CancellationToken cancellationToken = default)
    {
        var delayLogger = new RetryDelayLogger("reconnect", ZString.Concat('\'', peer.Ref, '\''), Log);
        return this.GetDelay(tryIndex, delayLogger, cancellationToken);
    }
}
