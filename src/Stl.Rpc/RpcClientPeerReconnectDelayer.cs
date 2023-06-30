using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcClientPeerReconnectDelayer : RpcServiceBase
{
    private CancellationTokenSource _cancelActiveDelaysCts = new();

    protected object Lock = new();
    protected CancellationToken CancelActiveDelaysToken;

    public RetryDelaySeq ReconnectDelays { get; init; } = new();
    public int? ReconnectRetryLimit { get; init; }
    public IMomentClock Clock { get; init; }

    public RpcClientPeerReconnectDelayer(IServiceProvider services)
        : base(services)
        => Clock = Services.Clocks().CpuClock;

    public virtual void CancelActiveDelays()
    {
        CancellationTokenSource cts;
        lock (Lock) {
            cts = _cancelActiveDelaysCts;
            _cancelActiveDelaysCts = new();
            CancelActiveDelaysToken = _cancelActiveDelaysCts.Token;
        }
        cts.CancelAndDisposeSilently();
    }

    public virtual (Task DelayTask, Moment EndsAt) Delay(
        RpcClientPeer peer,
        int failedTryCount,
        Exception? lastError,
        CancellationToken cancellationToken)
    {
        if (ReconnectRetryLimit is { } limit && failedTryCount >= limit) {
            Log.LogWarning(lastError, "'{PeerRef}': Reconnect retry limit exceeded", peer.Ref);
            throw Errors.ConnectionUnrecoverable();
        }

        var delay = ReconnectDelays[failedTryCount];
        var now = Clock.Now;
        if (failedTryCount == 0 || delay <= TimeSpan.Zero)
            return (Task.CompletedTask, now);

        delay = TimeSpanExt.Max(TimeSpan.FromMilliseconds(1), delay);
        Log.LogInformation(
            "'{PeerRef}': Reconnecting (#{FailedTryCount}) after {Delay}...",
            peer.Ref, failedTryCount, delay.ToShortString());

        return (DelayImpl(), now + delay);

        async Task DelayImpl()
        {
            var cancelActiveDelaysToken = CancelActiveDelaysToken;
            var cts = cancellationToken.LinkWith(cancelActiveDelaysToken);
            try {
                await Clock.Delay(delay, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancelActiveDelaysToken.IsCancellationRequested) {
                // We complete normally in this case
            }
            finally {
                cts.Dispose();
            }
        }
    }
}
