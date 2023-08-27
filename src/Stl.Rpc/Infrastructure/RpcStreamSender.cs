namespace Stl.Rpc.Infrastructure;

public sealed class RpcStreamSender(long id, RpcStream stream) : WorkerBase
{
    private long _ackOffset = 0;
    private TaskCompletionSource<Unit>? _whenAck;

    public readonly long Id = id;
    public readonly RpcStream Stream = stream;
    public readonly RpcPeer Peer = stream.Peer!;

    public void OnAck(long offset)
    {
        lock (Lock) {
            if (WhenRunning != null && offset != long.MaxValue)
                this.Start();
            _ackOffset = offset;
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
        Peer.OutgoingStreams.Unregister(Id, this);
        return Task.CompletedTask;
    }
}
