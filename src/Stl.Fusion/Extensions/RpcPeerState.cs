namespace Stl.Fusion.Extensions;

public enum RpcPeerStateKind
{
    Connected = 0,
    JustDisconnected,
    Disconnected,
    Reconnecting,
}

public sealed record RpcPeerState(
    RpcPeerStateKind Kind,
    Exception? LastError = null,
    TimeSpan? ReconnectsIn = null)
{
    public bool IsConnected => Kind == RpcPeerStateKind.Connected;
    public bool IsOrLikelyConnected =>
        Kind == RpcPeerStateKind.Connected
        || Kind == RpcPeerStateKind.JustDisconnected;

    public string GetDescription(bool useLastError = false)
    {
        switch (Kind) {
        case RpcPeerStateKind.Connected:
            return "Connected.";
        case RpcPeerStateKind.JustDisconnected:
            return "Connected, checking...";
        case RpcPeerStateKind.Reconnecting:
            return "Reconnecting...";
        }
        if (LastError == null || !useLastError)
            return "Disconnected.";

        var message = LastError.Message.Trim();
        if (!(message.EndsWith('.') || message.EndsWith('!') || message.EndsWith('?')))
            message += ".";
        return "Disconnected: " + message;
    }
}
