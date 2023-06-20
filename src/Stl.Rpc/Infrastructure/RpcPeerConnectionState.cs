using System.Diagnostics.CodeAnalysis;

namespace Stl.Rpc.Infrastructure;

public sealed record RpcPeerConnectionState(
    Channel<RpcMessage>? Channel = null,
    Exception? Error = null,
    CancellationTokenSource? ReaderAbortSource = null,
    int TryIndex = 0)
{
    public static readonly RpcPeerConnectionState Initial = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsConnected()
        => Channel != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETSTANDARD2_0
    public bool IsConnected(out Channel<RpcMessage>? channel)
#else
    public bool IsConnected([NotNullWhen(true)] out Channel<RpcMessage>? channel)
#endif
    {
        channel = Channel;
        return channel != null;
    }
}
