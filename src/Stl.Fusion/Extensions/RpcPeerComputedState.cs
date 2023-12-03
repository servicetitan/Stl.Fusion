namespace Stl.Fusion.Extensions;

public enum RpcPeerComputedStateKind
{
    Connected = 0,
    JustDisconnected,
    Disconnected,
    Reconnecting,
}

public sealed record RpcPeerComputedState(
    RpcPeerComputedStateKind Kind,
    Exception? LastError = null,
    TimeSpan? ReconnectsIn = null)
{
    public bool IsConnected => Kind == RpcPeerComputedStateKind.Connected;
    public bool IsOrLikelyConnected =>
        Kind == RpcPeerComputedStateKind.Connected
        || Kind == RpcPeerComputedStateKind.JustDisconnected;

    public string GetActivityDescription(bool useLastError = false)
    {
        switch (Kind) {
        case RpcPeerComputedStateKind.Connected:
            return "Connected.";
        case RpcPeerComputedStateKind.JustDisconnected:
            return "Connected, checking...";
        case RpcPeerComputedStateKind.Reconnecting:
            return "Reconnecting...";
        }
        if (LastError == null || !useLastError)
            return "Disconnected.";

        var message = LastError.Message.Trim();
        if (!(message.EndsWith(".", StringComparison.Ordinal)
            || message.EndsWith("!", StringComparison.Ordinal)
            || message.EndsWith("?", StringComparison.Ordinal)))
            message += ".";
        return "Disconnected: " + message;
    }
}
