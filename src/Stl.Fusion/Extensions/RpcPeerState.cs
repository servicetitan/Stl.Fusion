namespace Stl.Fusion.Extensions;

public record RpcPeerState(
    bool IsConnected,
    Exception? Error = null,
    Moment? ReconnectsAt = null);
