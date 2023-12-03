using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Extensions;

// Any Moment below is derived with RpcHub.Clock, which is CpuClock

public abstract record RpcPeerRawState
{
    public abstract bool IsConnected { get; }
    public abstract Moment EnteredAt { get; }

    public RpcPeerRawConnectedState ToConnected(Moment now)
        => this as RpcPeerRawConnectedState ?? new RpcPeerRawConnectedState(now);

    public RpcPeerRawDisconnectedState ToDisconnected(Moment now, Moment reconnectsAt, RpcPeerConnectionState state)
        => ToDisconnected(now, reconnectsAt, state.Error);
    public RpcPeerRawDisconnectedState ToDisconnected(Moment now, Moment reconnectsAt, Exception? lastError)
    {
        if (this is not RpcPeerRawDisconnectedState d)
            return new RpcPeerRawDisconnectedState(now, reconnectsAt, lastError);

        if (reconnectsAt == d.ReconnectsAt && d.LastError == lastError)
            return d;

        return new RpcPeerRawDisconnectedState(d.DisconnectedAt, reconnectsAt, lastError ?? d.LastError);
    }
}

public sealed record RpcPeerRawConnectedState(
    Moment ConnectedAt
) : RpcPeerRawState
{
    public override bool IsConnected => true;
    public override Moment EnteredAt => ConnectedAt;
}

public sealed record RpcPeerRawDisconnectedState(
    Moment DisconnectedAt,
    Moment ReconnectsAt, // < Now = tries to reconnect now
    Exception? LastError
) : RpcPeerRawState
{
    public override bool IsConnected => false;
    public override Moment EnteredAt => DisconnectedAt;
}
