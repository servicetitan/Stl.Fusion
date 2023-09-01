namespace Stl.Rpc.Infrastructure;

public sealed class RpcLocalStream(RpcStream stream) : WorkerBase, IRpcObject
{
    private long _ackIndex = 0;
    private TaskCompletionSource<Unit>? _whenAck;

    public long Id { get; } = stream.Id;
    public RpcObjectKind Kind { get; } = stream.Kind;
    public RpcStream Stream { get; } = stream;
    public RpcPeer Peer { get; } = stream.Peer!;

    public void OnAck(long offset)
    {
        lock (Lock) {
            if (WhenRunning != null && offset != long.MaxValue)
                this.Start();
            _ackIndex = offset;
            _whenAck?.TrySetResult(default);
        }
        if (offset == long.MaxValue)
            _ = DisposeAsync();
    }

    // Protected methods

    protected override Task OnRun(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override Task OnStop()
    {
        Peer.LocalObjects.Unregister(this);
        return Task.CompletedTask;
    }
}
