using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Stl.Rpc.Infrastructure;

public sealed record RpcPeerConnectionState(
    Channel<RpcMessage>? Channel = null,
    Exception? Error = null,
    int TryIndex = 0)
{
    public static readonly RpcPeerConnectionState Initial = new();

#if NETSTANDARD2_0
        public bool IsConnected(out Channel<RpcMessage>? channel)
#else
    public bool IsConnected([NotNullWhen(true)] out Channel<RpcMessage>? channel)
#endif
    {
        channel = Channel;
        return channel != null;
    }

    public RpcPeerConnectionState Next(Channel<RpcMessage>? channel, Exception? error)
        => error == null ? Next(channel) : Next(error);

    public RpcPeerConnectionState Next(Channel<RpcMessage>? channel)
        => new(channel);

    public RpcPeerConnectionState Next(Exception error)
        => new(null, error, TryIndex + 1);
}
