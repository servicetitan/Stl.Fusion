using System.Diagnostics.CodeAnalysis;

namespace Stl.Rpc.Infrastructure;

public sealed record RpcPeerConnectionState(
    RpcConnection? Connection = null,
    Exception? Error = null,
    int TryIndex = 0,
    CancellationTokenSource? ReaderAbortSource = null,
    TaskCompletionSource<RpcHandshake>? HandshakeSource = null)
{
    private static readonly TaskCompletionSource<RpcHandshake> NoHandshakeSource
        = new TaskCompletionSource<RpcHandshake>().WithCancellation();
    public static readonly RpcPeerConnectionState Initial = new();

    public Channel<RpcMessage>? Channel = Connection?.Channel;
    public Task<RpcHandshake> HandshakeTask => (HandshakeSource ?? NoHandshakeSource).Task;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsConnected()
        => Connection != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETSTANDARD2_0
    public bool IsConnected(out RpcConnection? connection)
#else
    public bool IsConnected([NotNullWhen(true)] out RpcConnection? connection)
#endif
    {
        connection = Connection;
        return connection != null;
    }
}
