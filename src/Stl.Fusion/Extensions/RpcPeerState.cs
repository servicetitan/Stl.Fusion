using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Extensions;

// Any Moment below is derived with RpcHub.Clock, which is CpuClock

public abstract record RpcPeerState
{
    public abstract Moment EnteredAt { get; }

    public RpcPeerConnectedState ToConnected(Moment now)
        => this as RpcPeerConnectedState ?? new RpcPeerConnectedState(now);

    public RpcPeerDisconnectedState ToDisconnected(Moment now, Moment reconnectsAt, RpcPeerConnectionState state)
        => ToDisconnected(now, reconnectsAt, state.Error);
    public RpcPeerDisconnectedState ToDisconnected(Moment now, Moment reconnectsAt, Exception? lastError)
    {
        if (this is not RpcPeerDisconnectedState d)
            return new RpcPeerDisconnectedState(now, reconnectsAt, lastError);

        if (reconnectsAt == d.ReconnectsAt && d.LastError == lastError)
            return d;

        return new RpcPeerDisconnectedState(d.DisconnectedAt, reconnectsAt, lastError ?? d.LastError);
    }
}

public sealed record RpcPeerConnectedState(
    Moment ConnectedAt
) : RpcPeerState
{
    public override Moment EnteredAt => ConnectedAt;
}

public sealed record RpcPeerDisconnectedState(
    Moment DisconnectedAt,
    Moment ReconnectsAt, // < Now = tries to reconnect now
    Exception? LastError
) : RpcPeerState
{
    public override Moment EnteredAt => DisconnectedAt;
}
